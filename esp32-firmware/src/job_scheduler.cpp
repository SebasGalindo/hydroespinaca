#include "job_scheduler.h"
#include "mqtt_handler.h"
#include "config.h"
#include "pwm_manager.h"
#include "job_consolidator.h"
#include "job_notifier.h"
#include "job_utils.h"
#include "sensors.h"

// Global instance
JobScheduler jobScheduler;

// ========================================
// CONSTRUCTOR & DESTRUCTOR
// ========================================

JobScheduler::JobScheduler() : currentJob(nullptr), currentJobIndex(-1),
                                mqttHandler(nullptr), sensorManager(nullptr) {
    // Initialize PWM manager
    PWMManager::initialize();
}

JobScheduler::~JobScheduler() {
    // Clean up (no mutexes or tasks to delete)
}

// ========================================
// INITIALIZATION
// ========================================

void JobScheduler::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
    JobNotifier::setMQTTHandler(handler);
    Serial.println("✅ MQTT handler vinculado al JobScheduler");
}

void JobScheduler::setSensorManager(SensorManager* manager) {
    sensorManager = manager;
    Serial.println("✅ SensorManager vinculado con JobScheduler (para monitoreo del calefactor)");
}

void JobScheduler::begin() {
    Serial.println("🚀 JobScheduler inicializado (modo ejecución secuencial)");
    Serial.println("📋 Backend gestiona concurrencia vía pin-locking");
    Serial.println("⚙️  Firmware procesa jobs uno a la vez en orden de llegada");

    jobQueue.clear();
    currentJob = nullptr;
    currentJobIndex = -1;
}

// ========================================
// MAIN LOOP - SEQUENTIAL PROCESSING
// ========================================

void JobScheduler::loop() {
    // If there's a job currently executing, process its current step
    if (currentJob != nullptr) {
        processCurrentStep();
        return;
    }

    // No job running - check if there's one in the queue
    if (!jobQueue.empty()) {
        processNextJob();
    }
}

// ========================================
// JOB PROCESSING
// ========================================

void JobScheduler::processNextJob() {
    if (jobQueue.empty()) return;

    // Take first job from queue
    currentJobIndex = 0;
    currentJob = &jobQueue[0];

    Serial.println("═══════════════════════════════════════");
    Serial.printf("🚀 INICIANDO JOB: %s\n", currentJob->commandId.c_str());
    Serial.printf("📋 BaseId: %s\n", currentJob->baseId.c_str());
    Serial.printf("📊 Steps totales: %d\n", currentJob->steps.size());
    Serial.println("═══════════════════════════════════════");

    currentJob->currentStepIndex = 0;

    // Process first step immediately
    processCurrentStep();
}

void JobScheduler::processCurrentStep() {
    if (currentJob == nullptr) return;

    Step* step = currentJob->getCurrentStep();
    if (step == nullptr) {
        // Job completed
        completeCurrentJob();
        return;
    }

    unsigned long now = millis();

    // Handle step lifecycle
    switch (step->status) {
        case STEP_PENDING:
            // Start step execution
            Serial.printf("🔹 Step %d/%d: Pin %d, Mode: %s, Power: %s, Duration: %lu ms\n",
                         currentJob->currentStepIndex + 1,
                         currentJob->steps.size(),
                         step->pin,
                         step->mode == DIGITAL ? "DIGITAL" : "PWM",
                         step->power == ON ? "ON" : "OFF",
                         step->duration);

            step->status = STEP_IN_PROGRESS;
            step->startTime = now;
            step->endTime = now + step->duration;

            // Execute the step
            executeStep(*step);
            break;

        case STEP_IN_PROGRESS:
            // Check if step duration has elapsed
            if (now >= step->endTime) {
                // Step completed
                step->status = STEP_OK;

                String logEntry = JobUtils::getCurrentTimestamp() + " - Step completado";
                step->executionLog.push_back(logEntry);

                Serial.printf("✅ Step %d completado\n", currentJob->currentStepIndex + 1);

                // Move to next step
                currentJob->currentStepIndex++;
            }
            break;

        case STEP_OK:
        case STEP_CANCELLED:
        case STEP_ERROR:
            // Step already finished - move to next
            currentJob->currentStepIndex++;
            break;
    }
}

void JobScheduler::executeStep(Step& step) {
    Serial.printf("⚙️  Ejecutando step: Pin %d, Mode: %s, Power/Duty: %s/%d, Duration: %lu ms\n",
                  step.pin,
                  step.mode == DIGITAL ? "DIGITAL" : "PWM",
                  (step.power == ON) ? "ON" : "OFF",
                  step.dutyCycle, step.duration);

    String connectivityStr = JobNotifier::hasInternetConnectivity() ? "conectado" : "desconectado";
    String logEntry = JobUtils::getCurrentTimestamp() + " - Step iniciado (internet: " + connectivityStr + ")";
    step.executionLog.push_back(logEntry);

    // Validate pin range
    if (step.pin < 0 || step.pin >= 40) {
        Serial.printf("❌ ERROR: Pin %d fuera de rango (0-39), step cancelado\n", step.pin);
        step.status = STEP_ERROR;
        String logEntry = JobUtils::getCurrentTimestamp() + " - Error: Pin fuera de rango";
        step.executionLog.push_back(logEntry);
        return;
    }

    // SPECIAL CASE: Humidifier control (PIN_HUMID_POWER = 14)
    if (step.pin == PIN_HUMID_POWER) {
        if (step.power == ON) {
            Serial.printf("💨 HUMIDIFICADOR: Iniciando rutina especializada por %lu ms\n", step.duration);
            runHumidifierRoutine(step.duration);
            step.status = STEP_OK;
            String logEntry = JobUtils::getCurrentTimestamp() + " - Rutina humidificador completada";
            step.executionLog.push_back(logEntry);
        } else {
            Serial.println("💨 HUMIDIFICADOR: Comando OFF - apagando ambos relés");
            pinMode(PIN_HUMID_POWER, OUTPUT);
            pinMode(PIN_HUMID_RELAY, OUTPUT);
            digitalWrite(PIN_HUMID_POWER, HIGH);  // Relé maestro OFF
            digitalWrite(PIN_HUMID_RELAY, HIGH);  // Relé de pulso OFF
            Serial.println("💨 HUMIDIFICADOR: Relés desactivados - PIN 14 y PIN 13 HIGH");
            step.status = STEP_OK;
            String logEntry = JobUtils::getCurrentTimestamp() + " - Humidificador apagado forzadamente";
            step.executionLog.push_back(logEntry);
        }
        return;
    }

    // SPECIAL CASE: Heater control with temperature monitoring (PIN_RELAY_HEATER = 17)
    if (step.pin == PIN_RELAY_HEATER) {
        if (step.power == ON) {
            Serial.printf("🔥 CALEFACTOR: Iniciando rutina con monitoreo térmico por %lu ms\n", step.duration);
            runHeaterRoutine(step.duration);
            step.status = STEP_OK;
            String logEntry = JobUtils::getCurrentTimestamp() + " - Rutina calefactor completada (objetivo 22.5°C alcanzado)";
            step.executionLog.push_back(logEntry);
        } else {
            Serial.println("🔥 CALEFACTOR: Comando OFF - apagando relé");
            pinMode(PIN_RELAY_HEATER, OUTPUT);
            digitalWrite(PIN_RELAY_HEATER, HIGH);  // Relé OFF (lógica invertida)
            Serial.println("🔥 CALEFACTOR: Relé desactivado - PIN 17 HIGH");
            step.status = STEP_OK;
            String logEntry = JobUtils::getCurrentTimestamp() + " - Calefactor apagado forzadamente";
            step.executionLog.push_back(logEntry);
        }
        return;
    }

    // Configure pin
    pinMode(step.pin, OUTPUT);

    // Execute based on mode
    if (step.mode == DIGITAL) {
        // Digital mode: ON/OFF
        int pinValue = (step.power == ON) ? HIGH : LOW;
        digitalWrite(step.pin, pinValue);

        Serial.printf("📌 Pin %d configurado: DIGITAL %s\n",
                     step.pin, step.power == ON ? "HIGH" : "LOW");

        String logEntry = JobUtils::getCurrentTimestamp() + " - Pin " + String(step.pin) +
                         " digital " + (step.power == ON ? "HIGH" : "LOW");
        step.executionLog.push_back(logEntry);
    }
    else if (step.mode == PWM) {
        // PWM mode
        PWMManager::ensureAttached(step.pin);
        PWMManager::writeDuty(step.pin, step.dutyCycle);

        Serial.printf("📌 Pin %d configurado: PWM duty=%d/255 (%.1f%%)\n",
                     step.pin, step.dutyCycle, (step.dutyCycle / 255.0f) * 100.0f);

        String logEntry = JobUtils::getCurrentTimestamp() + " - Pin " + String(step.pin) +
                         " PWM " + String(step.dutyCycle) + "/255";
        step.executionLog.push_back(logEntry);
    }
}

void JobScheduler::completeCurrentJob() {
    if (currentJob == nullptr) return;

    Serial.println("═══════════════════════════════════════");
    Serial.printf("🏁 JOB COMPLETADO: %s\n", currentJob->commandId.c_str());

    // Determine completion type
    if (currentJob->hasError()) {
        currentJob->completionType = JOB_COMPLETED_ERROR;
        Serial.println("⚠️  Completado con errores");
    } else if (currentJob->isCancelled()) {
        currentJob->completionType = JOB_COMPLETED_CANCELLED;
        Serial.println("⚠️  Completado (cancelado)");
    } else {
        Serial.println("✅ Completado exitosamente");
    }

    // Report completion via MQTT
    JobNotifier::publishCompletion(*currentJob);

    Serial.println("═══════════════════════════════════════");

    // Remove completed job from queue
    if (currentJobIndex >= 0 && currentJobIndex < jobQueue.size()) {
        jobQueue.erase(jobQueue.begin() + currentJobIndex);
    }

    // Reset current job pointer
    currentJob = nullptr;
    currentJobIndex = -1;

    Serial.printf("📊 Jobs restantes en cola: %d\n", jobQueue.size());
}

// ========================================
// MQTT JOB SCHEDULE PROCESSING
// ========================================

void JobScheduler::processJobSchedule(const JsonDocument& payload) {
    Serial.println("═══════════════════════════════════════");
    Serial.println("📥 PROCESANDO JOB SCHEDULE");

    // Extract esp32Id
    if (!payload.containsKey("esp32Id")) {
        Serial.println("❌ ERROR: Payload no contiene 'esp32Id'");
        return;
    }

    String esp32Id = payload["esp32Id"].as<String>();
    Serial.printf("🆔 ESP32 ID: %s\n", esp32Id.c_str());

    // Validate ESP32 ID
    if (esp32Id != ESP32_ID) {
        Serial.printf("❌ ERROR: Job schedule no es para este ESP32 (recibido: %s, esperado: %s)\n",
                     esp32Id.c_str(), ESP32_ID);
        return;
    }

    // Extract jobs array (backend sends JobScheduleDto with jobs array)
    if (!payload.containsKey("jobs")) {
        Serial.println("❌ ERROR: Payload no contiene 'jobs'");
        return;
    }

    JsonArrayConst jobsArray = payload["jobs"].as<JsonArrayConst>();
    Serial.printf("📦 Jobs recibidos: %d\n", jobsArray.size());

    int jobsAdded = 0;
    int jobsConsolidated = 0;

    for (JsonVariantConst jobVariant : jobsArray) {
        JsonObjectConst jobObj = jobVariant.as<JsonObjectConst>();

        // Parse job
        Job newJob;
        newJob.commandId = jobObj["commandId"].as<String>();
        newJob.baseId = jobObj["baseId"] | newJob.commandId;  // Use commandId as fallback
        newJob.queueTime = millis();

        Serial.printf("\n🔹 Job: %s (BaseId: %s)\n", newJob.commandId.c_str(), newJob.baseId.c_str());

        // Parse steps
        JsonArrayConst stepsArray = jobObj["steps"].as<JsonArrayConst>();
        Serial.printf("   Steps: %d\n", stepsArray.size());

        for (JsonVariantConst stepVariant : stepsArray) {
            JsonObjectConst stepObj = stepVariant.as<JsonObjectConst>();

            Step step;
            step.pin = String(stepObj["pin"].as<String>()).toInt();

            String modeStr = stepObj["mode"].as<String>();
            step.mode = (modeStr == "PWM") ? PWM : DIGITAL;

            if (stepObj.containsKey("power")) {
                String powerStr = stepObj["power"].as<String>();
                step.power = (powerStr == "ON") ? ON : OFF;
            }

            if (stepObj.containsKey("dutyCycle")) {
                // Backend sends 0-100, convert to 0-255
                double dutyCyclePercent = stepObj["dutyCycle"].as<double>();
                step.dutyCycle = (int)((dutyCyclePercent / 100.0) * 255.0);
            }

            // Backend sends duration in seconds (can be decimal)
            double durationSec = stepObj["duration"].as<double>();
            step.duration = (unsigned long)(durationSec * 1000.0);  // Convert to milliseconds

            step.status = STEP_PENDING;

            newJob.steps.push_back(step);

            Serial.printf("      Pin %d, %s, %s, Duty: %d, Duration: %lu ms\n",
                         step.pin,
                         step.mode == DIGITAL ? "DIGITAL" : "PWM",
                         step.power == ON ? "ON" : "OFF",
                         step.dutyCycle,
                         step.duration);
        }

        // Check if we should consolidate this job with an existing one
        bool consolidated = false;

        // Try to consolidate with queued jobs
        for (auto& existingJob : jobQueue) {
            if (JobConsolidator::canConsolidate(newJob, existingJob)) {
                Serial.printf("🔄 Consolidando job %s con job existente %s\n",
                             newJob.commandId.c_str(), existingJob.commandId.c_str());

                std::vector<String> consolidationLogs;
                JobConsolidator::consolidateJob(existingJob, newJob, consolidationLogs);

                // Log consolidation logs
                for (const auto& log : consolidationLogs) {
                    Serial.println(log);
                }

                jobsConsolidated++;
                consolidated = true;
                break;
            }
        }

        // Try to consolidate with currently running job
        if (!consolidated && currentJob != nullptr) {
            if (JobConsolidator::canConsolidate(newJob, *currentJob)) {
                Serial.printf("🔄 Consolidando job %s con job en ejecución %s\n",
                             newJob.commandId.c_str(), currentJob->commandId.c_str());

                std::vector<String> consolidationLogs;
                JobConsolidator::consolidateJob(*currentJob, newJob, consolidationLogs);

                // Log consolidation logs
                for (const auto& log : consolidationLogs) {
                    Serial.println(log);
                }

                jobsConsolidated++;
                consolidated = true;
            }
        }

        // If not consolidated, add to queue
        if (!consolidated) {
            jobQueue.push_back(newJob);
            jobsAdded++;
            Serial.printf("✅ Job %s agregado a cola (posición %d)\n",
                         newJob.commandId.c_str(), jobQueue.size());
        }
    }

    Serial.println("\n📊 RESUMEN:");
    Serial.printf("   ✅ Jobs nuevos agregados: %d\n", jobsAdded);
    Serial.printf("   🔄 Jobs consolidados: %d\n", jobsConsolidated);
    Serial.printf("   📋 Total en cola: %d\n", jobQueue.size());
    Serial.printf("   ⚙️  Job en ejecución: %s\n", currentJob ? currentJob->commandId.c_str() : "Ninguno");
    Serial.println("═══════════════════════════════════════");
}

// ========================================
// EMERGENCY STOP
// ========================================

void JobScheduler::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA - Deteniendo todos los jobs");

    // Clear queue
    jobQueue.clear();

    // Cancel current job if any
    if (currentJob != nullptr) {
        Serial.printf("🛑 Cancelando job en ejecución: %s\n", currentJob->commandId.c_str());
        currentJob = nullptr;
        currentJobIndex = -1;
    }

    // Turn off all pins (safety)
    for (int pin = 0; pin < 40; pin++) {
        pinMode(pin, OUTPUT);
        digitalWrite(pin, LOW);
        PWMManager::detachIfAttached(pin);
    }

    Serial.println("✅ Parada de emergencia completada");
}

// ========================================
// STATUS METHODS
// ========================================

bool JobScheduler::isBusy() {
    return currentJob != nullptr || !jobQueue.empty();
}

int JobScheduler::getQueueSize() {
    return jobQueue.size();
}

void JobScheduler::printStatus() {
    Serial.println("📊 JobScheduler Status:");
    Serial.printf("   ⚙️  Job actual: %s\n", currentJob ? currentJob->commandId.c_str() : "Ninguno");
    Serial.printf("   📋 Jobs en cola: %d\n", jobQueue.size());

    if (currentJob) {
        Serial.printf("      Step actual: %d/%d\n",
                     currentJob->currentStepIndex + 1,
                     currentJob->steps.size());
    }

    if (!jobQueue.empty()) {
        Serial.println("   📋 Cola:");
        for (size_t i = 0; i < jobQueue.size() && i < 5; i++) {
            Serial.printf("      %d. %s (%d steps)\n",
                         (int)i + 1,
                         jobQueue[i].commandId.c_str(),
                         jobQueue[i].steps.size());
        }
        if (jobQueue.size() > 5) {
            Serial.printf("      ... y %d más\n", jobQueue.size() - 5);
        }
    }
}

// ========================================
// PIN CANCELLATION
// ========================================

void JobScheduler::cancelJobsOnPin(int pin, const String& reason) {
    Serial.printf("🚫 Cancelando jobs que usan pin %d: %s\n", pin, reason.c_str());

    int cancelledCount = 0;

    // Cancel in queue
    for (auto& job : jobQueue) {
        bool usesPin = false;
        for (const auto& step : job.steps) {
            if (step.pin == pin) {
                usesPin = true;
                break;
            }
        }

        if (usesPin) {
            // Mark all steps as cancelled
            for (auto& step : job.steps) {
                if (step.status == STEP_PENDING || step.status == STEP_IN_PROGRESS) {
                    step.status = STEP_CANCELLED;
                    String logEntry = JobUtils::getCurrentTimestamp() + " - Cancelado: " + reason;
                    step.executionLog.push_back(logEntry);
                }
            }

            job.completionType = JOB_COMPLETED_CANCELLED;
            cancelledCount++;

            Serial.printf("   ❌ Job %s cancelado\n", job.commandId.c_str());
        }
    }

    // Cancel current job if it uses the pin
    if (currentJob != nullptr) {
        bool usesPin = false;
        for (const auto& step : currentJob->steps) {
            if (step.pin == pin) {
                usesPin = true;
                break;
            }
        }

        if (usesPin) {
            Serial.printf("   ❌ Job en ejecución %s cancelado\n", currentJob->commandId.c_str());

            for (auto& step : currentJob->steps) {
                if (step.status == STEP_PENDING || step.status == STEP_IN_PROGRESS) {
                    step.status = STEP_CANCELLED;
                    String logEntry = JobUtils::getCurrentTimestamp() + " - Cancelado: " + reason;
                    step.executionLog.push_back(logEntry);
                }
            }

            currentJob->completionType = JOB_COMPLETED_CANCELLED;
            cancelledCount++;

            // Complete it immediately
            completeCurrentJob();
        }
    }

    Serial.printf("📊 Total de jobs cancelados: %d\n", cancelledCount);
}

// ========================================
// HUMIDIFIER SPECIALIZED ROUTINE
// ========================================
void JobScheduler::runHumidifierRoutine(unsigned long durationMs) {
    Serial.printf("💨 [HUMID] Iniciando rutina especializada por %lu ms (simula pulsación de botón)\n", durationMs);

    unsigned long start = millis();

    // Configure pins
    pinMode(PIN_HUMID_POWER, OUTPUT);
    pinMode(PIN_HUMID_RELAY, OUTPUT);

    // === SECUENCIA DE ACTIVACIÓN (basada en firmware autónomo) ===

    // Step 1: Activar relé maestro (PIN 14) - LÓGICA INVERTIDA
    Serial.println("💨 [HUMID] Activando relé maestro (power) - PIN 14 LOW");
    digitalWrite(PIN_HUMID_POWER, LOW);  // ACTIVO LOW (lógica invertida)

    // Step 2: Delay de seguridad (2 segundos)
    Serial.println("💨 [HUMID] Delay de seguridad (2000ms)");
    delay(2000);

    // Step 3: Pulso de activación en relé (PIN 13) - simula pulsación de botón
    Serial.println("💨 [HUMID] Pulso de activación - PIN 13 LOW por 1 segundo");
    digitalWrite(PIN_HUMID_RELAY, LOW);  // ACTIVO LOW (lógica invertida) - pulso ON
    delay(1000);     // Mantener pulso por 1 segundo

    digitalWrite(PIN_HUMID_RELAY, HIGH); // Pulso OFF
    Serial.println("💨 [HUMID] Pulso de activación completado - PIN 13 HIGH");
    Serial.println("💨 [HUMID] Humidificador activado, funcionará durante el tiempo especificado");

    // === ESPERAR DURACIÓN ESPECIFICADA ===

    // El humidificador ya está funcionando, solo esperamos el tiempo restante
    unsigned long remainingTime = durationMs - (millis() - start);
    if (remainingTime > 0) {
        Serial.printf("💨 [HUMID] Esperando %lu ms hasta desactivación...\n", remainingTime);
        delay(remainingTime);
    }

    // === DESACTIVACIÓN COMPLETA ===

    // Step 4: Apagar relé maestro (power)
    Serial.println("💨 [HUMID] Desactivando relé maestro - PIN 14 HIGH");
    digitalWrite(PIN_HUMID_POWER, HIGH); // INACTIVO HIGH (lógica invertida)

    // Step 5: Asegurar relé de pulso OFF
    digitalWrite(PIN_HUMID_RELAY, HIGH);  // INACTIVO HIGH (lógica invertida)

    unsigned long totalTime = millis() - start;
    Serial.printf("💨 [HUMID] Rutina completada en %lu ms - humidificador desactivado\n", totalTime);
}

// ========================================
// HEATER SPECIALIZED ROUTINE WITH TEMPERATURE MONITORING
// ========================================
void JobScheduler::runHeaterRoutine(unsigned long durationMs) {
    Serial.printf("🔥 [HEATER] Iniciando rutina con monitoreo térmico por %lu ms\n", durationMs);
    Serial.println("🔥 [HEATER] Objetivo: mantener calefactor hasta alcanzar 22.5°C promedio");

    // Validate sensor manager availability
    if (sensorManager == nullptr) {
        Serial.println("❌ [HEATER] ERROR: SensorManager no disponible - abortando rutina");
        return;
    }

    // Constants for temperature monitoring (from autonomous firmware)
    const float HEATER_OFF_TEMP = 22.5f;           // Target temperature (°C)
    const float HEATER_EMERGENCY_TEMP = 25.0f;     // Emergency shutdown temperature (°C)
    const unsigned long MONITOR_INTERVAL = 3000;   // 3 seconds monitoring interval
    const int TEMP_BUFFER_SIZE = 10;               // Buffer size for moving average

    // Temperature buffer for moving average
    float tempBuffer[TEMP_BUFFER_SIZE];
    int tempBufferIndex = 0;
    bool tempBufferFull = false;

    unsigned long start = millis();
    unsigned long lastMonitorTime = 0;

    // Configure pin and turn heater ON
    pinMode(PIN_RELAY_HEATER, OUTPUT);
    digitalWrite(PIN_RELAY_HEATER, LOW);  // ACTIVO LOW (lógica invertida)
    Serial.println("🔥 [HEATER] Calefactor ENCENDIDO - PIN 17 LOW");
    Serial.println("🔥 [HEATER] Monitoreo térmico cada 3 segundos activado");

    // Main monitoring loop
    while (true) {
        unsigned long currentTime = millis();
        unsigned long elapsedTime = currentTime - start;

        // Check if maximum duration exceeded
        if (elapsedTime >= durationMs) {
            Serial.printf("🔥 [HEATER] Duración máxima alcanzada (%lu ms) - apagando calefactor\n", durationMs);
            break;
        }

        // Temperature monitoring every 3 seconds
        if (currentTime - lastMonitorTime >= MONITOR_INTERVAL) {
            lastMonitorTime = currentTime;

            // Read current temperature
            float currentTemp = sensorManager->readTemperature();

            if (isnan(currentTemp)) {
                Serial.println("🔥 [HEATER-MONITOR] Sensor de temperatura falló - continuando monitoreo");
                delay(100);  // Small delay before next iteration
                continue;
            }

            // Add temperature to buffer
            tempBuffer[tempBufferIndex] = currentTemp;
            tempBufferIndex = (tempBufferIndex + 1) % TEMP_BUFFER_SIZE;
            if (!tempBufferFull && tempBufferIndex == 0) {
                tempBufferFull = true;
            }

            // Calculate average temperature
            float avgTemp = 0.0f;
            int count = tempBufferFull ? TEMP_BUFFER_SIZE : tempBufferIndex;
            if (count > 0) {
                for (int i = 0; i < count; i++) {
                    avgTemp += tempBuffer[i];
                }
                avgTemp /= count;
            }

            // === EMERGENCY SHUTDOWN (current temperature) ===
            if (currentTemp >= HEATER_EMERGENCY_TEMP) {
                Serial.printf("🔴 [HEATER-MONITOR] EMERGENCIA detectada: %.1f°C ≥ %.1f°C\n",
                             currentTemp, HEATER_EMERGENCY_TEMP);
                Serial.println("🔴 [HEATER-MONITOR] Ejecutando apagado inmediato...");
                break;
            }

            // === TARGET SHUTDOWN (average temperature) ===
            if (count > 0 && avgTemp >= HEATER_OFF_TEMP) {
                Serial.printf("🔴 [HEATER-MONITOR] Objetivo alcanzado: promedio %.1f°C ≥ %.1f°C\n",
                             avgTemp, HEATER_OFF_TEMP);
                Serial.printf("🔴 [HEATER-MONITOR] Tiempo de calentamiento: %lu ms (%.1f segundos)\n",
                             elapsedTime, elapsedTime / 1000.0f);
                break;
            }

            // Periodic log every monitoring cycle
            Serial.printf("🔥 [HEATER-MONITOR] Temp actual: %.1f°C, Promedio: %.1f°C (buffer: %d/%d)\n",
                         currentTemp, avgTemp, count, TEMP_BUFFER_SIZE);
        }

        // Small delay to prevent tight loop
        delay(100);
    }

    // === SHUTDOWN SEQUENCE ===
    digitalWrite(PIN_RELAY_HEATER, HIGH);  // INACTIVO HIGH (lógica invertida)
    Serial.println("🔥 [HEATER] Calefactor APAGADO - PIN 17 HIGH");

    unsigned long totalTime = millis() - start;
    Serial.printf("🔥 [HEATER] Rutina completada en %lu ms (%.1f segundos) - calefactor desactivado\n",
                 totalTime, totalTime / 1000.0f);
}
