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
    
    // Inicializar timestamps
    unsigned long now = millis();
    state.lastControlCycle = now;
    state.lastPumpCycle = now;
    state.lastAirStoneCycle = now;
    
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
                  LIGHT_MIN, LIGHT_MAX, LIGHT_START_HOUR, LIGHT_END_HOUR);
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
            runPeriodicRoutines();
            
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
    reading.waterLevel = sensors->readWaterLevel();
    reading.waterTemp = sensors->readTankTemperature();
    reading.ph = sensors->readPH();
    reading.tds = sensors->readTDS();
    
    // Validar que las lecturas críticas sean válidas
    reading.valid = !isnan(reading.temperature) && 
                   !isnan(reading.humidity) && 
                   !isnan(reading.lightIndex) && 
                   !isnan(reading.waterLevel);
    
    Serial.printf("📊 [CTRL] Lectura %d: T=%.1f°C H=%.1f%% L=%.1f%% N=%.1fcm %s\n",
                  state.readingIndex, reading.temperature, reading.humidity, 
                  reading.lightIndex, reading.waterLevel,
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
    int validCount = 0;
    
    // Calcular promedios solo de lecturas válidas
    for (int i = 0; i < state.readingCount; i++) {
        const SensorReadings& reading = state.readings[i];
        if (reading.valid) {
            tempSum += reading.temperature;
            humidSum += reading.humidity;
            lightSum += reading.lightIndex;
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
        state.averageReadings.lightIndex = lightSum / validCount;
        state.averageReadings.waterLevel = levelSum / validCount;
        state.averageReadings.waterTemp = waterTempSum / validCount;
        state.averageReadings.ph = phSum / validCount;
        state.averageReadings.tds = tdsSum / validCount;
        state.averageReadings.valid = true;
        state.averageReadings.timestamp = millis();
        
        Serial.printf("📊 [CTRL] Promedios (%d lecturas): T=%.1f°C H=%.1f%% L=%.1f%% N=%.1fcm\n",
                      validCount, state.averageReadings.temperature, 
                      state.averageReadings.humidity, state.averageReadings.lightIndex,
                      state.averageReadings.waterLevel);
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
    
    bool previousWaterOk = state.waterLevelOk;
    state.waterLevelOk = state.averageReadings.waterLevel >= WATER_LEVEL_MIN;
    
    if (!state.waterLevelOk) {
        if (previousWaterOk) {
            logSafetyAction("Nivel agua crítico - bloqueando bomba y difusor");
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
            logSafetyAction("Nivel agua recuperado - habilitando operación normal");
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
    bool heaterShouldBeOn = false;
    bool fanShouldBeOn = false;
    
    // Lógica con histeresis
    if (state.heater.isOn) {
        // Calefactor encendido: apagar si T > MIN + histeresis
        heaterShouldBeOn = temp <= (TEMP_MIN + TEMP_HYSTERESIS);
    } else {
        // Calefactor apagado: encender si T < MIN
        heaterShouldBeOn = temp < TEMP_MIN;
    }
    
    if (state.fan.isOn) {
        // Ventilador encendido: apagar si T < MAX - histeresis
        fanShouldBeOn = temp >= (TEMP_MAX - TEMP_HYSTERESIS);
    } else {
        // Ventilador apagado: encender si T > MAX
        fanShouldBeOn = temp > TEMP_MAX;
    }
    
    // Aplicar cambios respetando tiempos mínimos
    if (heaterShouldBeOn != state.heater.isOn) {
        if (hasMinimumTimePassed(state.heater, HEATER_MIN_ON_TIME)) {
            setActuatorState(state.heater, heaterShouldBeOn, "calefactor");
            logControlDecision("Temperatura", temp, heaterShouldBeOn ? "calefactor ON" : "calefactor OFF");
        } else {
            Serial.printf("⏳ [CTRL] Calefactor bloqueado por tiempo mínimo (%.1f°C < %.1f°C)\n", 
                         temp, TEMP_MIN);
        }
    }
    
    if (fanShouldBeOn != state.fan.isOn) {
        if (hasMinimumTimePassed(state.fan, FAN_MIN_ON_TIME)) {
            setActuatorState(state.fan, fanShouldBeOn, "ventilador");
            logControlDecision("Temperatura", temp, fanShouldBeOn ? "ventilador ON" : "ventilador OFF");
        }
    }
}

void AutonomousController::controlHumidity() {
    if (!state.averageReadings.valid) return;
    
    float humidity = state.averageReadings.humidity;
    bool humidifierShouldBeOn = false;
    
    // Lógica con histeresis
    if (state.humidifier.isOn) {
        // Humidificador activo: parar si H > MAX - histeresis
        humidifierShouldBeOn = humidity < (HUMIDITY_MAX - HUMIDITY_HYSTERESIS);
    } else {
        // Humidificador inactivo: activar si H < MIN
        humidifierShouldBeOn = humidity < HUMIDITY_MIN;
    }
    
    if (humidifierShouldBeOn != state.humidifier.isOn) {
        setActuatorState(state.humidifier, humidifierShouldBeOn, "humidificador");
        logControlDecision("Humedad", humidity, 
                          humidifierShouldBeOn ? "humidificador rutina ON" : "humidificador OFF");
    }
}

void AutonomousController::controlWaterTemperature() {
    if (!state.averageReadings.valid) return;
    
    float waterTemp = state.averageReadings.waterTemp;
    bool waterHeaterShouldBeOn = false;
    bool pumpNeededForHeating = false;
    
    // Solo actuar si hay agua suficiente para evitar quemar la resistencia
    if (!state.waterLevelOk) {
        // Forzar apagado por seguridad
        if (state.waterHeater.isOn) {
            setActuatorState(state.waterHeater, false, "calefactor agua");
            logSafetyAction("Calefactor agua apagado - nivel agua insuficiente");
        }
        state.waterHeater.forceOff = true;
        return;
    } else {
        state.waterHeater.forceOff = false;
    }
    
    // Lógica con histeresis para calefactor agua
    if (state.waterHeater.isOn) {
        // Calefactor encendido: apagar si T > MAX - histeresis
        waterHeaterShouldBeOn = waterTemp < (WATER_TEMP_MAX - WATER_TEMP_HYSTERESIS);
    } else {
        // Calefactor apagado: encender si T < MIN
        waterHeaterShouldBeOn = waterTemp < WATER_TEMP_MIN;
    }
    
    // Aplicar cambios respetando tiempo mínimo
    if (waterHeaterShouldBeOn != state.waterHeater.isOn) {
        if (hasMinimumTimePassed(state.waterHeater, WATER_HEATER_MIN_ON_TIME)) {
            setActuatorState(state.waterHeater, waterHeaterShouldBeOn, "calefactor agua");
            logControlDecision("Temperatura agua", waterTemp, 
                              waterHeaterShouldBeOn ? "calefactor agua ON" : "calefactor agua OFF");
            
            if (waterHeaterShouldBeOn) {
                state.waterHeaterStartTime = millis();
                state.pumpRunningForHeater = false;  // Resetear estado bomba
            }
        } else {
            Serial.printf("⏳ [CTRL] Calefactor agua bloqueado por tiempo mínimo (%.1f°C < %.1f°C)\n", 
                         waterTemp, WATER_TEMP_MIN);
        }
    }
    
    // Coordinar bomba con calefactor para recirculación
    if (state.waterHeater.isOn) {
        pumpNeededForHeating = true;
        
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
    if (!state.averageReadings.valid) return;
    
    float light = state.averageReadings.lightIndex;
    bool lightShouldBeOn = false;
    bool inSchedule = isLightScheduleActive();
    
    if (!inSchedule) {
        // Fuera de horario: siempre OFF
        lightShouldBeOn = false;
    } else {
        // Dentro de horario: aplicar control por luz
        if (state.light.isOn) {
            // Luz encendida: apagar si light > MAX - histeresis
            lightShouldBeOn = light < (LIGHT_MAX - LIGHT_HYSTERESIS);
        } else {
            // Luz apagada: encender si light < MIN
            lightShouldBeOn = light < LIGHT_MIN;
        }
    }
    
    if (lightShouldBeOn != state.light.isOn) {
        if (hasMinimumTimePassed(state.light, LIGHT_MIN_TIME)) {
            setActuatorState(state.light, lightShouldBeOn, "luz");
            
            if (!inSchedule) {
                logControlDecision("Horario", 0, "luz OFF (fuera de horario)");
            } else {
                logControlDecision("Luz", light, lightShouldBeOn ? "luz ON" : "luz OFF");
            }
        }
    }
}

void AutonomousController::runPeriodicRoutines() {
    unsigned long now = millis();
    
    // Solo ejecutar si nivel de agua OK
    if (!state.waterLevelOk) return;
    
    // Rutina de bomba cada 4 horas
    if (now - state.lastPumpCycle >= PUMP_CYCLE_TIME) {
        if (!state.waterPump.isOn) {
            setActuatorState(state.waterPump, true, "bomba");
            state.pumpStartTime = now;
            state.lastPumpCycle = now;
            logRoutineAction("Bomba 4h", "ON por 2 minutos");
        }
    }
    
    // Apagar bomba después de 2 minutos
    if (state.waterPump.isOn && (now - state.pumpStartTime >= PUMP_ON_TIME)) {
        setActuatorState(state.waterPump, false, "bomba");
        logRoutineAction("Bomba 4h", "OFF - ciclo completado");
    }
    
    // Rutina de difusor cada 1 hora
    if (now - state.lastAirStoneCycle >= AIRSTONE_CYCLE_TIME) {
        if (!state.airStone.isOn) {
            setActuatorState(state.airStone, true, "difusor");
            state.airStoneStartTime = now;
            state.lastAirStoneCycle = now;
            logRoutineAction("Difusor 1h", "ON por 3 minutos");
        }
    }
    
    // Apagar difusor después de 3 minutos
    if (state.airStone.isOn && (now - state.airStoneStartTime >= AIRSTONE_ON_TIME)) {
        setActuatorState(state.airStone, false, "difusor");
        logRoutineAction("Difusor 1h", "OFF - ciclo completado");
    }
    
    // Difusor acompañando bomba (1 min antes, 2 min después)
    bool pumpWillStart = (now - state.lastPumpCycle >= (PUMP_CYCLE_TIME - AIRSTONE_PUMP_LEAD));
    bool pumpRecentlyFinished = state.waterPump.isOn || 
                               (now - (state.pumpStartTime + PUMP_ON_TIME) <= AIRSTONE_PUMP_FOLLOW);
    
    if ((pumpWillStart || pumpRecentlyFinished) && !state.airStone.isOn) {
        // Solo activar si no está ya en rutina de 1h
        if (now - state.airStoneStartTime > AIRSTONE_ON_TIME) {
            setActuatorState(state.airStone, true, "difusor");
            logRoutineAction("Difusor bomba", "ON - acompañando bomba");
        }
    }
}

// ========================================
// FUNCIONES AUXILIARES
// ========================================

bool AutonomousController::isLightScheduleActive() {
    if (!timeClient->isTimeSet()) {
        // Modo seguro sin NTP: permitir control de luz basado solo en sensores
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
    
    if (state.humidifier.isOn) {
        // Ejecutar rutina completa del humidificador (2 relés)
        ActuatorController::runHumidifierRoutine(60000);  // 1 minuto
        state.humidifier.isOn = false;  // Se ejecuta una vez y termina
    }
    
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
        Serial.printf("   🌡️  Temperatura aire: %.1f°C (umbral: <%.1f°C=ON, >%.1f°C=OFF)\n", 
                     state.averageReadings.temperature, TEMP_MIN, TEMP_MIN + TEMP_HYSTERESIS);
        Serial.printf("   💧 Humedad aire: %.1f%% (umbral: <%.1f%%=ON, >%.1f%%=OFF)\n", 
                     state.averageReadings.humidity, HUMIDITY_MIN, HUMIDITY_MAX);
        Serial.printf("   💡 Índice luz: %.1f%% (umbral: <%.1f%%=ON, >%.1f%%=OFF)\n", 
                     state.averageReadings.lightIndex, LIGHT_MIN, LIGHT_MAX);
        Serial.printf("   🌊 Nivel agua: %.1f cm (mínimo crítico: >%.1f cm)\n", 
                     state.averageReadings.waterLevel, WATER_LEVEL_MIN);
        Serial.printf("   🌡️  Temperatura agua: %.1f°C (umbral: <%.1f°C=ON, >%.1f°C=OFF)\n", 
                     state.averageReadings.waterTemp, WATER_TEMP_MIN, WATER_TEMP_MAX - WATER_TEMP_HYSTERESIS);
        Serial.printf("   🧪 pH: %.2f (óptimo: %.1f-%.1f)\n", 
                     state.averageReadings.ph, PH_MIN, PH_MAX);
        Serial.printf("   ⚡ TDS: %.1f ppm (óptimo: %.0f-%.0f ppm)\n", 
                     state.averageReadings.tds, TDS_MIN, TDS_MAX);
    }
}

void AutonomousController::printActuatorStates() {
    Serial.printf("🔧 [CTRL] Estados actuadores:\n");
    Serial.printf("   🔥 Calefactor aire: %s\n", state.heater.isOn ? "ON" : "OFF");
    Serial.printf("   🌡️  Calefactor agua: %s %s\n", state.waterHeater.isOn ? "ON" : "OFF",
                  state.waterHeater.forceOff ? "(BLOQUEADO)" : "");
    Serial.printf("   🌪️  Ventilador: %s\n", state.fan.isOn ? "ON" : "OFF");
    Serial.printf("   💡 Luz: %s\n", state.light.isOn ? "ON" : "OFF");
    Serial.printf("   💨 Humidificador: %s\n", state.humidifier.isOn ? "ACTIVO" : "OFF");
    Serial.printf("   🌊 Bomba agua: %s %s %s\n", state.waterPump.isOn ? "ON" : "OFF",
                  state.waterPump.forceOff ? "(BLOQUEADA)" : "",
                  state.pumpRunningForHeater ? "(RECIRCULANDO)" : "");
    Serial.printf("   💨 Difusor aire: %s %s\n", state.airStone.isOn ? "ON" : "OFF",
                  state.airStone.forceOff ? "(BLOQUEADO)" : "");
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