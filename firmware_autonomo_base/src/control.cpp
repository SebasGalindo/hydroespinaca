#include "control.h"

// ========================================
// CONSTRUCTOR E INICIALIZACIÓN
// ========================================

AutonomousController::AutonomousController(SensorManager* sensorManager, NTPClient* ntpClient) 
    : sensors(sensorManager), timeClient(ntpClient) {
    
    // Inicializar estructura de control
    memset(&state, 0, sizeof(ControlState));
    state.systemEnabled = true;
    state.waterLevelOk = true;
    state.readingIndex = 0;
    state.readingCount = 0;
}

void AutonomousController::begin() {
    Serial.println("🤖 [CTRL] Iniciando controlador autónomo para espinaca DWC");
    
    // Inicializar configuraciones de variables de control
    initializeVariableConfigs();
    
    // Inicializar timestamps
    unsigned long now = millis();
    state.lastControlCycle = now;
    
    // NUEVA LÓGICA: Inicializar máquina de estados de bomba de aire
    state.airStoneMode = AIR_MODE_AUTONOMOUS;
    state.airLeadStartTime = 0;
    state.airPostStartTime = 0;
    state.airPeriodicStartTime = 0;
    calculateNextAutonomousAirTime();
    
    // NUEVA LÓGICA: Inicializar máquina de estados de luz artificial
    state.lightState = LIGHT_OFF;
    state.lightStateStartTime = now;
    state.lightOnStartTime = 0;
    state.lightRestStartTime = 0;
    state.lightQualityAtChange = 0.0f;
    
    // Asegurar que todos los actuadores empiecen apagados
    ActuatorController::emergencyStop();
    
    Serial.println("✅ [CTRL] Controlador autónomo iniciado");
    Serial.println("📊 [CTRL] Configuración:");
    Serial.printf("   🌡️  Temperatura: %.1f-%.1f°C (histeresis ±%.1f°C)\n", 
                  TEMP_MIN, TEMP_MAX, TEMP_HYSTERESIS);
    Serial.printf("   💧 Humedad: %.1f-%.1f%% (histeresis ±%.1f%%)\n", 
                  HUMIDITY_MIN, HUMIDITY_MAX, HUMIDITY_HYSTERESIS);
    Serial.printf("   🌡️  Temperatura agua: %.1f-%.1f°C (histeresis ±%.1f°C)\n", 
                  WATER_TEMP_MIN, WATER_TEMP_MAX, WATER_TEMP_HYSTERESIS);
    Serial.printf("   💡 Luz: %.1f-%.1f%% (horario %d:00-%d:00)\n", 
                  LIGHT_ON_THRESHOLD, LIGHT_OFF_THRESHOLD, LIGHT_START_HOUR, LIGHT_END_HOUR);
    Serial.printf("   🌊 Nivel agua mínimo: %.1f cm\n", WATER_LEVEL_MIN);
}

// ========================================
// BUCLE PRINCIPAL
// ========================================

void AutonomousController::loop() {
    unsigned long now = millis();
    
    // Actualizar lecturas de sensores cada 2 minutos
    static unsigned long lastSensorUpdate = 0;
    if (now - lastSensorUpdate >= SENSOR_READING_INTERVAL) {
        updateSensorReadings();
        lastSensorUpdate = now;
    }
    
    // Ejecutar control cada 4 minutos (con 2 lecturas promediadas)
    if (shouldExecuteControl()) {
        Serial.println("⚡ [CTRL] ═══ CICLO DE CONTROL INICIADO ═══");
        
        calculateAverages();
        printSensorAverages();
        
        // Verificar reglas de seguridad primero
        checkWaterLevelSafety();
        
        if (state.systemEnabled) {
            // Ejecutar reglas de control
            controlTemperature();
            controlWaterTemperature();
            controlHumidity();
            controlLight();
            
            // NUEVA LÓGICA: Coordinación mejorada de bombas
            runPeriodicRoutines();
            handleAirStoneCoordination();
            
            // Aplicar cambios a actuadores
            applyActuatorStates();
        }
        
        printActuatorStates();
        state.lastControlCycle = now;
        
        Serial.println("✅ [CTRL] ═══ CICLO DE CONTROL COMPLETADO ═══");
    }
}

// ========================================
// GESTIÓN DE LECTURAS Y PROMEDIOS
// ========================================

void AutonomousController::updateSensorReadings() {
    Serial.println("📊 [CTRL] Actualizando lecturas de sensores...");
    
    SensorReadings& reading = state.readings[state.readingIndex];
    reading.timestamp = millis();
    
    // Leer todos los sensores
    reading.temperature = sensors->readTemperature();
    reading.humidity = sensors->readHumidity();
    reading.lightIndex = sensors->readLightIndex();
    reading.clearChannel = sensors->readLightClearChannel();  // Nuevo canal Clear
    // Convert distance (cm) to water level height (cm) for control logic
    float distanceCm = sensors->readWaterLevel();
    if (!isnan(distanceCm)) {
        const float TANK_HEIGHT_CM = 40.0f;  // Same constant as in sensors.cpp
        // Convert distance from sensor to actual water level height
        reading.waterLevel = TANK_HEIGHT_CM - distanceCm;
        if (reading.waterLevel < 0.0f) reading.waterLevel = 0.0f;
        if (reading.waterLevel > TANK_HEIGHT_CM) reading.waterLevel = TANK_HEIGHT_CM;
    } else {
        reading.waterLevel = NAN;
    }
    reading.waterTemp = sensors->readTankTemperature();
    reading.ph = sensors->readPH();
    reading.tds = sensors->readTDS();
    
    // Validar que las lecturas críticas sean válidas
    // NOTA: lightIndex puede ser NAN en oscuridad (C < 3000), pero la lectura sigue siendo válida
    reading.valid = !isnan(reading.temperature) && 
                   !isnan(reading.humidity) && 
                   !isnan(reading.waterLevel);
    
    Serial.printf("📊 [CTRL] Lectura %d: T=%.1f°C H=%.1f%% L=%.1f%% C=%d N=%.1fcm %s\n",
                  state.readingIndex, reading.temperature, reading.humidity, 
                  reading.lightIndex, reading.clearChannel, reading.waterLevel,
                  reading.valid ? "✅" : "❌");
    
    // Avanzar índice circular
    state.readingIndex = (state.readingIndex + 1) % MAX_READINGS;
    if (state.readingCount < MAX_READINGS) {
        state.readingCount++;
    }
}

void AutonomousController::calculateAverages() {
    if (state.readingCount == 0) {
        Serial.println("⚠️ [CTRL] No hay lecturas válidas para promediar");
        return;
    }
    
    // Inicializar acumuladores
    float tempSum = 0, humidSum = 0, lightSum = 0, levelSum = 0;
    float waterTempSum = 0, phSum = 0, tdsSum = 0;
    float clearSum = 0;
    int validCount = 0;
    int lightValidCount = 0;  // Contador específico para lightIndex (puede ser menor que validCount)
    
    // Calcular promedios solo de lecturas válidas
    for (int i = 0; i < state.readingCount; i++) {
        const SensorReadings& reading = state.readings[i];
        if (reading.valid) {
            tempSum += reading.temperature;
            humidSum += reading.humidity;
            
            // lightIndex puede ser NAN en oscuridad - manejar por separado
            if (!isnan(reading.lightIndex)) {
                lightSum += reading.lightIndex;
                lightValidCount++;
            }
            
            clearSum += reading.clearChannel;
            levelSum += reading.waterLevel;
            waterTempSum += reading.waterTemp;
            phSum += reading.ph;
            tdsSum += reading.tds;
            validCount++;
        }
    }
    
    if (validCount > 0) {
        state.averageReadings.temperature = tempSum / validCount;
        state.averageReadings.humidity = humidSum / validCount;
        
        // lightIndex promedio solo si hay lecturas válidas, sino NAN
        if (lightValidCount > 0) {
            state.averageReadings.lightIndex = lightSum / lightValidCount;
        } else {
            state.averageReadings.lightIndex = NAN;  // No hay lecturas de lightIndex válidas (oscuridad)
        }
        
        state.averageReadings.clearChannel = (uint16_t)(clearSum / validCount);
        state.averageReadings.waterLevel = levelSum / validCount;
        state.averageReadings.waterTemp = waterTempSum / validCount;
        state.averageReadings.ph = phSum / validCount;
        state.averageReadings.tds = tdsSum / validCount;
        state.averageReadings.valid = true;
        state.averageReadings.timestamp = millis();
        
        Serial.printf("📊 [CTRL] Promedios (%d lecturas): T=%.1f°C H=%.1f%% L=%s C=%d N=%.1fcm\n",
                      validCount, state.averageReadings.temperature, 
                      state.averageReadings.humidity, 
                      lightValidCount > 0 ? (String(state.averageReadings.lightIndex, 1) + "%").c_str() : "N/A",
                      state.averageReadings.clearChannel, state.averageReadings.waterLevel);
    } else {
        state.averageReadings.valid = false;
        Serial.println("❌ [CTRL] No hay lecturas válidas para calcular promedios");
    }
}

bool AutonomousController::shouldExecuteControl() {
    unsigned long now = millis();
    return (now - state.lastControlCycle >= CONTROL_CYCLE_INTERVAL) && 
           (state.readingCount >= MAX_READINGS);
}

// ========================================
// REGLAS DE CONTROL
// ========================================

void AutonomousController::checkWaterLevelSafety() {
    if (!state.averageReadings.valid) {
        logSafetyAction("Sensores inválidos - sistema en modo seguro");
        state.systemEnabled = false;
        state.waterLevelOk = false;
        
        // Asegurar que relé maestro humidificador esté OFF en modo seguro
        ActuatorController::turnHumidifierMasterOff();
        return;
    }
    
    float waterLevel = state.averageReadings.waterLevel;
    bool previousWaterOk = state.waterLevelOk;
    
    // NUEVA LÓGICA: Histéresis correcta para nivel de agua
    if (state.waterLevelOk) {
        // Bomba permitida: bloquear si nivel > 7cm (tanque vacío)
        state.waterLevelOk = waterLevel <= WATER_LEVEL_MIN;
    } else {
        // Bomba bloqueada: permitir solo si nivel < 6cm (tanque lleno)
        state.waterLevelOk = waterLevel < WATER_LEVEL_RECOVERY;
    }
    
    if (!state.waterLevelOk) {
        if (previousWaterOk) {
            logSafetyAction("Nivel agua ALTO - tanque vacío - bloqueando bomba y difusor");
            Serial.printf("🚨 [NIVEL] %.1fcm > %.1fcm (límite) - BOMBA OFF por tanque vacío\n", 
                         waterLevel, WATER_LEVEL_MIN);
        }
        
        // Forzar apagado de bomba y difusor
        state.waterPump.forceOff = true;
        state.airStone.forceOff = true;
        
        // Apagar inmediatamente si están encendidos
        if (state.waterPump.isOn) {
            setActuatorState(state.waterPump, false, "bomba");
        }
        if (state.airStone.isOn) {
            setActuatorState(state.airStone, false, "difusor");
        }
        
        // Asegurar que relé maestro humidificador esté OFF en modo seguro
        ActuatorController::turnHumidifierMasterOff();
    } else {
        if (!previousWaterOk) {
            logSafetyAction("Nivel agua BAJO - tanque lleno - habilitando bomba");
            Serial.printf("✅ [NIVEL] %.1fcm < %.1fcm (recuperación) - BOMBA habilitada, tanque lleno\n", 
                         waterLevel, WATER_LEVEL_RECOVERY);
        }
        
        // Restaurar operación normal
        state.waterPump.forceOff = false;
        state.airStone.forceOff = false;
        state.systemEnabled = true;
    }
}

void AutonomousController::controlTemperature() {
    if (!state.averageReadings.valid) return;
    
    float temp = state.averageReadings.temperature;
    
    // CONTROL DIRECTO SIN DEBOUNCE DEFECTUOSO
    // === CALEFACTOR AIRE ===
    if (state.heater.isOn) {
        // Calefactor encendido: apagar cuando alcance temperatura mínima + histéresis
        if (temp >= TEMP_MIN + TEMP_HYSTERESIS) {
            setActuatorState(state.heater, false, "calefactor_aire");
            state.heater.lastRestTime = millis();
            logControlDecision("Temperatura", temp, "calefactor aire OFF - temperatura recuperada");
        }
    } else {
        // Calefactor apagado: encender cuando esté por debajo del mínimo - histéresis
        if (temp <= TEMP_MIN - TEMP_HYSTERESIS) {
            if (canActivateAfterRest(state.heater, state.tempConfig.restTimeMs)) {
                setActuatorState(state.heater, true, "calefactor_aire");
                logControlDecision("Temperatura", temp, "calefactor aire ON - temperatura baja");
            } else {
                Serial.printf("⏳ [CTRL] Calefactor aire bloqueado por tiempo de reposo (%.1f°C ≤ %.1f°C)\n", 
                             temp, TEMP_MIN - TEMP_HYSTERESIS);
            }
        }
    }
    
    // === VENTILADOR ===
    if (state.fan.isOn) {
        // Ventilador encendido: apagar cuando baje de temperatura máxima - histéresis
        if (temp <= TEMP_MAX - TEMP_HYSTERESIS) {
            setActuatorState(state.fan, false, "ventilador");
            state.fan.lastRestTime = millis();
            logControlDecision("Temperatura", temp, "ventilador OFF - temperatura recuperada");
        }
    } else {
        // Ventilador apagado: encender cuando supere el máximo + histéresis
        if (temp >= TEMP_MAX + TEMP_HYSTERESIS) {
            if (canActivateAfterRest(state.fan, state.tempConfig.restTimeMs)) {
                setActuatorState(state.fan, true, "ventilador");
                logControlDecision("Temperatura", temp, "ventilador ON - temperatura alta");
            } else {
                Serial.printf("⏳ [CTRL] Ventilador bloqueado por tiempo de reposo (%.1f°C ≥ %.1f°C)\n", 
                             temp, TEMP_MAX + TEMP_HYSTERESIS);
            }
        }
    }
}

// NUEVA LÓGICA: Control de humedad con debounce y reposo
void AutonomousController::controlHumidity() {
    if (!state.averageReadings.valid) return;
    
    float humidity = state.averageReadings.humidity;
    unsigned long now = millis();
    
    // Verificar fail-safe por nivel de agua
    if (!state.waterLevelOk && state.humidifierMasterActive) {
        state.humidifierMasterActive = false;
        ActuatorController::turnHumidifierMasterOff();
        ActuatorController::turnHumidifierRelayOff();
        logSafetyAction("Humidificador APAGADO por nivel agua alto (tanque vacío)");
        return;
    }
    
    // === ACTIVACIÓN DIRECTA SIN DEBOUNCE DEFECTUOSO ===
    if (humidity <= HUMIDITY_MIN - HUMIDITY_HYSTERESIS && !state.humidifierMasterActive) {
        // Verificar tiempo de reposo
        if (!canActivateAfterRest(state.humidifier, state.humidityConfig.restTimeMs)) {
            Serial.printf("⏳ [CTRL] Humidificador bloqueado por tiempo de reposo (H=%.1f%% ≤ %.1f%%)\n", 
                         humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);
            return;
        }
        
        Serial.printf("💨 [CTRL] Iniciando secuencia completa humidificador (H=%.1f%% ≤ %.1f%%)\n", 
                     humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);
        
        // Marcar como activo antes de comenzar la secuencia
        state.humidifierMasterActive = true;
        state.humidifierStartTime = now;
        state.humidifier.isOn = true;
        state.humidifier.lastChangeTime = now;
        
        // 1. Activar relé maestro (PIN 14)
        ActuatorController::turnHumidifierMasterOn();
        Serial.println("💨 [CTRL] Humidificador: relé maestro ON");
        
        // 2. Esperar delay de seguridad (2 segundos)
        delay(2000);
        
        // 3. Pulso en relé de activación (1 segundo ON, luego OFF)
        ActuatorController::turnHumidifierRelayOn();
        Serial.println("💨 [CTRL] Humidificador: relé activación ON (pulso)");
        delay(1000);
        
        ActuatorController::turnHumidifierRelayOff(); 
        Serial.println("💨 [CTRL] Humidificador: relé activación OFF - secuencia completa ejecutada");
        
        logControlDecision("Humedad", humidity, "humidificador activado completamente");
        return;
    }
    
    // === DESACTIVACIÓN CON HISTÉRESIS ===
    if (state.humidifierMasterActive && humidity >= HUMIDITY_MAX + HUMIDITY_HYSTERESIS) {
        state.humidifierMasterActive = false;
        state.humidifier.isOn = false;
        state.humidifier.lastRestTime = now;  // Guardar tiempo para reposo
        
        ActuatorController::turnHumidifierMasterOff();
        ActuatorController::turnHumidifierRelayOff(); // Asegurar relé de activación OFF
        
        Serial.printf("✅ [CTRL] Humidificador: desactivado completamente (H=%.1f%% ≥ %.1f%%)\n", 
                     humidity, HUMIDITY_MAX + HUMIDITY_HYSTERESIS);
        logControlDecision("Humedad", humidity, "humidificador desactivado");
    }
}

void AutonomousController::controlWaterTemperature() {
    if (!state.averageReadings.valid) return;
    
    float waterTemp = state.averageReadings.waterTemp;
    
    // Solo actuar si hay agua suficiente para evitar quemar la resistencia
    if (!state.waterLevelOk) {
        // Forzar apagado por seguridad
        if (state.waterHeater.isOn) {
            setActuatorState(state.waterHeater, false, "calefactor_agua");
            state.waterHeater.lastRestTime = millis();  // Tiempo de reposo
            logSafetyAction("Calefactor agua apagado - nivel agua insuficiente");
        }
        state.waterHeater.forceOff = true;
        return;
    } else {
        state.waterHeater.forceOff = false;
    }
    
    // LÓGICA CORREGIDA: Control directo con histéresis apropiada para calefactor
    bool shouldActivate = false;
    bool shouldDeactivate = false;
    
    if (state.waterHeater.isOn) {
        // CALEFACTOR ENCENDIDO: Apagar cuando alcance temperatura objetivo + histéresis
        shouldDeactivate = (waterTemp >= WATER_TEMP_MAX);
        Serial.printf("🌡️ [CTRL] Calefactor ON: %.1f°C (apagar si ≥%.1f°C)\n", 
                     waterTemp, WATER_TEMP_MAX);
    } else {
        // CALEFACTOR APAGADO: Encender cuando esté por debajo del mínimo - histéresis
        shouldActivate = (waterTemp <= WATER_TEMP_MIN - WATER_TEMP_HYSTERESIS);
        Serial.printf("🌡️ [CTRL] Calefactor OFF: %.1f°C (encender si ≤%.1f°C)\n", 
                     waterTemp, WATER_TEMP_MIN - WATER_TEMP_HYSTERESIS);
    }
    
    // Activar calefactor
    if (shouldActivate) {
        // Verificar tiempo de reposo
        if (!canActivateAfterRest(state.waterHeater, state.waterTempConfig.restTimeMs)) {
            Serial.printf("⏳ [CTRL] Calefactor agua bloqueado por tiempo de reposo (%.1f°C ≤ %.1f°C)\n", 
                         waterTemp, WATER_TEMP_MIN - WATER_TEMP_HYSTERESIS);
            return;
        }
        
        setActuatorState(state.waterHeater, true, "calefactor_agua");
        state.waterHeaterStartTime = millis();
        state.pumpRunningForHeater = false;  // Resetear estado bomba
        logControlDecision("Temperatura agua", waterTemp, "calefactor agua ON - temperatura baja");
    }
    
    // Desactivar calefactor
    if (shouldDeactivate) {
        setActuatorState(state.waterHeater, false, "calefactor_agua");
        state.waterHeater.lastRestTime = millis();  // Guardar tiempo para reposo
        logControlDecision("Temperatura agua", waterTemp, "calefactor agua OFF - temperatura objetivo alcanzada");
    }
    
    // Coordinar bomba con calefactor para recirculación
    if (state.waterHeater.isOn) {
        // Si bomba no está corriendo por calefactor, iniciarla
        if (!state.pumpRunningForHeater && !state.waterPump.isOn) {
            setActuatorState(state.waterPump, true, "bomba");
            state.pumpRunningForHeater = true;
            state.heaterPumpStartTime = millis();
            logControlDecision("Calefactor agua activo", waterTemp, "bomba ON para recirculación");
        }
    }
    
    // Mantener bomba corriendo tiempo mínimo para distribución de calor
    if (state.pumpRunningForHeater && state.waterPump.isOn) {
        unsigned long now = millis();
        
        // Apagar bomba solo si:
        // 1. Calefactor ya no necesita recirculación Y
        // 2. Ha corrido el tiempo mínimo
        if (!state.waterHeater.isOn && 
            (now - state.heaterPumpStartTime >= PUMP_MIN_ON_TIME)) {
            setActuatorState(state.waterPump, false, "bomba");
            state.pumpRunningForHeater = false;
            logControlDecision("Recirculación completada", waterTemp, "bomba OFF");
        }
    }
}

void AutonomousController::controlLight() {
    // Usar la nueva máquina de estados para control de luz
    handleLightStateMachine();
}

// NUEVA LÓGICA: Rutinas periódicas con horarios fijos (sin persistencia)
void AutonomousController::runPeriodicRoutines() {
    // Solo ejecutar si nivel de agua OK
    if (!state.waterLevelOk) return;
    
    // El nuevo sistema no necesita calcular horarios - verifica tiempo absoluto directamente
    
    // Ejecutar rutinas según prioridades (orden de máxima a mínima prioridad)
    handleThermalEmergency();  // PRIORIDAD MÁXIMA: Emergencia >25°C
    handlePumpCycle();         // Prioridad 1: Recirculación cada 4h  
    handleAirCycle();          // Prioridad 2: Aireación cada 30min
}

// Verificar horarios fijos absolutos (basado en tiempo real, no en ciclos)
bool AutonomousController::isPumpScheduleTime() {
    unsigned long currentMinutes = getCurrentMinutes();
    unsigned long hoursSinceMidnight = currentMinutes / 60;
    unsigned long minutesInCurrentHour = currentMinutes % 60;
    
    // Horarios fijos de recirculación: 00:00, 04:00, 08:00, 12:00, 16:00, 20:00
    bool isFixedHour = (hoursSinceMidnight % 4 == 0);
    
    // Ventana de tolerancia: 0-3 minutos (para ciclo de control de 4 min)
    bool inToleranceWindow = (minutesInCurrentHour <= 3);
    
    return isFixedHour && inToleranceWindow;
}

bool AutonomousController::isAirScheduleTime() {
    unsigned long currentMinutes = getCurrentMinutes();
    unsigned long minutesInCurrentHour = currentMinutes % 60;
    
    // Horarios fijos de aireación: XX:00, XX:30 (cada 30 minutos exactos)
    bool isFixedMinute = (minutesInCurrentHour == 0 || minutesInCurrentHour == 30);
    
    // Ventana de tolerancia: 0-3 minutos después del horario fijo
    bool inToleranceWindow = (minutesInCurrentHour <= 3 || 
                              (minutesInCurrentHour >= 30 && minutesInCurrentHour <= 33));
    
    return isFixedMinute || inToleranceWindow;
}

// Obtener minutos actuales desde 00:00 (usando NTP si disponible)
unsigned long AutonomousController::getCurrentMinutes() {
    if (!timeClient->isTimeSet()) {
        // Fallback: usar millis() como aproximación (reinicia cada boot)
        return (millis() / 1000) / 60;  // millis -> segundos -> minutos
    }
    
    // Usar hora real de NTP (Colombia UTC-5)
    int hours = timeClient->getHours();
    int minutes = timeClient->getMinutes();
    
    // Convertir a Colombia (UTC-5)
    hours = (hours - 5 + 24) % 24;
    
    return (hours * 60) + minutes;
}

// Manejar ciclo de recirculación (prioridad 1) - HORARIOS FIJOS ABSOLUTOS
void AutonomousController::handlePumpCycle() {
    unsigned long now = millis();
    
    // Verificar si es exactamente hora de recirculación (cada 4h: 00:00, 04:00, 08:00, etc.)
    if (isPumpScheduleTime() && !state.pumpCycleActive) {
        state.pumpCycleActive = true;
        state.pumpHasPriority = true;  // Cancelar aire periódico
        
        // LEAD: Activar aire 90s antes de motobomba
        state.airLeadActive = true;
        state.airLeadStartTime = now;  // Usar timestamp específico
        setActuatorState(state.airStone, true, "difusor");
        logRoutineAction("pump_will_activate", "aire lead 90s");
        return;
    }
    
    // Estado: LEAD activo (aire antes de motobomba)
    if (state.airLeadActive && (now - state.airLeadStartTime >= AIR_LEAD_MS)) {
        state.airLeadActive = false;
        
        // Activar motobomba por 8 minutos
        state.pumpStartTime = now;
        setActuatorState(state.waterPump, true, "bomba");
        logRoutineAction("pump_activated", "motobomba 8min + aire continuo");
        return;
    }
    
    // Estado: Motobomba activa
    if (state.waterPump.isOn && (now - state.pumpStartTime >= PUMP_DURATION_MS)) {
        // Apagar motobomba, iniciar POST HOLD
        setActuatorState(state.waterPump, false, "bomba");
        state.airPostActive = true;
        state.airPostStartTime = now;  // Usar timestamp específico para post
        logRoutineAction("pump_finished", "aire post hold 45s");
        return;
    }
    
    // Estado: POST HOLD activo (aire después de motobomba)
    if (state.airPostActive && (now - state.airPostStartTime >= POST_AIR_HOLD_MS)) {
        state.airPostActive = false;
        state.pumpCycleActive = false;
        state.pumpHasPriority = false;  // Liberar prioridad
        
        // IMPORTANTE: NO apagar aire aquí - la coordinación se encarga
        // La función handleAirStoneCoordination() detectará el fin de recirculación
        
        logRoutineAction("pump_cycle_complete", "recirculación terminada - coordinación toma control de aire");
        return;
    }
}

// FUNCIÓN OBSOLETA: La coordinación ahora se maneja en handleAirStoneCoordination()
void AutonomousController::handleAirCycle() {
    // Esta función se mantiene por compatibilidad pero ya no se usa
    // La lógica de aireación ahora está centralizada en handleAirStoneCoordination()
    return;
}

// NUEVA REGLA: Emergencia térmica (>25°C) - PRIORIDAD MÁXIMA
void AutonomousController::handleThermalEmergency() {
    if (!state.averageReadings.valid) return;
    
    float temp = state.averageReadings.temperature;
    unsigned long now = millis();
    
    // Verificar si se debe activar emergencia
    if (temp > TEMP_EMERGENCY_THRESHOLD && !state.thermalEmergencyActive) {
        // Verificar intervalo mínimo entre emergencias (evitar spam)
        if (now - state.lastEmergencyTime < EMERGENCY_MIN_INTERVAL_MS) {
            return;
        }
        
        // ACTIVAR EMERGENCIA TÉRMICA
        state.thermalEmergencyActive = true;
        state.emergencyStartTime = now;
        state.lastEmergencyTime = now;
        
        // Cancelar todas las rutinas normales (máxima prioridad)
        state.pumpCycleActive = false;
        state.airPeriodicActive = false;
        state.airLeadActive = false;
        state.airPostActive = false;
        state.pumpHasPriority = true;  // Bloquear rutinas normales
        
        // Activar recirculación de emergencia (aire + bomba inmediato)
        setActuatorState(state.airStone, true, "difusor");
        setActuatorState(state.waterPump, true, "bomba");
        
        Serial.printf("🚨 [EMERGENCIA] Temperatura crítica %.1f°C > %.1f°C - recirculación forzada\n", 
                     temp, TEMP_EMERGENCY_THRESHOLD);
        logRoutineAction("thermal_emergency", "recirculación 5min por temperatura crítica");
        return;
    }
    
    // Manejar emergencia activa
    if (state.thermalEmergencyActive) {
        // Verificar si debe terminar emergencia
        if (now - state.emergencyStartTime >= EMERGENCY_PUMP_DURATION_MS) {
            // Terminar emergencia
            state.thermalEmergencyActive = false;
            state.pumpHasPriority = false;  // Liberar bloqueo
            
            setActuatorState(state.waterPump, false, "bomba");
            setActuatorState(state.airStone, false, "difusor");
            
            Serial.printf("✅ [EMERGENCIA] Emergencia térmica completada - temperatura actual: %.1f°C\n", temp);
            logRoutineAction("thermal_emergency_complete", "recirculación de emergencia finalizada");
            return;
        }
        
        // Emergencia en progreso - mantener recirculación
        Serial.printf("⚠️ [EMERGENCIA] En progreso - T:%.1f°C, tiempo restante: %lus\n", 
                     temp, (EMERGENCY_PUMP_DURATION_MS - (now - state.emergencyStartTime)) / 1000);
    }
}

// MÁQUINA DE ESTADOS PARA CONTROL DE LUZ ARTIFICIAL
void AutonomousController::handleLightStateMachine() {
    if (!state.averageReadings.valid) return;
    
    float lightIndex = state.averageReadings.lightIndex;
    uint16_t clearChannel = state.averageReadings.clearChannel;
    bool lightIndexValid = !isnan(lightIndex);  // lightIndex puede ser NAN en oscuridad
    bool inSchedule = isLightScheduleActive();
    unsigned long now = millis();
    unsigned long stateTime = now - state.lightStateStartTime;
    
    // Si estamos fuera de horario, forzar estado OFF
    if (!inSchedule) {
        if (state.lightState != LIGHT_OFF) {
            state.lightState = LIGHT_OFF;
            state.lightStateStartTime = now;
            setActuatorState(state.light, false, "luz");
            Serial.printf("💡 [LUZ-SM] FUERA DE HORARIO → Estado: OFF\n");
        }
        return;
    }
    
    // Dentro de horario: ejecutar máquina de estados
    switch (state.lightState) {
        case LIGHT_OFF: {
            // Estado OFF: La luz está apagada
            // Condiciones para encender:
            // 1. Oscuridad total (clearChannel < DARKNESS_THRESHOLD)
            // 2. Calidad de luz insuficiente (lightIndex < LIGHT_ON_THRESHOLD)
            
            if (clearChannel < DARKNESS_THRESHOLD) {
                // Oscuridad total → encender inmediatamente
                state.lightState = LIGHT_ON;
                state.lightStateStartTime = now;
                state.lightOnStartTime = now;
                state.lightQualityAtChange = lightIndex;
                setActuatorState(state.light, true, "luz");
                
                Serial.printf("💡 [LUZ-SM] OFF→ON: Oscuridad total (Clear=%d < %d)\n", 
                             clearChannel, DARKNESS_THRESHOLD);
                             
            } else if (lightIndexValid && lightIndex < LIGHT_ON_THRESHOLD) {
                // Calidad insuficiente → encender para complementar
                state.lightState = LIGHT_ON;
                state.lightStateStartTime = now;
                state.lightOnStartTime = now;
                state.lightQualityAtChange = lightIndex;
                setActuatorState(state.light, true, "luz");
                
                Serial.printf("💡 [LUZ-SM] OFF→ON: Calidad insuficiente (%.1f%% < %.1f%%)\n", 
                             lightIndex, LIGHT_ON_THRESHOLD);
            } else if (!lightIndexValid) {
                // lightIndex no disponible (oscuridad intermedia) → mantener OFF
                Serial.printf("💡 [LUZ-SM] OFF: LightIndex N/A (C=%d), mantener OFF\n", clearChannel);
            }
            break;
        }
            
        case LIGHT_ON: {
            // Estado ON: La luz está encendida
            // Condiciones para apagar:
            // 1. Tiempo mínimo cumplido Y calidad suficiente (lightIndex > LIGHT_OFF_THRESHOLD)
            // 2. Tiempo máximo alcanzado (forzar descanso)
            
            unsigned long onTime = now - state.lightOnStartTime;
            
            if (onTime >= LIGHT_MAX_ON_MS) {
                // Tiempo máximo alcanzado → forzar descanso
                state.lightState = LIGHT_RESTING;
                state.lightStateStartTime = now;
                state.lightRestStartTime = now;
                state.lightQualityAtChange = lightIndex;
                setActuatorState(state.light, false, "luz");
                
                Serial.printf("💡 [LUZ-SM] ON→RESTING: Tiempo máximo (%lus) → descanso obligatorio\n", 
                             onTime / 1000);
                             
            } else if (onTime >= LIGHT_MIN_ON_MS && lightIndexValid && lightIndex > LIGHT_OFF_THRESHOLD) {
                // Tiempo mínimo cumplido y calidad suficiente → apagar
                state.lightState = LIGHT_OFF;
                state.lightStateStartTime = now;
                state.lightQualityAtChange = lightIndex;
                setActuatorState(state.light, false, "luz");
                
                Serial.printf("💡 [LUZ-SM] ON→OFF: Calidad suficiente (%.1f%% > %.1f%%) después de %lus\n", 
                             lightIndex, LIGHT_OFF_THRESHOLD, onTime / 1000);
                             
            } else if (onTime < LIGHT_MIN_ON_MS) {
                // Aún no cumple tiempo mínimo
                Serial.printf("💡 [LUZ-SM] ON: Tiempo mínimo pendiente (%lus/%lus) - Calidad: %s\n", 
                             onTime / 1000, LIGHT_MIN_ON_MS / 1000, 
                             lightIndexValid ? (String(lightIndex, 1) + "%").c_str() : "N/A");
            } else {
                // Tiempo mínimo cumplido pero lightIndex no válido o no suficiente calidad → continuar
                Serial.printf("💡 [LUZ-SM] ON: Continuando - Tiempo: %lus, Calidad: %s\n", 
                             onTime / 1000, 
                             lightIndexValid ? (String(lightIndex, 1) + "%").c_str() : "N/A");
            }
            break;
        }
            
        case LIGHT_RESTING: {
            // Estado RESTING: Luz en descanso obligatorio
            // Solo puede salir después del tiempo de descanso
            
            unsigned long restTime = now - state.lightRestStartTime;
            
            if (restTime >= LIGHT_REST_MS) {
                // Descanso completado → volver a evaluar condiciones
                bool needsLight = clearChannel < DARKNESS_THRESHOLD || 
                                (lightIndexValid && lightIndex < LIGHT_ON_THRESHOLD);
                
                if (needsLight) {
                    // Aún necesita luz → volver a encender
                    state.lightState = LIGHT_ON;
                    state.lightStateStartTime = now;
                    state.lightOnStartTime = now;
                    state.lightQualityAtChange = lightIndexValid ? lightIndex : 0.0f;
                    setActuatorState(state.light, true, "luz");
                    
                    if (clearChannel < DARKNESS_THRESHOLD) {
                        Serial.printf("💡 [LUZ-SM] RESTING→ON: Descanso completado, oscuridad total (C=%d)\n", clearChannel);
                    } else {
                        Serial.printf("💡 [LUZ-SM] RESTING→ON: Descanso completado, calidad insuficiente (%.1f%% < %.1f%%)\n", 
                                     lightIndex, LIGHT_ON_THRESHOLD);
                    }
                                 
                } else {
                    // Ya no necesita luz → permanecer apagada
                    state.lightState = LIGHT_OFF;
                    state.lightStateStartTime = now;
                    state.lightQualityAtChange = lightIndexValid ? lightIndex : 0.0f;
                    
                    Serial.printf("💡 [LUZ-SM] RESTING→OFF: Descanso completado, luz suficiente (C=%d, Calidad: %s)\n", 
                                 clearChannel, lightIndexValid ? (String(lightIndex, 1) + "%").c_str() : "N/A");
                }
            } else {
                // Aún en descanso
                Serial.printf("💡 [LUZ-SM] RESTING: Descanso en progreso (%lus/%lus) - C=%d, Calidad: %s\n", 
                             restTime / 1000, LIGHT_REST_MS / 1000, clearChannel,
                             lightIndexValid ? (String(lightIndex, 1) + "%").c_str() : "N/A");
            }
            break;
        }
    }
}

// ========================================
// FUNCIONES AUXILIARES
// ========================================

bool AutonomousController::isLightScheduleActive() {
    if (!timeClient->isTimeSet()) {
        // Modo seguro sin NTP: permitir control basado en sensores únicamente
        static unsigned long lastNtpWarning = 0;
        if (millis() - lastNtpWarning > 300000) {  // Warn every 5 minutes
            Serial.println("⚠️ [CTRL] NTP no disponible - control de luz por sensores únicamente");
            lastNtpWarning = millis();
        }
        return true;  // Permitir control basado en luz ambiente
    }
    
    // Obtener hora actual en Colombia (UTC-5)
    int currentHour = timeClient->getHours();
    currentHour = (currentHour - 5 + 24) % 24;  // Convertir a UTC-5
    
    return (currentHour >= LIGHT_START_HOUR && currentHour < LIGHT_END_HOUR);
}

bool AutonomousController::hasMinimumTimePassed(const ActuatorState& state, unsigned long minTime) {
    unsigned long now = millis();
    return (now - state.lastChangeTime) >= minTime;
}

void AutonomousController::setActuatorState(ActuatorState& state, bool newState, const char* name) {
    if (state.forceOff && newState) {
        Serial.printf("🔒 [CTRL] %s bloqueado por seguridad\n", name);
        return;
    }
    
    if (state.isOn != newState) {
        state.isOn = newState;
        state.lastChangeTime = millis();
        
        if (newState) {
            Serial.printf("🔛 [CTRL] %s → ON\n", name);
        } else {
            Serial.printf("🔴 [CTRL] %s → OFF\n", name);
        }
    }
}

void AutonomousController::applyActuatorStates() {
    // Aplicar estados a actuadores físicos
    if (state.heater.isOn) {
        ActuatorController::turnHeaterOn();
    } else {
        ActuatorController::turnHeaterOff();
    }
    
    if (state.waterHeater.isOn) {
        ActuatorController::turnWaterHeaterOn();
    } else {
        ActuatorController::turnWaterHeaterOff();
    }
    
    if (state.fan.isOn) {
        ActuatorController::turnFanOn();
    } else {
        ActuatorController::turnFanOff();
    }
    
    if (state.light.isOn) {
        ActuatorController::turnLightOn();
    } else {
        ActuatorController::turnLightOff();
    }
    
    // Humidificador controlado por controlHumidity() - no usar ActuatorState
    
    if (state.waterPump.isOn) {
        ActuatorController::turnWaterPumpOn();
    } else {
        ActuatorController::turnWaterPumpOff();
    }
    
    if (state.airStone.isOn) {
        ActuatorController::turnAirStoneOn();
    } else {
        ActuatorController::turnAirStoneOff();
    }
}

// ========================================
// LOGGING Y DIAGNÓSTICO
// ========================================

void AutonomousController::logControlDecision(const char* sensor, float value, const char* action) {
    Serial.printf("🤖 [CTRL] %s=%.1f → %s\n", sensor, value, action);
}

void AutonomousController::logSafetyAction(const char* reason) {
    Serial.printf("🚨 [SEC] %s\n", reason);
}

void AutonomousController::logRoutineAction(const char* routine, const char* action) {
    Serial.printf("⏰ [ROUTINE] %s: %s\n", routine, action);
}

void AutonomousController::printSensorAverages() {
    if (state.averageReadings.valid) {
        Serial.printf("📊 [CTRL] Promedios sensores:\n");
        Serial.printf("   🌡️  Temperatura aire: %.1f°C (óptimo: %.1f-%.1f°C, calefactor: <%.1f°C, ventilador: >%.1f°C)\n", 
                     state.averageReadings.temperature, TEMP_MIN, TEMP_MAX, 
                     TEMP_MIN - TEMP_HYSTERESIS, TEMP_MAX + TEMP_HYSTERESIS);
        Serial.printf("   💧 Humedad aire: %.1f%% (umbral: <%.1f%%=ON, >%.1f%%=OFF)\n", 
                     state.averageReadings.humidity, HUMIDITY_MIN, HUMIDITY_MAX);
        Serial.printf("   💡 Índice luz: %.1f%% (umbral: <%.1f%%=ON, >%.1f%%=OFF)\n", 
                     state.averageReadings.lightIndex, LIGHT_ON_THRESHOLD, LIGHT_OFF_THRESHOLD);
        Serial.printf("   🌊 Altura agua: %.1f cm (bomba OFF si >%.1f cm, ON si <%.1f cm)\n", 
                     state.averageReadings.waterLevel, WATER_LEVEL_MIN, WATER_LEVEL_RECOVERY);
        Serial.printf("   🌡️  Temperatura agua: %.1f°C (umbral: <%.1f°C=ON, >%.1f°C=OFF)\n", 
                     state.averageReadings.waterTemp, WATER_TEMP_MIN, WATER_TEMP_MAX - WATER_TEMP_HYSTERESIS);
        Serial.printf("   🧪 pH: %.2f (óptimo: %.1f-%.1f)\n", 
                     state.averageReadings.ph, PH_MIN, PH_MAX);
        Serial.printf("   ⚡ EC: %.2f mS/cm (óptimo: %.1f-%.1f mS/cm)\n", 
                     state.averageReadings.tds, EC_MIN, EC_MAX);
    }
}

void AutonomousController::printActuatorStates() {
    Serial.printf("🔧 [CTRL] Estados actuadores:\n");
    Serial.printf("   🔥 Calefactor aire: %s\n", state.heater.isOn ? "ON" : "OFF");
    Serial.printf("   🌡️  Calefactor agua: %s %s\n", state.waterHeater.isOn ? "ON" : "OFF",
                  state.waterHeater.forceOff ? "(BLOQUEADO)" : "");
    Serial.printf("   🌪️  Ventilador: %s\n", state.fan.isOn ? "ON" : "OFF");
    Serial.printf("   💡 Luz: %s\n", state.light.isOn ? "ON" : "OFF");
    Serial.printf("   💨 Humidificador: maestro=%s\n", 
                  state.humidifierMasterActive ? "ON" : "OFF");
    Serial.printf("   🌊 Bomba agua: %s %s %s\n", state.waterPump.isOn ? "ON" : "OFF",
                  state.waterPump.forceOff ? "(BLOQUEADA)" : "",
                  state.pumpRunningForHeater ? "(RECIRCULANDO)" : "");
    
    // NUEVA INFORMACIÓN: Estado de coordinación de bomba de aire
    const char* airModeStr = (state.airStoneMode == AIR_MODE_AUTONOMOUS) ? "AUTONOMO" :
                            (state.airStoneMode == AIR_MODE_RECIRCULATION) ? "RECIRCULACION" : "INACTIVO";
    const char* airPhaseStr = "";
    if (state.airLeadActive) airPhaseStr = " (LEAD)";
    else if (state.airPostActive) airPhaseStr = " (POST)";
    else if (state.airPeriodicActive) airPhaseStr = " (PERIODICO)";
    
    Serial.printf("   💨 Difusor aire: %s %s - Modo: %s%s\n", 
                  state.airStone.isOn ? "ON" : "OFF",
                  state.airStone.forceOff ? "(BLOQUEADO)" : "",
                  airModeStr, airPhaseStr);
    
    if (state.airStoneMode != AIR_MODE_RECIRCULATION) {
        unsigned long currentMinutes = getCurrentMinutes();
        Serial.printf("       Próximo ciclo autónomo: minuto %lu (actual: %lu, en %lu min)\n", 
                      state.nextAutonomousAirTime, currentMinutes, 
                      (state.nextAutonomousAirTime > currentMinutes) ? 
                      (state.nextAutonomousAirTime - currentMinutes) : 0);
    }
}

void AutonomousController::printSystemStatus() {
    Serial.printf("🤖 [CTRL] Estado sistema:\n");
    Serial.printf("   Sistema habilitado: %s\n", state.systemEnabled ? "SÍ" : "NO");
    Serial.printf("   Nivel agua OK: %s\n", state.waterLevelOk ? "SÍ" : "NO");
    Serial.printf("   Lecturas válidas: %d/%d\n", state.readingCount, MAX_READINGS);
    
    if (timeClient->isTimeSet()) {
        int hour = (timeClient->getHours() - 5 + 24) % 24;  // UTC-5 Colombia
        Serial.printf("   Hora local: %02d:%02d (luz %s)\n", 
                      hour, timeClient->getMinutes(),
                      isLightScheduleActive() ? "ON" : "OFF");
    }
}

// ========================================
// FUNCIONES DE CONTROL SIMPLIFICADAS CON HISTÉRESIS Y REPOSO
// ========================================

void AutonomousController::initializeVariableConfigs() {
    Serial.println("🔧 [CTRL] Inicializando configuraciones de variables de control");
    
    // Temperatura aire - Con actuadores (calefactor/ventilador)
    state.tempConfig = {
        .minValue = TEMP_MIN,
        .maxValue = TEMP_MAX,
        .hysteresis = TEMP_HYSTERESIS,
        .restTimeMs = TEMP_REST_TIME_MS,
        .hasActuator = true
    };
    
    // Humedad aire - Con actuador (humidificador)
    state.humidityConfig = {
        .minValue = HUMIDITY_MIN,
        .maxValue = HUMIDITY_MAX,
        .hysteresis = HUMIDITY_HYSTERESIS,
        .restTimeMs = HUMIDITY_REST_TIME_MS,
        .hasActuator = true
    };
    
    // Temperatura agua - Con actuador (calefactor agua)
    state.waterTempConfig = {
        .minValue = WATER_TEMP_MIN,
        .maxValue = WATER_TEMP_MAX,
        .hysteresis = WATER_TEMP_HYSTERESIS,
        .restTimeMs = WATER_TEMP_REST_TIME_MS,
        .hasActuator = true
    };
    
    // pH - Solo monitoreo (sin actuadores)
    state.phConfig = {
        .minValue = PH_MIN,
        .maxValue = PH_MAX,
        .hysteresis = PH_HYSTERESIS,
        .restTimeMs = 0,         // No tiene tiempo de reposo
        .hasActuator = false
    };
    
    // EC - Solo monitoreo (sin actuadores)
    state.ecConfig = {
        .minValue = EC_MIN,
        .maxValue = EC_MAX,
        .hysteresis = EC_HYSTERESIS,
        .restTimeMs = 0,         // No tiene tiempo de reposo
        .hasActuator = false
    };
    
    // Nivel de agua - Fail-safe para proteger bomba
    state.waterLevelConfig = {
        .minValue = WATER_LEVEL_RECOVERY,  // Nivel para permitir bomba (< 6cm)
        .maxValue = WATER_LEVEL_MIN,       // Nivel para apagar bomba (> 7cm)
        .hysteresis = 0.5f,                // 0.5cm de histéresis
        .restTimeMs = WATER_LEVEL_REST_TIME_MS,
        .hasActuator = true
    };
    
    Serial.println("✅ [CTRL] Configuraciones inicializadas - Control directo con histéresis y tiempo de reposo");
}

// FUNCIÓN ELIMINADA: evaluateDebounce()
// Ya no se necesita - control directo con histéresis es más simple y efectivo

bool AutonomousController::canActivateAfterRest(const ActuatorState& actuator, unsigned long restTime) {
    if (restTime == 0) return true;  // Sin restricción de reposo
    
    unsigned long now = millis();
    
    // Si nunca se ha apagado, puede activarse
    if (actuator.lastRestTime == 0) return true;
    
    // Verificar tiempo de reposo desde último apagado
    return (now - actuator.lastRestTime) >= restTime;
}

// FUNCIÓN OBSOLETA: controlGenericVariable() 
// Esta función fue reemplazada por lógica específica para cada actuador
// porque el debounce genérico causaba problemas con calefactores y humidificadores
bool AutonomousController::controlGenericVariable(float sensorValue, const VariableConfig& config, ActuatorState& actuator, const char* name) {
    if (!config.hasActuator) {
        // Solo monitoreo para pH/EC - esta parte sigue siendo útil
        bool inRange = (sensorValue >= config.minValue && sensorValue <= config.maxValue);
        logVariableStatus(name, sensorValue, config, inRange);
        return true;
    }
    
    Serial.printf("⚠️ [CTRL] ADVERTENCIA: controlGenericVariable() está obsoleta para %s\n", name);
    Serial.printf("     Usar lógica específica en control[Variable]() en su lugar\n");
    
    // Solo permitir uso para monitoreo (pH/EC)
    return false;
}

// FUNCIÓN ELIMINADA: checkWaterLevelFailSafe()
// Toda la lógica de nivel de agua ahora está en checkWaterLevelSafety()

void AutonomousController::logVariableStatus(const char* name, float value, const VariableConfig& config, bool inRange) {
    const char* status = inRange ? "✅" : "⚠️";
    Serial.printf("📊 [CTRL] %s: %.2f %s (rango óptimo: %.1f-%.1f)\n", 
                  name, value, status, config.minValue, config.maxValue);
}

// ========================================
// NUEVAS FUNCIONES DE COORDINACIÓN DE BOMBAS
// ========================================

void AutonomousController::setAirStoneMode(AirStoneMode newMode, const char* reason) {
    if (state.airStoneMode != newMode) {
        const char* oldMode = (state.airStoneMode == AIR_MODE_AUTONOMOUS) ? "AUTONOMO" :
                             (state.airStoneMode == AIR_MODE_RECIRCULATION) ? "RECIRCULACION" : "INACTIVO";
        const char* newModeStr = (newMode == AIR_MODE_AUTONOMOUS) ? "AUTONOMO" :
                                (newMode == AIR_MODE_RECIRCULATION) ? "RECIRCULACION" : "INACTIVO";
        
        Serial.printf("🔄 [AIR-COORD] Cambio modo: %s → %s (%s)\n", oldMode, newModeStr, reason);
        state.airStoneMode = newMode;
        
        // Reset de estados al cambiar modo
        if (newMode == AIR_MODE_AUTONOMOUS) {
            state.airPeriodicActive = false;
            calculateNextAutonomousAirTime();
        } else if (newMode == AIR_MODE_RECIRCULATION) {
            state.airPeriodicActive = false;
        }
    }
}

void AutonomousController::calculateNextAutonomousAirTime() {
    unsigned long currentMinutes = getCurrentMinutes();
    unsigned long currentMinutesInHour = currentMinutes % 60;
    
    // Calcular próximo horario de 30 minutos: XX:00 o XX:30
    unsigned long nextMinutesInHour;
    if (currentMinutesInHour < 30) {
        nextMinutesInHour = 30;
    } else {
        nextMinutesInHour = 60; // Siguiente hora XX:00
    }
    
    // Calcular tiempo absoluto del próximo ciclo
    unsigned long hoursSinceMidnight = currentMinutes / 60;
    if (nextMinutesInHour == 60) {
        hoursSinceMidnight++;
        nextMinutesInHour = 0;
    }
    
    state.nextAutonomousAirTime = (hoursSinceMidnight * 60) + nextMinutesInHour;
    
    Serial.printf("📅 [AIR-COORD] Próximo aire autónomo programado para minuto %lu (actual: %lu)\n", 
                  state.nextAutonomousAirTime, currentMinutes);
}

bool AutonomousController::shouldActivateAutonomousAir() {
    if (state.airStoneMode != AIR_MODE_AUTONOMOUS) return false;
    
    unsigned long currentMinutes = getCurrentMinutes();
    
    // Verificar si es hora exacta del ciclo autónomo programado
    bool isTimeForAir = (currentMinutes >= state.nextAutonomousAirTime && 
                        currentMinutes <= state.nextAutonomousAirTime + 3); // Ventana de 3 min
    
    return isTimeForAir && !state.airPeriodicActive;
}

void AutonomousController::handleAirStoneCoordination() {
    unsigned long now = millis();
    
    // PRIORIDAD 1: Verificar si la recirculación debe tomar control
    if (state.pumpCycleActive && state.airStoneMode != AIR_MODE_RECIRCULATION) {
        setAirStoneMode(AIR_MODE_RECIRCULATION, "recirculación iniciada");
        return;
    }
    
    // PRIORIDAD 2: Liberar control cuando termine la recirculación
    if (!state.pumpCycleActive && state.airStoneMode == AIR_MODE_RECIRCULATION) {
        setAirStoneMode(AIR_MODE_IDLE, "recirculación completada");
        // Calcular próximo horario autónomo considerando el tiempo que ya pasó
        calculateNextAutonomousAirTime();
        return;
    }
    
    // MODO AUTÓNOMO: Verificar si debe activarse el ciclo periódico
    if (state.airStoneMode == AIR_MODE_AUTONOMOUS || state.airStoneMode == AIR_MODE_IDLE) {
        if (shouldActivateAutonomousAir()) {
            setAirStoneMode(AIR_MODE_AUTONOMOUS, "horario ciclo autónomo");
            state.airPeriodicActive = true;
            state.airPeriodicStartTime = now;
            setActuatorState(state.airStone, true, "difusor");
            logAirStoneAction("ACTIVADO", "AUTONOMO", "ciclo periódico 5min");
            return;
        }
        
        // Desactivar aire autónomo después de 5 minutos
        if (state.airPeriodicActive && (now - state.airPeriodicStartTime >= AIR_PERIODIC_ON_MS)) {
            state.airPeriodicActive = false;
            setActuatorState(state.airStone, false, "difusor");
            logAirStoneAction("DESACTIVADO", "AUTONOMO", "ciclo periódico completado");
            calculateNextAutonomousAirTime();
            setAirStoneMode(AIR_MODE_IDLE, "esperando próximo ciclo");
        }
    }
}

void AutonomousController::logAirStoneAction(const char* action, const char* mode, const char* reason) {
    Serial.printf("💨 [AIR-COORD] %s - Modo: %s - Razón: %s\n", action, mode, reason);
}