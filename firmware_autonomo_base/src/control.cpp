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
    
    // NUEVA LÓGICA: Inicializar cronogramas independientes
    state.airStoneMode = AIR_MODE_IDLE;              // Modo inicial: Idle
    state.recirculationActive = false;               // Recirculación inactiva
    state.autonomousAirActive = false;               // Aireación autónoma inactiva
    
    // ========================================
    // INICIALIZAR RECIRCULACIÓN EXTRA POR CALEFACTOR DE AGUA
    // ========================================
    state.extraRecirculationActive = false;
    state.extraRecirculationStartTime = 0;
    state.lastExtraRecirculationTime = 0;            // Nunca se ha ejecutado
    state.lastWaterHeaterState = false;              // Calefactor inicialmente apagado
    
    // Inicializar cronogramas
    initializeRecirculationSchedule();
    initializeAutonomousAirSchedule();
    
    // NUEVA LÓGICA: Inicializar máquina de estados de luz artificial
    state.lightState = LIGHT_OFF;
    state.lightOnStartTime = 0;
    
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
    Serial.printf("   💡 Luz: %.0f-%.0f lux (horario %d:00-%d:00, %dh diarias)\n",
                  LUX_ON_THRESHOLD, LUX_OFF_THRESHOLD, LIGHT_START_HOUR, LIGHT_END_HOUR, REQUIRED_LIGHT_HOURS);
    Serial.printf("   🌊 Distancia máxima: %.1f cm (bomba OFF si mayor)\n", WATER_DISTANCE_MAX);
}

// ========================================
// BUCLE PRINCIPAL
// ========================================

void AutonomousController::loop() {
    unsigned long now = millis();
    
    // ========================================
    // CRONOGRAMAS INDEPENDIENTES - VALIDACIÓN INMEDIATA
    // ========================================
    // IMPORTANTE: Estos cronogramas son independientes del ciclo de control
    // Se validan cada loop() para máxima precisión temporal
    runIndependentRoutines();
    
    // ========================================
    // CICLO DE CONTROL DE SENSORES (cada 4 min)
    // ========================================
    
    // Actualizar lecturas de sensores cada 2 minutos
    static unsigned long lastSensorUpdate = 0;
    if (now - lastSensorUpdate >= SENSOR_READING_INTERVAL) {
        updateSensorReadings();
        lastSensorUpdate = now;
    }
    
    // Ejecutar control de sensores cada 4 minutos (con 2 lecturas promediadas)
    if (shouldExecuteControl()) {
        Serial.println("⚡ [CTRL] ═══ CICLO DE CONTROL INICIADO ═══");
        
        calculateAverages();
        printSensorAverages();
        
        // Verificar reglas de seguridad primero
        checkWaterLevelSafety();
        
        if (state.systemEnabled) {
            // Ejecutar reglas de control basadas en sensores
            controlTemperature();
            controlWaterTemperature();
            controlHumidity();
            controlLight();
            
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

    // Leer todos los sensores con yields para evitar watchdog timeout
    reading.temperature = sensors->readTemperature();
    yield(); // Ceder control al watchdog

    reading.humidity = sensors->readHumidity();
    yield(); // Ceder control al watchdog

    reading.lightLux = sensors->readLightLux();
    yield(); // Ceder control al watchdog

    // Leer nivel de agua (ya calculado internamente como altura_tanque - distancia)
    reading.waterLevel = sensors->readWaterLevel();
    yield(); // Ceder control al watchdog después de múltiples lecturas ultrasónicas

    reading.waterTemp = sensors->readTankTemperature();
    yield(); // Ceder control al watchdog

    reading.ph = sensors->readPH();
    yield(); // Ceder control al watchdog

    reading.tds = sensors->readTDS();
    yield(); // Ceder control al watchdog

    // Validar que las lecturas críticas sean válidas
    reading.valid = !isnan(reading.temperature) &&
                   !isnan(reading.humidity) &&
                   !isnan(reading.waterLevel);

    Serial.printf("📊 [CTRL] Lectura %d: T=%.1f°C H=%.1f%% Lux=%.0f N=%.1fcm %s\n",
                  state.readingIndex, reading.temperature, reading.humidity,
                  reading.lightLux, reading.waterLevel,
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
    float tempSum = 0, humidSum = 0, luxSum = 0, levelSum = 0;
    float waterTempSum = 0, phSum = 0, tdsSum = 0;
    int validCount = 0;
    int luxValidCount = 0;  // Contador específico para lux

    // Calcular promedios solo de lecturas válidas
    for (int i = 0; i < state.readingCount; i++) {
        const SensorReadings& reading = state.readings[i];
        if (reading.valid) {
            tempSum += reading.temperature;
            humidSum += reading.humidity;

            // lux puede ser NAN - manejar por separado
            if (!isnan(reading.lightLux)) {
                luxSum += reading.lightLux;
                luxValidCount++;
            }

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

        // lux promedio solo si hay lecturas válidas, sino NAN
        if (luxValidCount > 0) {
            state.averageReadings.lightLux = luxSum / luxValidCount;
        } else {
            state.averageReadings.lightLux = NAN;
        }

        state.averageReadings.waterLevel = levelSum / validCount;
        state.averageReadings.waterTemp = waterTempSum / validCount;
        state.averageReadings.ph = phSum / validCount;
        state.averageReadings.tds = tdsSum / validCount;
        state.averageReadings.valid = true;
        state.averageReadings.timestamp = millis();

        Serial.printf("📊 [CTRL] Promedios (%d lecturas): T=%.1f°C H=%.1f%% Lux=%s N=%.1fcm\n",
                      validCount, state.averageReadings.temperature,
                      state.averageReadings.humidity,
                      luxValidCount > 0 ? (String(state.averageReadings.lightLux, 0) + " lux").c_str() : "N/A",
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
    
    float waterDistance = state.averageReadings.waterLevel;  // Distancia en cm del sensor ultrasónico
    bool previousWaterOk = state.waterLevelOk;

    // LÓGICA: Sensor montado ARRIBA midiendo hacia abajo con histéresis
    // Distancia GRANDE (>7cm) = Tanque VACÍO → Bloquear bomba
    // Distancia PEQUEÑA (<6cm) = Tanque LLENO → Permitir bomba
    if (state.waterLevelOk) {
        // Bomba permitida: bloquear si distancia > 7cm (tanque vacío)
        state.waterLevelOk = waterDistance <= WATER_DISTANCE_MAX;
    } else {
        // Bomba bloqueada: permitir solo si distancia < 6cm (tanque lleno)
        state.waterLevelOk = waterDistance < WATER_DISTANCE_RECOVERY;
    }

    if (!state.waterLevelOk) {
        if (previousWaterOk) {
            logSafetyAction("Distancia ALTA - tanque vacío - bloqueando bomba y difusor");
            Serial.printf("🚨 [DISTANCIA] %.1fcm > %.1fcm (vacío) - BOMBA OFF\n",
                         waterDistance, WATER_DISTANCE_MAX);
        }

        // Forzar apagado de bomba y difusor
        state.waterPump.forceOff = true;
        state.airStone.forceOff = true;

        // Apagar inmediatamente si están encendidos (SEGURIDAD CRÍTICA)
        if (state.waterPump.isOn) {
            setActuatorStateImmediate(state.waterPump, false, "bomba");
        }
        if (state.airStone.isOn) {
            setActuatorStateImmediate(state.airStone, false, "difusor");
        }

        // Asegurar que relé maestro humidificador esté OFF en modo seguro
        ActuatorController::turnHumidifierMasterOff();
    } else {
        if (!previousWaterOk) {
            logSafetyAction("Distancia BAJA - tanque lleno - habilitando bomba");
            Serial.printf("✅ [DISTANCIA] %.1fcm < %.1fcm (lleno) - BOMBA habilitada\n",
                         waterDistance, WATER_DISTANCE_RECOVERY);
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
    
    // ========================================
    // CALEFACTOR AIRE CON PARCHE DE SEGURIDAD
    // ========================================
    if (state.heater.isOn) {
        // Calefactor encendido: usar nuevos umbrales de seguridad
        if (temp >= HEATER_OFF_TEMP && canTurnHeaterOff()) {
            setActuatorState(state.heater, false, "calefactor_aire");
            state.heaterOffStartTime = millis();
            state.heaterFastMonitoringActive = false;  // Detener monitoreo rápido

            // ⚠️ SINCRONIZACIÓN DESHABILITADA (parche temporal - relé defectuoso)
            // if (state.humidifierMasterActive) {
            //     state.humidifierMasterActive = false;
            //     state.humidifier.isOn = false;
            //     state.windowValidationActive = false;
            //     ActuatorController::turnHumidifierMasterOff();
            //     ActuatorController::turnHumidifierRelayOff();
            //     Serial.println("💧 [CTRL] Humidificador APAGADO - sincronizado con calefactor OFF");
            // }

            logControlDecision("Temperatura", temp, "calefactor aire OFF - umbral seguridad 22.5°C");
        }
    } else {
        // Calefactor apagado: usar nuevos umbrales de seguridad
        if (temp <= HEATER_ON_TEMP && canTurnHeaterOn()) {
            setActuatorState(state.heater, true, "calefactor_aire");
            state.heaterOnStartTime = millis();
            state.heaterFastMonitoringActive = true;   // Iniciar monitoreo rápido
            state.lastHeaterMonitorTime = millis();
            // Inicializar buffer de temperaturas
            state.heaterTempBufferIndex = 0;
            state.heaterTempBufferFull = false;

            // ⚠️ SINCRONIZACIÓN DESHABILITADA (parche temporal - relé defectuoso)
            // unsigned long now = millis();
            // if (state.waterLevelOk && !state.humidifierLocked && !state.humidifierMasterActive) {
            //     float humidity = state.averageReadings.humidity;
            //     if (!isnan(humidity) && humidity <= HUMIDITY_MAX) {
            //         state.humidifierMasterActive = true;
            //         state.humidifierStartTime = now;
            //         state.humidityAtStart = humidity;
            //         state.temperatureAtStart = temp;
            //         state.humidifierWindowStart = now;
            //         state.windowValidationActive = true;
            //         ActuatorController::turnHumidifierMasterOn();
            //         delay(2000);
            //         ActuatorController::turnHumidifierRelayOn();
            //         delay(1000);
            //         ActuatorController::turnHumidifierRelayOff();
            //         Serial.printf("💧 [CTRL] Humidificador ACTIVADO - sincronizado con calefactor ON (H=%.1f%%)\n", humidity);
            //     }
            // }

            logControlDecision("Temperatura", temp, "calefactor aire ON - umbral seguridad 17.0°C - MONITOREO RÁPIDO INICIADO");
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
    // ⚠️ PARCHE TEMPORAL: HUMIDIFICADOR DESHABILITADO
    // Relé maestro del humidificador (PIN 14) está defectuoso y produce zumbido
    // TODO: Remover este return cuando se reemplace el relé
    return;

    if (!state.averageReadings.valid) return;

    float humidity = state.averageReadings.humidity;
    float temperature = state.averageReadings.temperature;
    unsigned long now = millis();

    // ===============================================
    // PASO 1: VERIFICAR FAIL-SAFE POR NIVEL DE AGUA
    // ===============================================
    if (!state.waterLevelOk && state.humidifierMasterActive) {
        state.humidifierMasterActive = false;
        state.windowValidationActive = false;  // Cancelar validación
        ActuatorController::turnHumidifierMasterOff();
        ActuatorController::turnHumidifierRelayOff();
        logSafetyAction("Humidificador APAGADO por nivel agua alto (tanque vacío)");
        return;
    }

    // ===============================================
    // PASO 2: VERIFICAR BLOQUEO POR INEFECTIVIDAD
    // ===============================================
    if (state.humidifierLocked) {
        // Verificar si expiró el bloqueo
        if (now >= state.humidifierLockedUntil) {
            state.humidifierLocked = false;
            Serial.printf("🔓 [HUMID] Bloqueo por inefectividad EXPIRADO - humidificador desbloqueado\n");
        } else {
            // Aún bloqueado - no permitir activación
            unsigned long remainingMs = state.humidifierLockedUntil - now;
            unsigned long remainingMin = remainingMs / 60000;
            if (humidity <= HUMIDITY_MIN - HUMIDITY_HYSTERESIS) {
                Serial.printf("🔒 [HUMID] Bloqueado por inefectividad - tiempo restante: %lu min (H=%.1f%% ≤ %.1f%%)\n",
                             remainingMin, humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);
            }
            return;
        }
    }

    // ===============================================
    // PASO 3: PROTECCIÓN POR TIEMPO MÁXIMO CONTINUO
    // ===============================================
    if (state.humidifierMasterActive) {
        unsigned long onTime = now - state.humidifierStartTime;

        // Verificar tiempo máximo continuo (9 minutos)
        if (onTime >= HUMID_MAX_ON_TIME_MS) {
            state.humidifierMasterActive = false;
            state.humidifier.isOn = false;
            state.humidifier.lastRestTime = now;
            state.windowValidationActive = false;  // Cancelar validación

            ActuatorController::turnHumidifierMasterOff();
            ActuatorController::turnHumidifierRelayOff();

            Serial.printf("⏱️ [HUMID] Tiempo máximo continuo alcanzado (9 min) - desactivado\n");
            Serial.printf("⏱️ [HUMID] Cooldown obligatorio: 2 minutos\n");
            logControlDecision("Humedad", humidity, "humidificador desactivado por tiempo máximo");
            return;
        }
    }

    // ===============================================
    // PASO 4: VALIDACIÓN DE EFECTIVIDAD (VENTANA DE 5 MIN)
    // ===============================================
    if (state.windowValidationActive) {
        unsigned long windowElapsed = now - state.humidifierWindowStart;

        // Detectar aumento de temperatura >2°C → resetear ventana
        float tempDelta = temperature - state.temperatureAtStart;
        if (tempDelta > HUMID_TEMP_RESET_THRESHOLD) {
            Serial.printf("🌡️ [HUMID] Temperatura aumentó %.1f°C (>%.1f°C) → reiniciando ventana de validación\n",
                         tempDelta, HUMID_TEMP_RESET_THRESHOLD);
            state.humidifierWindowStart = now;
            state.humidityAtStart = humidity;
            state.temperatureAtStart = temperature;
            Serial.printf("🔄 [HUMID] Nueva ventana iniciada: HR inicial=%.1f%%, T inicial=%.1f°C\n",
                         state.humidityAtStart, state.temperatureAtStart);
        }

        // Verificar si expiró la ventana de evaluación (5 minutos)
        if (windowElapsed >= (HUMID_WINDOW_MINUTES * 60000UL)) {
            float humidityDelta = humidity - state.humidityAtStart;

            if (humidityDelta < HUMID_MIN_DELTA) {
                // INEFECTIVO: No aumentó la humedad lo suficiente
                state.humidifierMasterActive = false;
                state.humidifier.isOn = false;
                state.humidifier.lastRestTime = now;
                state.windowValidationActive = false;

                // BLOQUEAR POR 2 HORAS
                state.humidifierLocked = true;
                state.humidifierLockedUntil = now + HUMID_LOCKOUT_DURATION_MS;

                ActuatorController::turnHumidifierMasterOff();
                ActuatorController::turnHumidifierRelayOff();

                Serial.printf("❌ [HUMID] INEFECTIVO: HR inicial=%.1f%%, HR actual=%.1f%%, delta=%.1f%% (<%.1f%% requerido)\n",
                             state.humidityAtStart, humidity, humidityDelta, HUMID_MIN_DELTA);
                Serial.printf("🔒 [HUMID] BLOQUEADO por 2 horas - sin agua o módulo defectuoso\n");
                logSafetyAction("Humidificador bloqueado por inefectividad - revisar nivel de agua en módulo");
                return;
            } else {
                // EFECTIVO: Aumentó la humedad correctamente
                Serial.printf("✅ [HUMID] EFECTIVO: HR inicial=%.1f%%, HR actual=%.1f%%, delta=%.1f%% (≥%.1f%% requerido)\n",
                             state.humidityAtStart, humidity, humidityDelta, HUMID_MIN_DELTA);
                Serial.printf("✅ [HUMID] Validación exitosa - continuando operación normal\n");
                state.windowValidationActive = false;  // Ventana cerrada exitosamente
            }
        }
    }

    // ===============================================
    // PASO 5: ACTIVACIÓN DIRECTA CON VALIDACIÓN
    // ===============================================
    if (humidity <= HUMIDITY_MIN - HUMIDITY_HYSTERESIS && !state.humidifierMasterActive) {
        // Verificar tiempo de reposo/cooldown (incluye cooldown de 2 min después de max time)
        unsigned long restTimeRequired = state.humidityConfig.restTimeMs;
        unsigned long timeSinceRest = now - state.humidifier.lastRestTime;

        // Si fue por tiempo máximo, forzar cooldown de 2 minutos
        if (timeSinceRest < HUMID_COOLDOWN_MS) {
            unsigned long cooldownRemaining = (HUMID_COOLDOWN_MS - timeSinceRest) / 1000;
            Serial.printf("⏳ [HUMID] Cooldown obligatorio - tiempo restante: %lu s (H=%.1f%% ≤ %.1f%%)\n",
                         cooldownRemaining, humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);
            return;
        }

        if (!canActivateAfterRest(state.humidifier, restTimeRequired)) {
            Serial.printf("⏳ [HUMID] Bloqueado por tiempo de reposo (H=%.1f%% ≤ %.1f%%)\n",
                         humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);
            return;
        }

        Serial.printf("💨 [HUMID] Iniciando secuencia completa humidificador (H=%.1f%% ≤ %.1f%%)\n",
                     humidity, HUMIDITY_MIN - HUMIDITY_HYSTERESIS);

        // Marcar como activo antes de comenzar la secuencia
        state.humidifierMasterActive = true;
        state.humidifierStartTime = now;
        state.humidifier.isOn = true;
        state.humidifier.lastChangeTime = now;

        // INICIAR VENTANA DE VALIDACIÓN
        state.windowValidationActive = true;
        state.humidifierWindowStart = now;
        state.humidityAtStart = humidity;
        state.temperatureAtStart = temperature;
        Serial.printf("🔍 [HUMID] Ventana de validación iniciada: HR inicial=%.1f%%, T inicial=%.1f°C\n",
                     state.humidityAtStart, state.temperatureAtStart);
        Serial.printf("🔍 [HUMID] Requiere incremento ≥%.1f%% en %d min (o reset si T sube >%.1f°C)\n",
                     HUMID_MIN_DELTA, HUMID_WINDOW_MINUTES, HUMID_TEMP_RESET_THRESHOLD);

        // 1. Activar relé maestro (PIN 14)
        ActuatorController::turnHumidifierMasterOn();
        Serial.println("💨 [HUMID] Relé maestro ON");

        // 2. Esperar delay de seguridad (2 segundos)
        delay(2000);

        // 3. Pulso en relé de activación (1 segundo ON, luego OFF)
        ActuatorController::turnHumidifierRelayOn();
        Serial.println("💨 [HUMID] Relé activación ON (pulso)");
        delay(1000);

        ActuatorController::turnHumidifierRelayOff();
        Serial.println("💨 [HUMID] Relé activación OFF - secuencia completa ejecutada");

        logControlDecision("Humedad", humidity, "humidificador activado con validación de efectividad");
        return;
    }

    // ===============================================
    // PASO 6: DESACTIVACIÓN CON HISTÉRESIS
    // ===============================================
    if (state.humidifierMasterActive && humidity >= HUMIDITY_MAX + HUMIDITY_HYSTERESIS) {
        state.humidifierMasterActive = false;
        state.humidifier.isOn = false;
        state.humidifier.lastRestTime = now;
        state.windowValidationActive = false;  // Cancelar validación si estaba activa

        ActuatorController::turnHumidifierMasterOff();
        ActuatorController::turnHumidifierRelayOff();

        Serial.printf("✅ [HUMID] Desactivado por humedad suficiente (H=%.1f%% ≥ %.1f%%)\n",
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
            onWaterHeaterStateChanged(false);  // Hook para detectar cambio de estado
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
        onWaterHeaterStateChanged(true);  // Hook para detectar cambio de estado
        logControlDecision("Temperatura agua", waterTemp, "calefactor agua ON - temperatura baja");
    }
    
    // Desactivar calefactor
    if (shouldDeactivate) {
        setActuatorState(state.waterHeater, false, "calefactor_agua");
        state.waterHeater.lastRestTime = millis();  // Guardar tiempo para reposo
        onWaterHeaterStateChanged(false);  // Hook para detectar cambio de estado (puede disparar recirculación extra)
        logControlDecision("Temperatura agua", waterTemp, "calefactor agua OFF - temperatura objetivo alcanzada");
    }
    
    // ELIMINADO: Coordinación bomba-calefactor
    // El calentador de agua funciona independientemente de la bomba
    // La bomba solo se activa por:
    // 1. Cronograma programado (cada 4h)
    // 2. Emergencia térmica (T_ambiente > 25°C)
    
    // El calentador de agua ahora funciona completamente independiente de la bomba
}

void AutonomousController::controlLight() {
    // Usar la nueva máquina de estados para control de luz
    handleLightStateMachine();
}

// NUEVA LÓGICA: Rutinas periódicas con horarios fijos (sin persistencia)
// ========================================
// RUTINAS PERIÓDICAS INDEPENDIENTES - NUEVA IMPLEMENTACIÓN
// ========================================

void AutonomousController::runIndependentRoutines() {
    // Solo ejecutar si nivel de agua OK
    if (!state.waterLevelOk) {
        // Evitar spam - solo log cada 30 segundos si agua insuficiente
        static unsigned long lastWaterWarning = 0;
        unsigned long now = millis();
        if (now - lastWaterWarning > 30000) {
            Serial.println("⚠️ [CRONOGRAMAS] Suspendidos por nivel de agua insuficiente");
            lastWaterWarning = now;
        }
        return;
    }
    
    // Ejecutar rutinas según prioridades (orden de máxima a mínima prioridad)
    handleThermalEmergency();     // PRIORIDAD MÁXIMA: Emergencia >25°C
    handleRecirculationRoutine(); // Prioridad 1: Recirculación cada 4h (independiente)
    handleExtraRecirculation();   // Prioridad 1.5: Recirculación extra por calefactor agua
    handleAutonomousAirRoutine(); // Prioridad 2: Aireación cada 30min (independiente)
    updateAirStoneControl();      // Controlador central de exclusión mutua
}

void AutonomousController::runPeriodicRoutines() {
    // FUNCIÓN LEGACY - redirigir a nueva implementación
    runIndependentRoutines();
}

// ========================================
// CRONOGRAMA INDEPENDIENTE DE RECIRCULACIÓN (cada 2h)
// ========================================

void AutonomousController::initializeRecirculationSchedule() {
    unsigned long currentMinutes = getCurrentMinutes();

    // Calcular próximo horario de recirculación (múltiplo de 2h desde medianoche)
    unsigned long hoursSinceMidnight = currentMinutes / 60;
    unsigned long nextHour = ((hoursSinceMidnight / 2) + 1) * 2; // Próximo múltiplo de 2
    if (nextHour >= 24) nextHour = 0; // Wrap around midnight

    state.nextRecirculationTime = nextHour * 60; // Convertir a minutos

    Serial.printf("🔄 [RECIRCULACIÓN] Próximo horario: %02ld:00 (%ld min desde medianoche)\n",
                  nextHour, state.nextRecirculationTime);
}

bool AutonomousController::isRecirculationScheduleTime() {
    if (state.recirculationActive) return false; // Ya está activa

    unsigned long currentMinutes = getCurrentMinutes();

    // CORREGIDO: Verificar si es hora de activar (amplia tolerancia para recuperarse)
    if (currentMinutes >= state.nextRecirculationTime) {
        // Si perdimos la ventana (más de 5 min tarde), recalcular próximo horario
        if (currentMinutes > state.nextRecirculationTime + 5) {
            Serial.printf("🌊 [RECIRCULACIÓN] Ventana perdida - recalculando desde minuto %lu\n", currentMinutes);

            // Recalcular basándose en múltiplos de 2h desde 00:00
            unsigned long hoursSinceMidnight = currentMinutes / 60;
            unsigned long nextHour = ((hoursSinceMidnight / 2) + 1) * 2; // Próximo múltiplo de 2
            nextHour = nextHour % 24; // Wrap 24h

            state.nextRecirculationTime = nextHour * 60;
            Serial.printf("🌊 [RECIRCULACIÓN] Nuevo próximo horario: %02ld:%02ld (minuto %ld)\n",
                          state.nextRecirculationTime / 60, state.nextRecirculationTime % 60,
                          state.nextRecirculationTime);

            return false; // No activar ahora, esperar al próximo ciclo
        }

        return true; // Dentro de ventana válida (0-5 min después)
    }

    return false; // Aún no es hora
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

void AutonomousController::handleRecirculationRoutine() {
    unsigned long now = millis();
    
    // INICIO: Verificar si debe comenzar el ciclo completo
    if (isRecirculationScheduleTime() && !state.recirculationActive) {
        state.recirculationActive = true;
        state.recirculationStartTime = now;
        state.airStoneMode = AIR_MODE_RECIRCULATION;
        
        // FASE 1: Activar aire lead (90s antes de bomba)
        state.recircAirLeadActive = true;
        state.recircAirLeadStartTime = now;
        setActuatorStateImmediate(state.airStone, true, "difusor");
        
        logRoutineAction("recirculación", "INICIADA - Fase 1: Aire lead 90s");

        // Programar próximo horario (2h después)
        state.nextRecirculationTime += PUMP_INTERVAL_MINUTES; // +2h en minutos (120 min)
        if (state.nextRecirculationTime >= 1440) state.nextRecirculationTime -= 1440; // Wrap 24h
        
        return;
    }
    
    // Si no está activo, no hay nada que hacer
    if (!state.recirculationActive) return;
    
    // FASE 1 → FASE 2: Aire lead terminado, activar bomba + aire continuo
    if (state.recircAirLeadActive && (now - state.recircAirLeadStartTime >= AIR_LEAD_MS)) {
        state.recircAirLeadActive = false;
        state.recircPumpActive = true;
        state.recircPumpStartTime = now;
        
        // Bomba ON + Aire sigue ON (aireación durante bomba)
        setActuatorStateImmediate(state.waterPump, true, "motobomba");
        setActuatorStateImmediate(state.airStone, true, "difusor"); // Mantener activo
        
        logRoutineAction("recirculación", "FASE 2: Bomba 8min + aire continuo");
        return;
    }
    
    // FASE 2 → FASE 3: Bomba terminada, activar post air
    if (state.recircPumpActive && (now - state.recircPumpStartTime >= PUMP_DURATION_MS)) {
        state.recircPumpActive = false;
        state.recircAirPostActive = true;
        state.recircAirPostStartTime = now;
        
        // Bomba OFF + Aire sigue ON (post hold)
        setActuatorStateImmediate(state.waterPump, false, "motobomba");
        setActuatorStateImmediate(state.airStone, true, "difusor"); // Mantener activo
        
        logRoutineAction("recirculación", "FASE 3: Post aire 45s");
        return;
    }
    
    // FASE 3 → FIN: Post air terminado, finalizar ciclo completo
    if (state.recircAirPostActive && (now - state.recircAirPostStartTime >= POST_AIR_HOLD_MS)) {
        state.recircAirPostActive = false;
        state.recirculationActive = false;
        
        // Todo OFF - el control autónomo tomará el control
        setActuatorStateImmediate(state.airStone, false, "difusor");
        state.airStoneMode = AIR_MODE_IDLE; // Liberar control
        
        logRoutineAction("recirculación", "COMPLETADA - Control liberado");
        return;
    }
}

// ========================================
// CRONOGRAMA INDEPENDIENTE DE AIREACIÓN AUTÓNOMA (cada 30min)
// ========================================

void AutonomousController::initializeAutonomousAirSchedule() {
    unsigned long currentMinutes = getCurrentMinutes();

    // CORREGIDO: Calcular próximo horario basado en múltiplos de 30 desde 00:00
    // Horarios válidos: 0, 30, 60, 90, 120, 150... (cada 30min desde medianoche)
    unsigned long nextCycle = ((currentMinutes / 30) + 1) * 30;
    nextCycle = nextCycle % 1440; // Wrap around medianoche usando modulo

    state.nextAutonomousAirTime = nextCycle;

    Serial.printf("💨 [AIREACIÓN] Próximo horario: %02ld:%02ld (minuto %ld desde medianoche)\n",
                  nextCycle / 60, nextCycle % 60, nextCycle);
}

bool AutonomousController::isAutonomousAirScheduleTime() {
    if (state.autonomousAirActive) return false; // Ya está activa
    if (state.airStoneMode != AIR_MODE_IDLE && state.airStoneMode != AIR_MODE_AUTONOMOUS) return false; // No disponible
    
    unsigned long currentMinutes = getCurrentMinutes();
    
    // CORREGIDO: Verificar si es hora de activar (amplia tolerancia para recuperarse)
    if (currentMinutes >= state.nextAutonomousAirTime) {
        // Si perdimos la ventana (más de 5 min tarde), recalcular próximo horario
        if (currentMinutes > state.nextAutonomousAirTime + 5) {
            Serial.printf("💨 [AIREACIÓN] Ventana perdida - recalculando desde minuto %lu\n", currentMinutes);

            // Recalcular basándose en múltiplos de 30 desde 00:00
            unsigned long nextCycle = ((currentMinutes / 30) + 1) * 30;
            nextCycle = nextCycle % 1440; // Wrap 24h usando modulo

            state.nextAutonomousAirTime = nextCycle;
            Serial.printf("💨 [AIREACIÓN] Nuevo próximo horario: %02ld:%02ld (minuto %ld)\n",
                          nextCycle / 60, nextCycle % 60, nextCycle);

            return false; // No activar ahora, esperar al próximo ciclo
        }
        
        return true; // Dentro de ventana válida (0-5 min después)
    }
    
    return false; // Aún no es hora
}

void AutonomousController::handleAutonomousAirRoutine() {
    unsigned long now = millis();
    
    // INICIO: Verificar si debe comenzar el ciclo de aireación autónoma
    if (isAutonomousAirScheduleTime() && !state.autonomousAirActive) {
        state.autonomousAirActive = true;
        state.autonomousAirStartTime = now;
        state.airStoneMode = AIR_MODE_AUTONOMOUS;
        
        setActuatorStateImmediate(state.airStone, true, "difusor");
        logRoutineAction("aireación_autónoma", "INICIADA - 5 minutos");
        
        // Programar próximo horario (30min después)
        state.nextAutonomousAirTime += 30;
        if (state.nextAutonomousAirTime >= 1440) state.nextAutonomousAirTime -= 1440; // Wrap 24h
        
        Serial.printf("💨 [AIREACIÓN] Próximo horario programado: %02ld:%02ld (minuto %ld)\n", 
                      state.nextAutonomousAirTime / 60, state.nextAutonomousAirTime % 60, 
                      state.nextAutonomousAirTime);
        return;
    }
    
    // FIN: Verificar si debe terminar el ciclo (5 minutos)
    if (state.autonomousAirActive && (now - state.autonomousAirStartTime >= AIR_PERIODIC_ON_MS)) {
        state.autonomousAirActive = false;
        state.airStoneMode = AIR_MODE_IDLE;
        
        setActuatorStateImmediate(state.airStone, false, "difusor");
        logRoutineAction("aireación_autónoma", "COMPLETADA");
        return;
    }
}

// NUEVA REGLA: Emergencia térmica (>25°C) - PRIORIDAD MÁXIMA
// NUEVA REGLA: Emergencia térmica (>25°C) - PRIORIDAD MÁXIMA
void AutonomousController::handleThermalEmergency() {
    if (!state.averageReadings.valid) return;
    
    float temp = state.averageReadings.temperature;
    unsigned long now = millis();
    
    // Verificar si se debe activar emergencia
    if (temp > TEMP_EMERGENCY_THRESHOLD && !state.thermalEmergencyActive) {
        if (now - state.lastEmergencyTime < EMERGENCY_MIN_INTERVAL_MS) {
            return;
        }
        
        state.thermalEmergencyActive = true;
        state.emergencyStartTime = now;
        state.lastEmergencyTime = now;
        
        state.recirculationActive = false;
        state.autonomousAirActive = false;
        state.recircAirLeadActive = false;
        state.recircAirPostActive = false;
        state.recircPumpActive = false;
        
        setActuatorStateImmediate(state.airStone, true, "difusor");
        setActuatorStateImmediate(state.waterPump, true, "bomba");
        
        Serial.printf("🚨 [EMERGENCIA] Temperatura crítica %.1f°C > %.1f°C - recirculación forzada\n", 
                     temp, TEMP_EMERGENCY_THRESHOLD);
        logRoutineAction("thermal_emergency", "recirculación 5min por temperatura crítica");
        return;
    }
    
    // Manejar emergencia activa
    if (state.thermalEmergencyActive) {
        if (now - state.emergencyStartTime >= EMERGENCY_PUMP_DURATION_MS) {
            state.thermalEmergencyActive = false;
            initializeRecirculationSchedule();
            initializeAutonomousAirSchedule();
            
            setActuatorStateImmediate(state.waterPump, false, "bomba");
            setActuatorStateImmediate(state.airStone, false, "difusor");
            
            Serial.printf("✅ [EMERGENCIA] Emergencia térmica completada - temperatura actual: %.1f°C\n", temp);
            logRoutineAction("thermal_emergency_complete", "recirculación de emergencia finalizada");
            return;
        }
        
        // Solo loguear cada X segundos (ej: 20s)
        static unsigned long lastProgressLog = 0;
        const unsigned long LOG_INTERVAL_MS = 20000; // 20 segundos
        if (now - lastProgressLog >= LOG_INTERVAL_MS) {
            lastProgressLog = now;
            Serial.printf("⚠️ [EMERGENCIA] En progreso - T:%.1f°C, tiempo restante: %lus\n", 
                         temp, (EMERGENCY_PUMP_DURATION_MS - (now - state.emergencyStartTime)) / 1000);
        }
    }
}


// MÁQUINA DE ESTADOS PARA CONTROL DE LUZ ARTIFICIAL
void AutonomousController::handleLightStateMachine() {
    if (!state.averageReadings.valid) return;

    float lux = state.averageReadings.lightLux;
    bool luxValid = !isnan(lux);
    bool inSchedule = isLightScheduleActive();
    unsigned long now = millis();

    // Si estamos fuera de horario, forzar estado OFF
    if (!inSchedule) {
        if (state.lightState != LIGHT_OFF) {
            state.lightState = LIGHT_OFF;
            setActuatorState(state.light, false, "luz");
            Serial.println("💡 [LUZ] FUERA DE HORARIO (19:00-5:00) → OFF");
        }
        return;
    }

    // Si no hay lectura válida de lux, mantener estado actual
    if (!luxValid) {
        Serial.println("💡 [LUZ] Lectura de lux no válida - manteniendo estado actual");
        return;
    }

    // Dentro de horario: control simplificado basado en lux
    if (state.lightState == LIGHT_OFF) {
        // Luz apagada: encender si lux < umbral ON
        if (lux < LUX_ON_THRESHOLD) {
            state.lightState = LIGHT_ON;
            state.lightOnStartTime = now;
            setActuatorState(state.light, true, "luz");
            Serial.printf("💡 [LUZ] OFF→ON: Lux insuficiente (%.0f < %.0f)\n", lux, LUX_ON_THRESHOLD);
        } else {
            Serial.printf("💡 [LUZ] OFF: Lux suficiente (%.0f >= %.0f)\n", lux, LUX_ON_THRESHOLD);
        }
    } else {
        // Luz encendida: apagar si lux > umbral OFF Y tiempo mínimo cumplido
        unsigned long onTime = now - state.lightOnStartTime;

        if (lux > LUX_OFF_THRESHOLD && onTime >= LIGHT_MIN_ON_MS) {
            state.lightState = LIGHT_OFF;
            setActuatorState(state.light, false, "luz");
            Serial.printf("💡 [LUZ] ON→OFF: Lux suficiente (%.0f > %.0f) después de %lu min\n",
                         lux, LUX_OFF_THRESHOLD, onTime / 60000);
        } else if (onTime < LIGHT_MIN_ON_MS) {
            unsigned long remainingMin = (LIGHT_MIN_ON_MS - onTime) / 60000;
            Serial.printf("💡 [LUZ] ON: Tiempo mínimo pendiente (%lu min) - Lux: %.0f\n",
                         remainingMin, lux);
        } else {
            Serial.printf("💡 [LUZ] ON: Lux insuficiente (%.0f <= %.0f) - continuando\n",
                         lux, LUX_OFF_THRESHOLD);
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

// NUEVA FUNCIÓN: Aplicación inmediata para cronogramas (sin esperar ciclo de control)
void AutonomousController::setActuatorStateImmediate(ActuatorState& state, bool newState, const char* name) {
    // Actualizar estado lógico
    setActuatorState(state, newState, name);
    
    // APLICAR INMEDIATAMENTE al hardware físico para eventos críticos
    if (&state == &(this->state.heater)) {
        if (state.isOn && !state.forceOff) {
            ActuatorController::turnHeaterOn();
            Serial.printf("⚡ [INMEDIATO] Calefactor aire → ON\n");
        } else {
            ActuatorController::turnHeaterOff();
            Serial.printf("⚡ [INMEDIATO] Calefactor aire → OFF\n");
        }
    }
    else if (&state == &(this->state.waterPump)) {
        if (state.isOn && !state.forceOff) {
            ActuatorController::turnWaterPumpOn();
            Serial.printf("⚡ [INMEDIATO] Bomba agua → ON (cronograma)\n");
        } else {
            ActuatorController::turnWaterPumpOff();
            Serial.printf("⚡ [INMEDIATO] Bomba agua → OFF (cronograma)\n");
        }
    }
    else if (&state == &(this->state.airStone)) {
        if (state.isOn && !state.forceOff) {
            ActuatorController::turnAirStoneOn();
            Serial.printf("⚡ [INMEDIATO] Difusor aire → ON (cronograma)\n");
        } else {
            ActuatorController::turnAirStoneOff();
            Serial.printf("⚡ [INMEDIATO] Difusor aire → OFF (cronograma)\n");
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

    if (state.waterHeater.isOn && !state.waterHeater.forceOff) {
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

    if (state.waterPump.isOn && !state.waterPump.forceOff) {
        ActuatorController::turnWaterPumpOn();
    } else {
        ActuatorController::turnWaterPumpOff();
    }

    if (state.airStone.isOn && !state.airStone.forceOff) {
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
        Serial.printf("   💡 Lux: %.0f (umbral: <%.0f=ON, >%.0f=OFF)\n",
                     state.averageReadings.lightLux, LUX_ON_THRESHOLD, LUX_OFF_THRESHOLD);
        Serial.printf("   🌊 Distancia sensor: %.1f cm (bomba OFF si >%.1f cm, ON si <%.1f cm)\n",
                     state.averageReadings.waterLevel, WATER_DISTANCE_MAX, WATER_DISTANCE_RECOVERY);
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

    // HUMIDIFICADOR: Mostrar estado maestro + estado de bloqueo/validación
    Serial.printf("   💨 Humidificador: maestro=%s",
                  state.humidifierMasterActive ? "ON" : "OFF");

    if (state.humidifierLocked) {
        unsigned long now = millis();
        unsigned long remainingMs = state.humidifierLockedUntil - now;
        unsigned long remainingMin = remainingMs / 60000;
        Serial.printf(" 🔒BLOQUEADO (expira en %lu min)", remainingMin);
    } else if (state.windowValidationActive) {
        unsigned long now = millis();
        unsigned long windowElapsed = (now - state.humidifierWindowStart) / 1000;
        Serial.printf(" 🔍VALIDANDO (%.1fs/300s)", (float)windowElapsed);
    }
    Serial.printf("\n");

    Serial.printf("   🌊 Bomba agua: %s %s %s\n", state.waterPump.isOn ? "ON" : "OFF",
                  state.waterPump.forceOff ? "(BLOQUEADA)" : "",
                  "");

    // NUEVA INFORMACIÓN: Estado de coordinación de bomba de aire
    const char* airModeStr = (state.airStoneMode == AIR_MODE_AUTONOMOUS) ? "AUTONOMO" :
                            (state.airStoneMode == AIR_MODE_RECIRCULATION) ? "RECIRCULACION" : "INACTIVO";
    const char* airPhaseStr = "";
    if (state.recircAirLeadActive) airPhaseStr = " (LEAD)";
    else if (state.recircAirPostActive) airPhaseStr = " (POST)";
    else if (state.autonomousAirActive) airPhaseStr = " (PERIODICO)";

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
    
    // Distancia del sensor ultrasónico - Fail-safe para proteger bomba
    state.waterLevelConfig = {
        .minValue = WATER_DISTANCE_RECOVERY,  // Distancia mínima para permitir bomba (< 6cm = lleno)
        .maxValue = WATER_DISTANCE_MAX,       // Distancia máxima antes de bloquear (> 7cm = vacío)
        .hysteresis = 1.0f,                   // 1cm de histéresis
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
        
        // Reset de estados al cambiar modo (ya no necesario - sistema independiente)
        if (newMode == AIR_MODE_AUTONOMOUS) {
            // Los horarios autónomos se manejan internamente
        } else if (newMode == AIR_MODE_RECIRCULATION) {
            // La recirculación maneja sus propios estados
        }
    }
}

// FUNCIONES LEGACY ELIMINADAS - REEMPLAZADAS POR SISTEMA INDEPENDIENTE
// calculateNextAutonomousAirTime() → initializeAutonomousAirSchedule()
// shouldActivateAutonomousAir() → isAutonomousAirScheduleTime()

// ========================================
// CONTROLADOR CENTRAL DE EXCLUSIÓN MUTUA
// ========================================

void AutonomousController::updateAirStoneControl() {
    // PRIORIDAD 1: Recirculación tiene control absoluto
    if (state.recirculationActive) {
        if (state.airStoneMode != AIR_MODE_RECIRCULATION) {
            setAirStoneMode(AIR_MODE_RECIRCULATION, "recirculación tomó control");
        }
        return; // Recirculación maneja todo internamente
    }
    
    // PRIORIDAD 2: Liberar control cuando recirculación termine
    if (!state.recirculationActive && state.airStoneMode == AIR_MODE_RECIRCULATION) {
        setAirStoneMode(AIR_MODE_IDLE, "recirculación completada - liberando control");
        return;
    }
    
    // PRIORIDAD 3: Aireación autónoma (solo si no hay recirculación)
    if (state.autonomousAirActive) {
        if (state.airStoneMode != AIR_MODE_AUTONOMOUS) {
            setAirStoneMode(AIR_MODE_AUTONOMOUS, "aireación autónoma activa");
        }
        return; // Aireación autónoma maneja todo internamente
    }
    
    // PRIORIDAD 4: Liberar control cuando aireación autónoma termine
    if (!state.autonomousAirActive && state.airStoneMode == AIR_MODE_AUTONOMOUS) {
        setAirStoneMode(AIR_MODE_IDLE, "aireación autónoma completada - liberando control");
        return;
    }
}

void AutonomousController::logAirStoneAction(const char* action, const char* mode, const char* reason) {
    Serial.printf("💨 [AIR-COORD] %s - Modo: %s - Razón: %s\n", action, mode, reason);
}

// ========================================
// PARCHE DE SEGURIDAD DEL CALEFACTOR
// ========================================

void AutonomousController::heaterFastMonitoring() {
    // Solo ejecutar si el monitoreo rápido está activo (calefactor encendido)
    if (!state.heaterFastMonitoringActive) return;
    
    unsigned long currentTime = millis();
    
    // Verificar si ha pasado el intervalo de monitoreo (3 segundos)
    if (currentTime - state.lastHeaterMonitorTime < HEATER_FAST_MONITOR_INTERVAL) return;
    
    state.lastHeaterMonitorTime = currentTime;
    
    // Leer temperatura actual
    float currentTemp = sensors->readTemperature();
    if (isnan(currentTemp)) {
        Serial.println("🔥 [HEATER-MONITOR] Sensor de temperatura falló durante monitoreo rápido");
        return;
    }
    
    // Añadir temperatura al buffer para media móvil
    addTemperatureToHeaterBuffer(currentTemp);
    
    // Obtener temperatura promedio
    float avgTemp = getHeaterAverageTemperature();
    
    // === VERIFICACIÓN DE EMERGENCIA (temperatura actual) ===
    // PRIORITARIO: Apagar inmediatamente sin verificar tiempo mínimo
    if (currentTemp >= HEATER_EMERGENCY_TEMP) {
        Serial.printf("🔴 [HEATER-MONITOR] EMERGENCIA detectada: %.1f°C ≥ %.1f°C\n",
                     currentTemp, HEATER_EMERGENCY_TEMP);
        Serial.printf("🔴 [HEATER-MONITOR] Ejecutando apagado inmediato...\n");
        heaterSafetyTurnOff("EMERGENCIA - temperatura crítica ≥25.0°C");
        Serial.printf("✅ [HEATER-MONITOR] Calefactor_aire estado OFF confirmado\n");
        return;
    }

    // === VERIFICACIÓN DE APAGADO CRÍTICO (temperatura promedio ≥22.5°C) ===
    // IMPORTANTE: Apagar sin verificar tiempo mínimo cuando se alcanza temperatura objetivo
    // El tiempo mínimo solo aplica para prevenir ciclos rápidos, no para seguridad térmica
    if (!isnan(avgTemp) && avgTemp >= HEATER_OFF_TEMP) {
        Serial.printf("🔴 [HEATER-MONITOR] Umbral alcanzado: promedio %.1f°C ≥ %.1f°C\n",
                     avgTemp, HEATER_OFF_TEMP);
        Serial.printf("🔴 [HEATER-MONITOR] Ejecutando apagado inmediato...\n");
        heaterSafetyTurnOff("monitoreo rápido - promedio ≥22.5°C (temperatura objetivo alcanzada)");
        Serial.printf("✅ [HEATER-MONITOR] Calefactor_aire estado OFF confirmado\n");
        return;
    }
    
    // Log periódico cada 30 segundos durante monitoreo activo
    static unsigned long lastLogTime = 0;
    if (currentTime - lastLogTime >= 30000) {
        Serial.printf("🔥 [HEATER-MONITOR] Temp actual: %.1f°C, Promedio: %.1f°C (buffer: %d/%d)\n", 
                      currentTemp, avgTemp, 
                      state.heaterTempBufferFull ? HEATER_TEMP_BUFFER_SIZE : state.heaterTempBufferIndex,
                      HEATER_TEMP_BUFFER_SIZE);
        lastLogTime = currentTime;
    }
}

void AutonomousController::addTemperatureToHeaterBuffer(float temp) {
    state.heaterTempBuffer[state.heaterTempBufferIndex] = temp;
    state.heaterTempBufferIndex = (state.heaterTempBufferIndex + 1) % HEATER_TEMP_BUFFER_SIZE;
    
    if (!state.heaterTempBufferFull && state.heaterTempBufferIndex == 0) {
        state.heaterTempBufferFull = true;
    }
}

float AutonomousController::getHeaterAverageTemperature() {
    if (!state.heaterTempBufferFull && state.heaterTempBufferIndex == 0) {
        // No hay lecturas aún
        return NAN;
    }
    
    int count = state.heaterTempBufferFull ? HEATER_TEMP_BUFFER_SIZE : state.heaterTempBufferIndex;
    float sum = 0.0f;
    
    for (int i = 0; i < count; i++) {
        sum += state.heaterTempBuffer[i];
    }
    
    return sum / count;
}

bool AutonomousController::canTurnHeaterOn() {
    unsigned long currentTime = millis();
    
    // Verificar tiempo mínimo de reposo
    if (state.heaterOffStartTime > 0) {
        unsigned long timeSinceOff = currentTime - state.heaterOffStartTime;
        if (timeSinceOff < HEATER_MIN_OFF_TIME) {
            return false;
        }
    }
    
    return true;
}

bool AutonomousController::canTurnHeaterOff() {
    unsigned long currentTime = millis();

    // Verificar tiempo mínimo encendido
    if (state.heaterOnStartTime > 0) {
        unsigned long timeSinceOn = currentTime - state.heaterOnStartTime;
        if (timeSinceOn < HEATER_MIN_ON_TIME) {
            // NOTA: Esta función solo se usa para apagados normales del ciclo de control
            // NO se usa para apagados de seguridad (≥22.5°C o ≥25°C) que son prioritarios
            return false;
        }
    }

    return true;
}

void AutonomousController::heaterSafetyTurnOff(const char* reason) {
    if (!state.heater.isOn) return;

    unsigned long currentTime = millis();

    // CRÍTICO: Apagar calefactor INMEDIATAMENTE (hardware + estado lógico)
    setActuatorStateImmediate(state.heater, false, "calefactor_aire");

    // ⚠️ SINCRONIZACIÓN DESHABILITADA (parche temporal - relé defectuoso)
    // if (state.humidifierMasterActive) {
    //     state.humidifierMasterActive = false;
    //     state.humidifier.isOn = false;
    //     state.windowValidationActive = false;
    //     ActuatorController::turnHumidifierMasterOff();
    //     ActuatorController::turnHumidifierRelayOff();
    //     Serial.println("💧 [SAFETY] Humidificador APAGADO - sincronizado con apagado de seguridad del calefactor");
    // }

    // Actualizar estados del monitoreo rápido
    state.heaterOffStartTime = currentTime;
    state.heaterFastMonitoringActive = false;

    // Logging detallado con timestamp
    float currentTemp = sensors->readTemperature();
    float avgTemp = getHeaterAverageTemperature();

    Serial.printf("🚨 [HEATER-SAFETY] APAGADO INMEDIATO: %s\n", reason);
    Serial.printf("    Temp actual: %.1f°C | Promedio: %.1f°C\n", currentTemp, avgTemp);
    Serial.printf("    Tiempo encendido: %lu segundos\n",
                  (currentTime - state.heaterOnStartTime) / 1000);
    Serial.printf("    ✅ Estado físico actualizado inmediatamente\n");

    logControlDecision("Temperatura", currentTemp, reason);
}

// ========================================
// RECIRCULACIÓN EXTRA POR CALEFACTOR DE AGUA
// ========================================

void AutonomousController::onWaterHeaterStateChanged(bool isNowOn) {
    // Si el calefactor pasó de ON → OFF
    if (state.lastWaterHeaterState && !isNowOn) {
        // Verificar que se apagó por alcanzar temperatura máxima
        if (state.averageReadings.valid && state.averageReadings.waterTemp >= WATER_TEMP_MAX) {
            Serial.printf("🌡️ [WATER-HEATER] Calefactor apagado por temperatura objetivo (%.1f°C ≥ %.1f°C)\n",
                         state.averageReadings.waterTemp, WATER_TEMP_MAX);
            tryTriggerExtraRecirculation();
        }
    }
    
    // Actualizar estado previo
    state.lastWaterHeaterState = isNowOn;
}

bool AutonomousController::canTriggerExtraRecirculation() {
    unsigned long currentTime = millis();
    
    // Validación 1: Verificar cooldown de 1 hora
    if (state.lastExtraRecirculationTime > 0) {
        unsigned long timeSinceLastExtra = currentTime - state.lastExtraRecirculationTime;
        if (timeSinceLastExtra < EXTRA_RECIRCULATION_COOLDOWN_MS) {
            unsigned long remainingCooldown = EXTRA_RECIRCULATION_COOLDOWN_MS - timeSinceLastExtra;
            Serial.printf("❌ [EXTRA-RECIRCULATION] Cooldown activo - faltan %lu minutos\n", 
                         remainingCooldown / (60 * 1000));
            return false;
        }
    }
    
    // Validación 2: No interferir con recirculación programada en progreso
    if (state.recirculationActive) {
        Serial.println("❌ [EXTRA-RECIRCULATION] Recirculación programada en progreso");
        return false;
    }
    
    // Validación 3: Ventana de seguridad con próxima recirculación programada
    unsigned long currentMinutes = getCurrentMinutes();
    unsigned long timeToNextRecirculation;
    
    if (state.nextRecirculationTime > currentMinutes) {
        timeToNextRecirculation = (state.nextRecirculationTime - currentMinutes) * 60 * 1000;
    } else {
        // Próxima recirculación es mañana
        timeToNextRecirculation = ((1440 - currentMinutes) + state.nextRecirculationTime) * 60 * 1000;
    }
    
    if (timeToNextRecirculation < SAFE_WINDOW_MS) {
        unsigned long safeWindowMinutes = SAFE_WINDOW_MS / (60 * 1000);
        Serial.printf("❌ [EXTRA-RECIRCULATION] Muy cerca de recirculación programada - faltan %lu min (mín: %lu min)\n",
                     timeToNextRecirculation / (60 * 1000), safeWindowMinutes);
        return false;
    }
    
    // Validación 4: Ya hay recirculación extra activa
    if (state.extraRecirculationActive) {
        Serial.println("❌ [EXTRA-RECIRCULATION] Recirculación extra ya activa");
        return false;
    }
    
    return true;
}

void AutonomousController::tryTriggerExtraRecirculation() {
    if (!canTriggerExtraRecirculation()) {
        return;
    }
    
    unsigned long currentTime = millis();
    
    // Activar recirculación extra
    state.extraRecirculationActive = true;
    state.extraRecirculationStartTime = currentTime;
    state.lastExtraRecirculationTime = currentTime;
    
    // Activar solo la bomba (NO aireador)
    setActuatorStateImmediate(state.waterPump, true, "motobomba");
    
    Serial.println("🔄 [EXTRA-RECIRCULATION] INICIADA - 3 minutos sin aireador");
    Serial.printf("   💡 Motivo: Calefactor apagado por temp %.1f°C ≥ %.1f°C\n",
                  state.averageReadings.waterTemp, WATER_TEMP_MAX);
    
    logRoutineAction("recirculación-extra", "INICIADA por calefactor agua");
}

void AutonomousController::handleExtraRecirculation() {
    if (!state.extraRecirculationActive) return;
    
    unsigned long currentTime = millis();
    unsigned long elapsedTime = currentTime - state.extraRecirculationStartTime;
    
    // Verificar si ha pasado la duración (3 minutos)
    if (elapsedTime >= EXTRA_RECIRCULATION_DURATION_MS) {
        // Finalizar recirculación extra
        state.extraRecirculationActive = false;
        
        // Apagar bomba
        setActuatorStateImmediate(state.waterPump, false, "motobomba");
        
        Serial.println("✅ [EXTRA-RECIRCULATION] COMPLETADA - bomba apagada");
        Serial.printf("   ⏱️  Duración: %lu segundos\n", elapsedTime / 1000);
        
        logRoutineAction("recirculación-extra", "COMPLETADA");
    }
}