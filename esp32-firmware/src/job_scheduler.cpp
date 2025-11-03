#include "job_scheduler.h"
#include "mqtt_handler.h"
#include "config.h"
#include "pwm_manager.h"
#include "job_notifier.h"
#include "job_utils.h"
#include "sensors.h"
#include "pins.h"

// JobConsolidator removed - no longer needed with concurrent execution

// Global instance
JobScheduler jobScheduler;

// ========================================
// CONSTRUCTOR & DESTRUCTOR
// ========================================

JobScheduler::JobScheduler() : mqttHandler(nullptr), sensorManager(nullptr) {
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
    Serial.println("🚀 JobScheduler inicializado (modo ejecución concurrente)");
    Serial.println("📋 Backend gestiona pin-locking vía coordinación");
    Serial.println("⚙️  Firmware procesa múltiples jobs simultáneamente con timers no bloqueantes");

    activeJobs.clear();
}

// ========================================
// MAIN LOOP - CONCURRENT PROCESSING
// ========================================

void JobScheduler::loop() {
    // Accumulate completed jobs in this iteration
    std::vector<Job> completedJobs;

    // Process all active jobs simultaneously (non-blocking)
    for (auto it = activeJobs.begin(); it != activeJobs.end(); ) {
        Job& job = *it;

        // Process this job
        processJob(job);

        // If job is completed, accumulate it for batch reporting
        if (job.isCompleted()) {
            completedJobs.emplace_back(std::move(job));  // Move para evitar copia y double-delete
            it = activeJobs.erase(it);  // Remove from active jobs
        } else {
            ++it;
        }
    }

    // Report all completions in a single batch
    if (!completedJobs.empty()) {
        reportCompletionsBatch(completedJobs);
    }
}

// ========================================
// JOB PROCESSING
// ========================================

void JobScheduler::processJob(Job& job) {
    Step* step = job.getCurrentStep();
    if (step == nullptr) return;  // Job completed

    // Process the current step
    processStep(*step);

    // If step completed, move to next step
    if (step->status == STEP_OK || step->status == STEP_CANCELLED || step->status == STEP_ERROR) {
        job.currentStepIndex++;
    }
}

void JobScheduler::processStep(Step& step) {
    unsigned long now = millis();

    switch (step.status) {
        case STEP_PENDING:
            // Start the step
            startStep(step);
            break;

        case STEP_IN_PROGRESS:
            // Update specialized routines if needed
            if (step.pin == PIN_RELAY_HEATER && step.heaterState != nullptr) {
                updateHeaterMonitoring(step);
            } else if (step.pin == PIN_HUMID_POWER && step.humidifierState != nullptr) {
                updateHumidifierCycle(step);
            }

            // Check if step duration has elapsed
            if (now >= step.endTime) {
                // Step completed by timeout
                step.status = STEP_OK;

                // 🔥 CRÍTICO: APAGADO AUTOMÁTICO AL FINALIZAR
                // Todos los pines deben apagarse cuando el tiempo termina
                Serial.printf("⏰ [TIMEOUT] Tiempo cumplido para pin %d - apagando actuador\n", step.pin);

                // Cleanup: Turn off pin if needed
                if (step.pin == PIN_RELAY_HEATER) {
                    digitalWrite(PIN_RELAY_HEATER, HIGH);  // Turn OFF
                    Serial.println("🔥 [HEATER] Timeout alcanzado - calefactor apagado");
                    // 🔒 SEGURIDAD: Liberar estado antes de marcar como completo
                    if (step.heaterState) {
                        Serial.println("🔥 [HEATER] Liberando estado de monitoreo");
                        step.heaterState.reset();  // Libera memoria explícitamente
                    }
                } else if (step.pin == PIN_HUMID_POWER) {
                    digitalWrite(PIN_HUMID_POWER, HIGH);  // Turn OFF power
                    digitalWrite(PIN_HUMID_RELAY, HIGH);  // Turn OFF relay
                    Serial.println("💨 [HUMID] Timeout alcanzado - humidificador apagado");
                    // 🔒 SEGURIDAD: Liberar estado antes de marcar como completo
                    if (step.humidifierState) {
                        Serial.println("💨 [HUMID] Liberando estado de ciclo");
                        step.humidifierState.reset();  // Libera memoria explícitamente
                    }
                } else {
                    // ⚡ APAGADO GENÉRICO: Para todos los demás pines
                    if (step.mode == DIGITAL) {
                        int offValue;

                        // EXCEPCIÓN: Pin 27 (ventiladores) usa lógica directa (MOSFET, no relé)
                        if (step.pin == 27) {
                            // DIRECT LOGIC: LOW = OFF
                            offValue = LOW;
                        } else {
                            // INVERTED LOGIC: HIGH = OFF (para relés)
                            offValue = HIGH;
                        }

                        digitalWrite(step.pin, offValue);

                        // Nombre del actuador para logging
                        const char* pinName = "Genérico";
                        if (step.pin == 15) pinName = "Piedra difusora";
                        else if (step.pin == 25) pinName = "Calefactor agua";
                        else if (step.pin == 19) pinName = "Bomba agua";
                        else if (step.pin == 18) pinName = "LED amplio espectro";
                        else if (step.pin == 27) pinName = "Ventiladores";

                        Serial.printf("🔌 [TIMEOUT] Pin %d (%s): DIGITAL apagado (%s)\n",
                                     step.pin, pinName, offValue == LOW ? "LOW" : "HIGH");

                        // Verificar que realmente se apagó
                        int readBack = digitalRead(step.pin);
                        if (readBack != offValue) {
                            Serial.printf("⚠️  [TIMEOUT] ADVERTENCIA: Pin %d no se apagó correctamente (leído=%d, esperado=%d)\n",
                                         step.pin, readBack, offValue);
                        }
                    } else if (step.mode == PWM) {
                        // PWM: duty 0 = OFF (lógica directa, sin inversión)
                        PWMManager::writeDuty(step.pin, 0);
                        Serial.printf("🔌 [TIMEOUT] Pin %d: PWM apagado (duty=0)\n", step.pin);

                        // 🔥 CRÍTICO: Liberar canal LEDC para permitir reutilización del pin
                        PWMManager::detachIfAttached(step.pin);
                        Serial.printf("🔓 [TIMEOUT] Pin %d: Canal PWM liberado\n", step.pin);
                    }
                }

                Serial.printf("✅ Step completado (pin %d)\n", step.pin);
            }
            break;

        case STEP_OK:
        case STEP_CANCELLED:
        case STEP_ERROR:
            // Step already finished - nothing to do
            break;
    }
}

void JobScheduler::startStep(Step& step) {
    unsigned long now = millis();

    Serial.printf("⚙️  Iniciando step: Pin %d, Mode: %s, Power: %s, Duration: %lu ms\n",
                  step.pin,
                  step.mode == DIGITAL ? "DIGITAL" : "PWM",
                  (step.power == ON) ? "ON" : "OFF",
                  step.duration);

    // Validate pin range
    if (step.pin < 0 || step.pin >= 40) {
        Serial.printf("❌ ERROR: Pin %d fuera de rango (0-39)\n", step.pin);
        step.status = STEP_ERROR;
        return;
    }

    // ⏱️ VALIDACIÓN: Tiempo mínimo de activación para actuadores específicos
    const unsigned long MIN_WATER_PUMP_TIME = 5000;     // Bomba agua: mínimo 5 segundos
    const unsigned long MIN_LED_TIME = 3000;            // LED: mínimo 3 segundos

    if (step.power == ON) {
        if (step.pin == 19 && step.duration < MIN_WATER_PUMP_TIME) {  // Bomba de agua
            Serial.printf("⚠️  [VALIDACIÓN] Bomba agua (pin 19): duration=%lu ms < mínimo=%lu ms\n",
                         step.duration, MIN_WATER_PUMP_TIME);
            Serial.printf("⚠️  [VALIDACIÓN] Ajustando a tiempo mínimo de seguridad\n");
            step.duration = MIN_WATER_PUMP_TIME;
        } else if (step.pin == 18 && step.duration < MIN_LED_TIME) {  // LED amplio espectro
            Serial.printf("⚠️  [VALIDACIÓN] LED (pin 18): duration=%lu ms < mínimo=%lu ms\n",
                         step.duration, MIN_LED_TIME);
            Serial.printf("⚠️  [VALIDACIÓN] Ajustando a tiempo mínimo de seguridad\n");
            step.duration = MIN_LED_TIME;
        }
    }

    // Mark as in progress
    step.status = STEP_IN_PROGRESS;
    step.startTime = now;
    step.endTime = now + step.duration;

    // Configure pin
    pinMode(step.pin, OUTPUT);

    // SPECIAL CASE: Heater with temperature monitoring
    if (step.pin == PIN_RELAY_HEATER && step.power == ON) {
        Serial.println("🔥 [HEATER] Iniciando con monitoreo térmico (22.5°C target, 25°C emergency)");
        digitalWrite(PIN_RELAY_HEATER, LOW);  // Turn ON (active LOW)

        // 🔒 SEGURIDAD: unique_ptr (no memory leak, compatible C++11)
        step.heaterState.reset(new HeaterMonitorState());
        Serial.println("🔥 [HEATER] Estado de monitoreo inicializado");
        return;
    }

    // SPECIAL CASE: Humidifier with fan cycle
    if (step.pin == PIN_HUMID_POWER && step.power == ON) {
        Serial.println("💨 [HUMID] Iniciando rutina de humidificador completa");
        Serial.println("💨 [HUMID] Orden: 1️⃣ Generador niebla ON → 2️⃣ Ventilador ciclo (40s OFF / 15s ON)");

        // Initialize pins
        pinMode(PIN_HUMID_RELAY, OUTPUT);
        pinMode(PIN_HUMID_POWER, OUTPUT);

        // 1️⃣ PRIMERO: Activar generador de niebla (relay)
        digitalWrite(PIN_HUMID_RELAY, LOW);   // Mist ON (active LOW)
        Serial.println("💨 [HUMID] ✅ Step 1: Generador de niebla activado (PIN 13 = LOW)");
        delay(100);  // Pequeña pausa para que se active

        // 2️⃣ SEGUNDO: Mantener ventilador OFF inicialmente (ciclo comenzará después)
        digitalWrite(PIN_HUMID_POWER, HIGH);  // Fan OFF initially (active LOW)
        Serial.println("💨 [HUMID] ✅ Step 2: Ventilador en espera (PIN 14 = HIGH, iniciará ciclo en 40s)");

        // 🔒 SEGURIDAD: unique_ptr (no memory leak, compatible C++11)
        step.humidifierState.reset(new HumidifierCycleState());
        step.humidifierState->lastFanCycleTime = now;
        step.humidifierState->fanOn = false;

        Serial.println("💨 [HUMID] 🎉 Rutina de humidificador iniciada correctamente");
        return;
    }

    // OFF commands for special pins
    if (step.pin == PIN_RELAY_HEATER && step.power == OFF) {
        digitalWrite(PIN_RELAY_HEATER, HIGH);  // Turn OFF
        Serial.println("🔥 [HEATER] Apagado forzado");
        step.status = STEP_OK;
        return;
    }

    if (step.pin == PIN_HUMID_POWER && step.power == OFF) {
        digitalWrite(PIN_HUMID_POWER, HIGH);   // Fan OFF
        digitalWrite(PIN_HUMID_RELAY, HIGH);   // Mist OFF
        Serial.println("💨 [HUMID] Apagado forzado");
        step.status = STEP_OK;
        return;
    }

    // GENERIC: Digital or PWM control
    if (step.mode == DIGITAL) {
        int pinValue;
        bool isInverted = true;  // Default: relay logic (inverted)

        // EXCEPCIÓN: Pin 27 (ventiladores) usa lógica directa (MOSFET, no relé)
        if (step.pin == 27) {
            // DIRECT LOGIC: MOSFET connection (non-inverted)
            // power: "ON"  → HIGH (fan ON)
            // power: "OFF" → LOW (fan OFF)
            pinValue = (step.power == ON) ? HIGH : LOW;
            isInverted = false;
        } else {
            // INVERTED LOGIC: All relays are active-LOW
            // power: "ON"  → LOW (relay ON)
            // power: "OFF" → HIGH (relay OFF)
            pinValue = (step.power == ON) ? LOW : HIGH;
        }

        digitalWrite(step.pin, pinValue);

        // Mapeo de pines comunes para mejor diagnóstico
        const char* pinName = "Genérico";
        if (step.pin == 15) pinName = "Piedra difusora";
        else if (step.pin == 25) pinName = "Calefactor agua";
        else if (step.pin == 19) pinName = "Bomba agua";
        else if (step.pin == 18) pinName = "LED amplio espectro";
        else if (step.pin == 27) pinName = "Ventiladores";

        Serial.printf("📌 Pin %d (%s): DIGITAL %s (lógica %s: %s)\n",
                     step.pin,
                     pinName,
                     pinValue == LOW ? "LOW" : "HIGH",
                     isInverted ? "invertida" : "directa",
                     step.power == ON ? "ON" : "OFF");

        // Verificar estado del pin después de escritura
        int readBack = digitalRead(step.pin);
        if (readBack != pinValue) {
            Serial.printf("⚠️  ADVERTENCIA: Pin %d no cambió correctamente (esperado=%d, leído=%d)\n",
                         step.pin, pinValue, readBack);
        }

        // OFF commands complete immediately (no need to wait for duration)
        if (step.power == OFF) {
            Serial.printf("✅ Pin %d: OFF completado inmediatamente\n", step.pin);
            step.status = STEP_OK;
            return;
        }
    } else if (step.mode == PWM) {
        PWMManager::ensureAttached(step.pin);
        PWMManager::writeDuty(step.pin, step.dutyCycle);

        // Logging detallado para PWM
        const char* pinName = "Genérico";
        if (step.pin == 27) pinName = "Ventiladores";

        Serial.printf("📌 Pin %d (%s): PWM duty=%d/255 (~%d%%)\n",
                     step.pin, pinName, step.dutyCycle, (int)((step.dutyCycle / 255.0) * 100));

        // NOTA: Pin 27 (ventiladores) conectado vía MOSFET - soporta control PWM directo
        if (step.pin == 27) {
            Serial.println("✅ Ventiladores (pin 27): PWM directo vía MOSFET (0=OFF, 255=ON)");
        }

        // OFF commands (duty=0) complete immediately and release PWM channel
        if (step.dutyCycle == 0) {
            // 🔥 CRÍTICO: Liberar canal LEDC para permitir reutilización del pin
            PWMManager::detachIfAttached(step.pin);
            Serial.printf("🔓 Pin %d: PWM OFF (duty=0) - Canal liberado inmediatamente\n", step.pin);
            step.status = STEP_OK;
            return;
        }
    }
}

void JobScheduler::completeJob(Job& job) {
    Serial.println("═══════════════════════════════════════");
    Serial.printf("🏁 JOB COMPLETADO: %s\n", job.commandId.c_str());

    // Determine completion type
    if (job.hasError()) {
        job.completionType = JOB_COMPLETED_ERROR;
        Serial.println("⚠️  Completado con errores");
    } else if (job.isCancelled()) {
        job.completionType = JOB_COMPLETED_CANCELLED;
        Serial.println("⚠️  Completado (cancelado)");
    } else {
        Serial.println("✅ Completado exitosamente");
    }

    // Report completion via MQTT
    JobNotifier::publishCompletion(job);

    Serial.println("═══════════════════════════════════════");
    Serial.printf("📊 Jobs activos restantes: %d\n", activeJobs.size() - 1);
}

void JobScheduler::reportCompletionsBatch(const std::vector<Job>& jobs) {
    Serial.println("═══════════════════════════════════════");
    Serial.printf("🏁 BATCH DE JOBS COMPLETADOS: %d jobs\n", jobs.size());

    // Log each completed job
    for (const auto& job : jobs) {
        Serial.printf("   ✅ %s", job.commandId.c_str());

        if (job.hasError()) {
            Serial.println(" (con errores)");
        } else if (job.isCancelled()) {
            Serial.println(" (cancelado)");
        } else {
            Serial.println(" (exitoso)");
        }
    }

    // Report all completions in a single batch via MQTT
    JobNotifier::publishCompletionsBatch(jobs);

    Serial.println("═══════════════════════════════════════");
    Serial.printf("📊 Jobs activos restantes: %d\n", activeJobs.size());
}

// ========================================
// PREEMPTION/CANCELLATION LOGIC
// ========================================

bool JobScheduler::isOffCommand(const Step& step) {
    // A step is an OFF command if:
    // 1. Digital mode with power=OFF, OR
    // 2. PWM mode with dutyCycle=0
    if (step.mode == DIGITAL && step.power == OFF) {
        return true;
    }
    if (step.mode == PWM && step.dutyCycle == 0) {
        return true;
    }
    return false;
}

void JobScheduler::cancelJobsOnPin(int pin) {
    int cancelledCount = 0;

    for (auto it = activeJobs.begin(); it != activeJobs.end(); ) {
        Job& job = *it;
        bool jobAffected = false;

        // Check if any step in this job uses the pin
        for (auto& step : job.steps) {
            if (step.pin == pin && (step.status == STEP_PENDING || step.status == STEP_IN_PROGRESS)) {
                step.status = STEP_CANCELLED;
                jobAffected = true;
                Serial.printf("🚫 [PREEMPT] Step cancelado en Job %s (Pin %d)\n",
                             job.commandId.c_str(), pin);
            }
        }

        // If the current step was cancelled, mark the job as completed (with cancellation)
        if (jobAffected) {
            job.completionType = JOB_COMPLETED_CANCELLED;
            it = activeJobs.erase(it);
            cancelledCount++;

            // Report cancellation
            JobNotifier::publishCompletion(job);
        } else {
            ++it;
        }
    }

    if (cancelledCount > 0) {
        Serial.printf("🚫 [PREEMPT] %d job(s) cancelado(s) en Pin %d\n", cancelledCount, pin);
    }
}

void JobScheduler::executeOffCommandImmediately(const Step& step) {
    Serial.printf("⚡ [INSTANT-OFF] Ejecutando apagado inmediato en Pin %d\n", step.pin);

    if (step.mode == DIGITAL) {
        // Configure pin ONLY for DIGITAL mode (PWM uses LEDC and shouldn't use pinMode)
        pinMode(step.pin, OUTPUT);

        int offValue;

        // EXCEPCIÓN: Pin 27 (ventiladores) usa lógica directa (MOSFET, no relé)
        if (step.pin == 27) {
            // DIRECT LOGIC: LOW = OFF
            offValue = LOW;
        } else {
            // INVERTED LOGIC: HIGH = OFF (para relés)
            offValue = HIGH;
        }

        digitalWrite(step.pin, offValue);
        Serial.printf("⚡ [INSTANT-OFF] Pin %d → %s (lógica %s: OFF)\n",
                     step.pin,
                     offValue == LOW ? "LOW" : "HIGH",
                     step.pin == 27 ? "directa" : "invertida");
    } else if (step.mode == PWM) {
        // For PWM, directly detach without pinMode (avoid conflict with LEDC)
        // detachIfAttached already sets duty to 0 before detaching
        PWMManager::detachIfAttached(step.pin);
        Serial.printf("⚡ [INSTANT-OFF] Pin %d: PWM apagado y canal liberado\n", step.pin);
    }
}

// ========================================
// JOB CONSOLIDATION - PREVENT DUPLICATES
// ========================================

int JobScheduler::findActiveJobIndexByCommandId(const String& commandId) {
    for (size_t i = 0; i < activeJobs.size(); i++) {
        if (activeJobs[i].commandId == commandId) {
            return (int)i;
        }
    }
    return -1;  // Not found
}

bool JobScheduler::extendOrUpdateJob(Job& existingJob, Job& newJob) {
    Serial.printf("🔄 [CONSOLIDATE] Job duplicado detectado: %s\n", existingJob.commandId.c_str());

    // Get current step of existing job
    Step* currentStep = existingJob.getCurrentStep();

    if (currentStep == nullptr) {
        Serial.println("⚠️  [CONSOLIDATE] Job existente ya completado - ignorando consolidación");
        return false;
    }

    // Get equivalent step from new job (assume same pin/actuator)
    if (newJob.steps.empty()) {
        Serial.println("⚠️  [CONSOLIDATE] Nuevo job sin steps - ignorando consolidación");
        return false;
    }

    Step& newStep = newJob.steps[0];  // First step of new job

    // Validate that both jobs control the same pin
    if (currentStep->pin != newStep.pin) {
        Serial.printf("⚠️  [CONSOLIDATE] Pines diferentes (actual=%d, nuevo=%d) - no se puede consolidar\n",
                     currentStep->pin, newStep.pin);
        return false;
    }

    // Calculate new end time based on new duration
    unsigned long now = millis();
    unsigned long newEndTime = now + newStep.duration;

    // Only extend if new duration is longer than remaining time
    if (newEndTime > currentStep->endTime) {
        unsigned long oldRemainingTime = (currentStep->endTime > now) ? (currentStep->endTime - now) : 0;
        unsigned long newRemainingTime = newStep.duration;

        Serial.printf("🔄 [CONSOLIDATE] Extendiendo duración:\n");
        Serial.printf("   - Tiempo restante anterior: %.2f s\n", oldRemainingTime / 1000.0);
        Serial.printf("   - Nueva duración solicitada: %.2f s\n", newRemainingTime / 1000.0);

        // Update end time
        currentStep->endTime = newEndTime;
        currentStep->duration = newStep.duration;

        Serial.printf("   ✅ Duración extendida a %.2f s\n", (newEndTime - now) / 1000.0);
        return true;
    } else {
        Serial.printf("🔄 [CONSOLIDATE] Duración nueva (%.2f s) menor o igual que restante (%.2f s) - no se extiende\n",
                     newStep.duration / 1000.0,
                     (currentStep->endTime - now) / 1000.0);
        return false;
    }
}

// ========================================
// MQTT JOB SCHEDULE PROCESSING
// ========================================

void JobScheduler::processJobSchedule(const JsonDocument& payload) {
    Serial.println("═══════════════════════════════════════");
    Serial.println("📥 PROCESANDO JOB SCHEDULE");

    // Validate esp32Id
    if (!payload.containsKey("esp32Id") || payload["esp32Id"].as<String>() != ESP32_ID) {
        Serial.println("❌ ERROR: esp32Id inválido o no coincide");
        return;
    }

    // Extract queue array (actuator-service sends "queue" not "jobs")
    if (!payload.containsKey("queue")) {
        Serial.println("❌ ERROR: Payload no contiene 'queue'");
        return;
    }

    JsonArrayConst jobsArray = payload["queue"].as<JsonArrayConst>();
    Serial.printf("📦 Jobs en cola recibidos: %d\n", jobsArray.size());

    // Parse and add all jobs to activeJobs directly (no queue, no consolidation)
    for (JsonVariantConst jobVariant : jobsArray) {
        JsonObjectConst jobObj = jobVariant.as<JsonObjectConst>();

        Job newJob;
        newJob.commandId = jobObj["commandId"].as<String>();
        newJob.baseId = jobObj["baseId"] | newJob.commandId;
        newJob.queueTime = millis();

        Serial.printf("🔹 Job: %s\n", newJob.commandId.c_str());

        // Parse steps
        JsonArrayConst stepsArray = jobObj["steps"].as<JsonArrayConst>();
        bool hasOffCommands = false;

        for (JsonVariantConst stepVariant : stepsArray) {
            JsonObjectConst stepObj = stepVariant.as<JsonObjectConst>();

            Step step;
            step.pin = String(stepObj["pin"].as<String>()).toInt();
            step.mode = (stepObj["mode"].as<String>() == "PWM") ? PWM : DIGITAL;

            // Parse power field (puede ser "ON", "OFF", null, o "null")
            if (stepObj.containsKey("power") && !stepObj["power"].isNull()) {
                String powerStr = stepObj["power"].as<String>();
                // Manejar tanto null real como "null" string
                if (powerStr == "null" || powerStr == "") {
                    step.power = OFF;  // Se auto-detectará para PWM
                    Serial.printf("   [PARSE] power='%s' (null/vacío) → OFF temporal\n", powerStr.c_str());
                } else {
                    step.power = (powerStr == "ON") ? ON : OFF;
                    Serial.printf("   [PARSE] power='%s' → %s\n", powerStr.c_str(), step.power == ON ? "ON" : "OFF");
                }
            } else {
                // Si power no existe o es null real
                step.power = OFF;
                Serial.println("   [PARSE] power=null (ausente) → OFF temporal");
            }

            // Duty cycle: backend sends 0-100, convert to 0-255
            if (stepObj.containsKey("dutyCycle")) {
                double percent = stepObj["dutyCycle"].as<double>();
                step.dutyCycle = (int)((percent / 100.0) * 255.0);
                Serial.printf("   [PARSE] dutyCycle=%d%% → duty=%d/255\n", (int)percent, step.dutyCycle);

                // AUTO-DETECT: Si es PWM con dutyCycle > 0, se considera ON
                if (step.mode == PWM && step.dutyCycle > 0) {
                    step.power = ON;
                    Serial.printf("   [AUTO-DETECT] PWM con duty=%d → power=ON ✓\n", step.dutyCycle);
                }
            }

            // Duration: backend sends seconds (decimal), convert to ms
            double durationSec = stepObj["duration"].as<double>();
            step.duration = (unsigned long)(durationSec * 1000.0);
            step.status = STEP_PENDING;

            // Log ANTES del move (después del move, step está indefinido)
            Serial.printf("   Pin %d, %s, %s, Duration: %lu ms\n",
                         step.pin,
                         step.mode == DIGITAL ? "DIGITAL" : "PWM",
                         step.power == ON ? "ON" : "OFF",
                         step.duration);

            newJob.steps.emplace_back(std::move(step));  // Move para evitar copia

            // Check if this is an OFF command (usar referencia al step ya movido)
            const Step& addedStep = newJob.steps.back();
            if (isOffCommand(addedStep)) {
                hasOffCommands = true;
            }
        }

        // PREEMPTION LOGIC: If this job contains OFF commands, apply preemption
        if (hasOffCommands) {
            Serial.println("🚫 [PREEMPT] Job contiene comandos OFF - aplicando prelación");

            // Process each OFF command with preemption
            for (auto& step : newJob.steps) {
                if (isOffCommand(step)) {
                    // Step 1: Cancel all active jobs on this pin
                    cancelJobsOnPin(step.pin);

                    // Step 2: Execute OFF command immediately (ignore duration)
                    executeOffCommandImmediately(step);

                    // Step 3: Mark this step as completed immediately
                    step.status = STEP_OK;
                    Serial.printf("⚡ [PREEMPT] Comando OFF en Pin %d ejecutado y completado instantáneamente\n", step.pin);
                }
            }

            // Mark the entire OFF job as completed
            newJob.currentStepIndex = newJob.steps.size();  // All steps processed
            newJob.completionType = JOB_COMPLETED_NORMAL;

            // Report completion immediately
            JobNotifier::publishCompletion(newJob);
            Serial.printf("✅ Job OFF %s completado instantáneamente (no agregado a cola activa)\n",
                         newJob.commandId.c_str());
        } else {
            // Normal job (not an OFF command) - check for duplicates before adding
            int existingJobIndex = findActiveJobIndexByCommandId(newJob.commandId);

            if (existingJobIndex >= 0) {
                // Job with same commandId already exists - consolidate/extend
                Job& existingJob = activeJobs[existingJobIndex];

                if (extendOrUpdateJob(existingJob, newJob)) {
                    Serial.printf("✅ Job %s consolidado (extendido/actualizado)\n", newJob.commandId.c_str());
                } else {
                    Serial.printf("ℹ️  Job %s ya existe - duplicado ignorado\n", newJob.commandId.c_str());
                }
            } else {
                // New unique job - add to active jobs for concurrent execution
                Serial.printf("✅ Job %s iniciado (total activos: %d)\n",
                             newJob.commandId.c_str(), activeJobs.size() + 1);
                activeJobs.emplace_back(std::move(newJob));  // Move para evitar copia
            }
        }
    }

    Serial.println("═══════════════════════════════════════");
}

// ========================================
// EMERGENCY STOP
// ========================================

void JobScheduler::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA - Deteniendo todos los jobs");

    // Clear all active jobs
    activeJobs.clear();

    // Turn off all pins (safety)
    for (int pin = 0; pin < 40; pin++) {
        pinMode(pin, OUTPUT);

        // EXCEPCIÓN: Pin 27 (ventiladores) usa lógica directa (MOSFET, no relé)
        if (pin == 27) {
            // DIRECT LOGIC: LOW = OFF
            digitalWrite(pin, LOW);
        } else {
            // INVERTED LOGIC: All relays are active-LOW, so HIGH = OFF
            digitalWrite(pin, HIGH);
        }

        PWMManager::detachIfAttached(pin);
    }

    Serial.println("✅ Parada de emergencia completada (pines apagados según su lógica)");
}

// ========================================
// STATUS METHODS
// ========================================

bool JobScheduler::isBusy() {
    return !activeJobs.empty();
}

int JobScheduler::getQueueSize() {
    return activeJobs.size();
}

void JobScheduler::printStatus() {
    Serial.println("📊 JobScheduler Status:");
    Serial.printf("   ⚙️  Jobs activos: %d\n", activeJobs.size());

    if (activeJobs.empty()) {
        Serial.println("   ✅ No hay jobs en ejecución");
        return;
    }

    unsigned long now = millis();

    for (const auto& job : activeJobs) {
        Step* currentStep = const_cast<Job&>(job).getCurrentStep();

        if (currentStep != nullptr) {
            unsigned long remainingTime = (currentStep->endTime > now) ? (currentStep->endTime - now) : 0;

            Serial.printf("   🔹 %s\n", job.commandId.c_str());
            Serial.printf("      Pin: %d, Step: %d/%d, Restante: %.1f s\n",
                         currentStep->pin,
                         job.currentStepIndex + 1,
                         (int)job.steps.size(),
                         remainingTime / 1000.0);
        } else {
            Serial.printf("   🔹 %s (completando...)\n", job.commandId.c_str());
        }
    }
}
// ========================================
// NON-BLOCKING SPECIALIZED ROUTINES
// ========================================

void JobScheduler::updateHeaterMonitoring(Step& step) {
    if (sensorManager == nullptr || step.heaterState == nullptr) {
        step.status = STEP_ERROR;
        return;
    }

    const float HEATER_OFF_TEMP = 18.5f;
    const float HEATER_EMERGENCY_TEMP = 25.0f;
    const unsigned long MONITOR_INTERVAL = 3000;  // 3 seconds

    unsigned long now = millis();

    // Check monitoring interval
    if (now - step.heaterState->lastMonitorTime < MONITOR_INTERVAL) {
        return;  // Not time to monitor yet
    }

    step.heaterState->lastMonitorTime = now;

    // Read temperature
    float currentTemp = sensorManager->readTemperature();

    if (isnan(currentTemp)) {
        Serial.println("🔥 [HEATER-MONITOR] Lectura falló");
        return;
    }

    // Add to buffer
    step.heaterState->tempBuffer[step.heaterState->tempBufferIndex] = currentTemp;
    step.heaterState->tempBufferIndex = (step.heaterState->tempBufferIndex + 1) % 10;
    if (!step.heaterState->tempBufferFull && step.heaterState->tempBufferIndex == 0) {
        step.heaterState->tempBufferFull = true;
    }

    // Calculate average
    float avgTemp = 0.0f;
    int count = step.heaterState->tempBufferFull ? 10 : step.heaterState->tempBufferIndex;
    if (count > 0) {
        for (int i = 0; i < count; i++) {
            avgTemp += step.heaterState->tempBuffer[i];
        }
        avgTemp /= count;
    }

    // EMERGENCY SHUTDOWN
    if (currentTemp >= HEATER_EMERGENCY_TEMP) {
        Serial.printf("🔴 [HEATER] EMERGENCIA: %.1f°C ≥ %.1f°C - apagando\n",
                     currentTemp, HEATER_EMERGENCY_TEMP);
        digitalWrite(PIN_RELAY_HEATER, HIGH);
        step.status = STEP_OK;
        return;
    }

    // TARGET REACHED
    if (count > 0 && avgTemp >= HEATER_OFF_TEMP) {
        Serial.printf("🔴 [HEATER] Objetivo alcanzado: %.1f°C promedio - apagando\n", avgTemp);
        digitalWrite(PIN_RELAY_HEATER, HIGH);
        step.status = STEP_OK;
        return;
    }

    // Log progress
    Serial.printf("🔥 [HEATER] Temp: %.1f°C, Promedio: %.1f°C (%d/%d)\n",
                 currentTemp, avgTemp, count, 10);
}

void JobScheduler::updateHumidifierCycle(Step& step) {
    if (step.humidifierState == nullptr) return;

    const unsigned long FAN_OFF_DURATION = 40000;  // 40 seconds
    const unsigned long FAN_ON_DURATION = 15000;   // 15 seconds

    unsigned long now = millis();
    unsigned long elapsed = now - step.humidifierState->lastFanCycleTime;

    if (step.humidifierState->fanOn) {
        // Fan is ON - check if it's time to turn OFF
        if (elapsed >= FAN_ON_DURATION) {
            digitalWrite(PIN_HUMID_POWER, HIGH);  // Fan OFF
            step.humidifierState->fanOn = false;
            step.humidifierState->lastFanCycleTime = now;
            Serial.println("🌬️  [HUMID] Ventilador OFF - esperando 40s");
        }
    } else {
        // Fan is OFF - check if it's time to turn ON
        if (elapsed >= FAN_OFF_DURATION) {
            digitalWrite(PIN_HUMID_POWER, LOW);  // Fan ON
            step.humidifierState->fanOn = true;
            step.humidifierState->lastFanCycleTime = now;
            Serial.println("🌬️  [HUMID] Ventilador ON - duración 15s");
        }
    }
}
