#include "job_scheduler.h"
#include "mqtt_handler.h"
#include "config.h"
#include "pwm_manager.h"
#include "job_consolidator.h"
#include "job_notifier.h"
#include "job_utils.h"

// Global instance
JobScheduler jobScheduler;

JobScheduler::JobScheduler() : globalMutex(nullptr), mqttHandler(nullptr) {
    // Initialize channels
    for (int i = 0; i < MAX_CHANNELS; i++) {
        channels[i] = ChannelState(i);
        channelTasks[i] = nullptr;
    }
    
    // Initialize PWM manager
    PWMManager::initialize();
}

JobScheduler::~JobScheduler() {
    if (globalMutex) {
        vSemaphoreDelete(globalMutex);
    }
    
    // Clean up channel mutexes
    for (int i = 0; i < MAX_CHANNELS; i++) {
        if (channels[i].channelMutex) {
            vSemaphoreDelete(channels[i].channelMutex);
        }
    }
}

void JobScheduler::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
    JobNotifier::setMQTTHandler(handler);
    Serial.println("✅ MQTT handler vinculado al JobScheduler");
}

void JobScheduler::begin() {
    // Create global mutex for cross-channel operations
    globalMutex = xSemaphoreCreateMutex();
    if (globalMutex == nullptr) {
        Serial.println("❌ Error creating global mutex");
        return;
    }
    
    // Create individual mutex for each channel
    for (int i = 0; i < MAX_CHANNELS; i++) {
        channels[i].channelMutex = xSemaphoreCreateMutex();
        if (channels[i].channelMutex == nullptr) {
            Serial.printf("❌ Error creating mutex for channel %d\n", i);
            return;
        }
    }
    
    // Clear all channels and reset state
    Serial.println("🧹 Limpiando estado de canales...");
    for (int i = 0; i < MAX_CHANNELS; i++) {
        channels[i].jobQueue.clear();
        channels[i].jobQueue.reserve(MAX_JOBS_PER_CHANNEL);  // Pre-allocate to reduce fragmentation
        channels[i].isRunning = false;
        channels[i].lastActivity = 0;
        Serial.printf("✅ Canal %d: queue limpiada, estado reset, capacidad reservada: %d\n", i, MAX_JOBS_PER_CHANNEL);
    }
    
    // Create tasks for each channel
    for (int i = 0; i < MAX_CHANNELS; i++) {
        String taskName = "ChannelTask" + String(i);
        xTaskCreate(
            channelTaskWrapper,
            taskName.c_str(),
            8192,  // Increased stack size for long-running jobs
            &channels[i],  // Parameter
            1,     // Priority
            &channelTasks[i]
        );
    }
    
    Serial.printf("✅ JobScheduler inicializado - %d canales limpiados\n", MAX_CHANNELS);
}

void JobScheduler::channelTaskWrapper(void* parameter) {
    ChannelState* channel = (ChannelState*)parameter;
    jobScheduler.channelTask(channel->channelId);
}

void JobScheduler::channelTask(int channelId) {
    Serial.printf("🔄 Canal %d iniciado\n", channelId);
    
    // Check initial stack
    UBaseType_t stackHighWaterMark = uxTaskGetStackHighWaterMark(NULL);
    Serial.printf("📊 Canal %d: stack inicial libre: %d bytes\n", channelId, stackHighWaterMark * sizeof(StackType_t));
    
    while (true) {
        // Use channel-specific mutex with shorter timeout (reduced contention)
        if (xSemaphoreTake(channels[channelId].channelMutex, pdMS_TO_TICKS(200))) { // 200ms timeout
            ChannelState& channel = channels[channelId];
            
            // Si no hay job activo y hay elementos en cola, tomar el primero
            if (!channel.hasActiveJob() && !channel.jobQueue.empty()) {
                Serial.printf("▶️ Canal %d: promoviendo job %s de la cola\n", 
                             channelId, channel.jobQueue[0].commandId.c_str());
                // El primer job en la cola se convierte automáticamente en el job activo
                // porque getCurrentJob() retorna &jobQueue[0] y hasActiveJob() verifica !isCompleted()
            }
            
            if (channel.hasActiveJob()) {
                Job* currentJob = channel.getCurrentJob();
                if (currentJob && !currentJob->isCompleted()) {
                    
                    // Get current step
                    if (currentJob->currentStepIndex < currentJob->steps.size()) {
                        Step& currentStep = currentJob->steps[currentJob->currentStepIndex];
                        
                        if (currentStep.status == STEP_PENDING) {
                            // Start step execution
                            currentStep.status = STEP_IN_PROGRESS;
                            currentStep.startTime = millis();
                            currentStep.endTime = currentStep.startTime + currentStep.duration;
                            channel.isRunning = true;
                            
                            Serial.printf("▶️ Canal %d: Iniciando step %d del job %s\n", 
                                        channelId, currentJob->currentStepIndex, 
                                        currentJob->commandId.c_str());
#ifdef DEBUG
                            Serial.printf("🔍 DEBUG: Step pin=%d, mode=%s, power=%s, duty=%d%%, duration=%lums\n",
                                        currentStep.pin, 
                                        (currentStep.mode == PWM) ? "PWM" : "DIGITAL",
                                        (currentStep.power == ON) ? "ON" : "OFF",
                                        currentStep.dutyCycle, currentStep.duration);
                            Serial.printf("🔍 DEBUG: Job queue size=%d, isCompleted=%s\n", 
                                        channel.jobQueue.size(), 
                                        currentJob->isCompleted() ? "true" : "false");
#endif
                            
                            xSemaphoreGive(channels[channelId].channelMutex);
                            executeStep(currentStep);
                            if (!xSemaphoreTake(channels[channelId].channelMutex, pdMS_TO_TICKS(500))) {
                                Serial.printf("⚠️ Canal %d: timeout retomando mutex después de executeStep\n", channelId);
                                continue; // Skip to next iteration
                            }
                            
                        } else if (currentStep.status == STEP_IN_PROGRESS) {
                            // Check if step endTime is reached (supports extensions)
                            unsigned long now = millis();
                            if (now >= currentStep.endTime) {
                                // Turn off the pin atomically when duration expires
                                if (currentStep.mode == DIGITAL) {
                                    digitalWrite(currentStep.pin, HIGH);  // Inverted logic for relay board
                                } else if (currentStep.mode == PWM) {
                                    PWMManager::writeDuty(currentStep.pin, 0);
                                    PWMManager::detachIfAttached(currentStep.pin);  // Detach to ensure clean reinit
                                }
                                
                                currentStep.status = STEP_OK;
                                String logEntry = JobUtils::getCurrentTimestamp() + " - Step completado (endTime reached)";
                                currentStep.executionLog.push_back(logEntry);
                                
                                #ifdef DEBUG
                                Serial.printf("⏰ Canal %d: Step duration completed, pin %d turned OFF\n", channelId, currentStep.pin);
                                #endif
                            }
                        }
                        
                        // Check if current step is done
                        if (currentStep.status == STEP_OK || currentStep.status == STEP_CANCELLED || currentStep.status == STEP_ERROR) {
                            currentJob->currentStepIndex++;
                            
                            if (currentJob->isCompleted()) {
                                // Job completed - remove immediately to prevent consolidation race
                                channel.isRunning = false;
                                Serial.printf("✅ Canal %d: Job %s completado\n", 
                                            channelId, currentJob->commandId.c_str());
                                
                                // Make a copy for publishing before removing from queue
                                Job completedJob = *currentJob;
                                channel.removeCompletedJob();
                                
                                xSemaphoreGive(channels[channelId].channelMutex);
                                JobNotifier::publishCompletion(completedJob);
                                xSemaphoreTake(channels[channelId].channelMutex, portMAX_DELAY);
                            }
                        }
                    }
                }
            } else {
                channel.isRunning = false;
            }
            
            xSemaphoreGive(channels[channelId].channelMutex);
        } else {
            Serial.printf("⚠️ Canal %d: timeout obteniendo mutex de canal\n", channelId);
            // Reduced timeout means less blocking, but we still need to monitor contention
            static unsigned long lastMutexWarning = 0;
            unsigned long now = millis();
            if (now - lastMutexWarning > 10000) { // Every 10 seconds
                Serial.printf("🚨 CHANNEL MUTEX CONTENTION: Canal %d esperando > 200ms\n", channelId);
                lastMutexWarning = now;
            }
        }
         
        vTaskDelay(pdMS_TO_TICKS(100)); // Check every 100ms
    }
}

void JobScheduler::executeStep(Step& step) {
    Serial.printf("🔧 Ejecutando step en pin %d, mode %s, power %s, duty %d, duration %lu ms\n",
                  step.pin, 
                  (step.mode == PWM) ? "PWM" : "DIGITAL",
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
    
    // Configure pin
    pinMode(step.pin, OUTPUT);
    
    if (step.mode == DIGITAL) {
        if (step.power == ON) {
            // Reset pin before activation for TIP41C BJT
            digitalWrite(step.pin, HIGH);  // Inverted logic for relay board
            Serial.printf("🔄 DIGITAL Pin %d: Reset LOW\n", step.pin);
            vTaskDelay(pdMS_TO_TICKS(50)); // 50ms reset pulse
            digitalWrite(step.pin, LOW);  // Inverted logic for relay board
            Serial.printf("📌 DIGITAL Pin %d: LOW (activado - lógica invertida)\n", step.pin);
        } else {
            digitalWrite(step.pin, HIGH);  // Inverted logic for relay board
            Serial.printf("📌 DIGITAL Pin %d: HIGH (desactivado - lógica invertida)\n", step.pin);
        }
    } else if (step.mode == PWM) {
        // Convert dutyCycle 0-100 to 0-255 for LEDC
        int duty8bit = map(step.dutyCycle, 0, 100, 0, 255);
        // Ensure PWM channel is attached once
        PWMManager::ensureAttached(step.pin);
        
        if (step.power == ON && step.dutyCycle > 0) {
            // Direct PWM update without detach/attach cycle
            PWMManager::writeDuty(step.pin, duty8bit);
            Serial.printf("🌀 PWM Pin %d: duty=%d%% (%d/255) activado\n", step.pin, step.dutyCycle, duty8bit);
        } else {
            PWMManager::writeDuty(step.pin, 0);
            Serial.printf("🌀 PWM Pin %d: OFF (duty=0)\n", step.pin);
        }
        // Note: PWM channel stays attached throughout job lifecycle
    }
    
    // executeStep is now atomic - only activates pin, channelTask handles duration/deactivation
    // Status remains STEP_IN_PROGRESS until channelTask completes it based on elapsed time
}

void JobScheduler::processJobSchedule(const JsonDocument& payload) {
    Serial.println("╔══════════════════════════════════════════════════════");
    Serial.println("║  🔄 PROCESANDO JOB SCHEDULE");
    Serial.println("╚══════════════════════════════════════════════════════");
    
    // 🌐 VALIDACIÓN DE CONECTIVIDAD OBLIGATORIA
    Serial.println("🌐 Verificando conectividad a internet...");
    if (!JobNotifier::hasInternetConnectivity()) {
        Serial.println("🛑 JOBS RECHAZADOS: Sin conectividad a internet");
        Serial.println("   → WiFi y MQTT deben estar conectados antes de procesar rutinas");
        Serial.println("   → Los jobs serán ignorados hasta que se restablezca la conexión");
        return;
    }
    Serial.println("✅ Conectividad verificada - Procediendo con jobs");
    
    // Diagnostic: Print current state before processing
    Serial.println("📊 ESTADO PREVIO DEL SCHEDULER:");
    for (int i = 0; i < MAX_CHANNELS; i++) {
        Serial.printf("   Canal %d: %s, %d jobs en cola, running=%s\n", 
                     i, channels[i].hasActiveJob() ? "OCUPADO" : "LIBRE",
                     channels[i].jobQueue.size(),
                     channels[i].isRunning ? "Sí" : "No");
    }
    
    if (!payload.containsKey("jobSchedule")) {
        Serial.println("❌ No jobSchedule en el payload");
        return;
    }
    
    JsonArrayConst jobScheduleArray = payload["jobSchedule"];
    Serial.printf("📋 JobSchedule contiene %d canal(es)\n", jobScheduleArray.size());
    
    int totalJobsProcessed = 0;
    int totalJobsQueued = 0;
    int totalJobsConsolidated = 0;
    int totalJobsDirectAdded = 0;

    Serial.printf("DEBUG: jobScheduleArray.size() = %d\n", jobScheduleArray.size());

    for (size_t i = 0; i < jobScheduleArray.size(); i++) {
        Serial.printf("DEBUG: entrando al for, i = %d\n", i);

        JsonVariantConst channelVariant = jobScheduleArray[i];
        if (channelVariant.isNull()) {
            Serial.printf("⚠️  Canal %d es null\n", i);
            continue;
        }

        if (!channelVariant.is<JsonObjectConst>()) {
            Serial.printf("⚠️  Canal %d no es un objeto JSON\n", i);
            continue;
        }

        JsonObjectConst channelObj = channelVariant.as<JsonObjectConst>();
        Serial.printf("✅ Canal %d válido, keys=%d\n", i, channelObj.size());

        // Validar claves esperadas
        if (!channelObj.containsKey("channel") || !channelObj.containsKey("queue")) {
            Serial.printf("⚠️  Canal %d no tiene 'channel' o 'queue'\n", i);
            continue;
        }

        int channelId = channelObj["channel"] | -1; // si no existe, -1
        if (channelId < 0 || channelId >= MAX_CHANNELS) {
            Serial.printf("❌ Canal inválido: %d (válidos: 0-%d)\n", channelId, MAX_CHANNELS-1);
            continue;
        }

        // Obtener queue
        JsonVariantConst queueVariant = channelObj["queue"];
        if (!queueVariant.is<JsonArrayConst>()) {
            Serial.printf("❌ Canal %d → 'queue' no es un array\n", channelId);

            // Debug extra de tipo real
            if (queueVariant.is<JsonObjectConst>()) Serial.println("   → Es un OBJECT");
            else if (queueVariant.is<const char*>()) Serial.println("   → Es un STRING");
            else if (queueVariant.is<int>()) Serial.println("   → Es un INT");
            else if (queueVariant.isNull()) Serial.println("   → Es NULL");
            else Serial.println("   → Tipo desconocido");

            continue;
        }

        JsonArrayConst queueArray = queueVariant.as<JsonArrayConst>();
        Serial.printf("✅ Canal %d → queue con %d job(s)\n", channelId, queueArray.size());

        if (queueArray.size() == 0) {
            Serial.printf("⚠️  Canal %d tiene 'queue' vacío\n", channelId);
            continue;
        }

        // Aquí ya puedes procesar queueArray
        Serial.printf("✅ Canal %d listo para procesar %d job(s)\n", channelId, queueArray.size());

        Serial.println("┌─────────────────────────────────────────────────");
        Serial.printf("│ 📍 PROCESANDO CANAL %d\n", channelId);
        Serial.printf("│ 📦 Jobs en cola recibida: %d\n", queueArray.size());
        Serial.printf("│ 🔄 Estado canal: %s\n", (channels[channelId].hasActiveJob() ? "OCUPADO" : "LIBRE"));
        Serial.printf("│ 📋 Jobs actuales en canal: %d\n", channels[channelId].jobQueue.size());
        Serial.println("└─────────────────────────────────────────────────");
        
        if (xSemaphoreTake(channels[channelId].channelMutex, pdMS_TO_TICKS(1000))) {
            ChannelState& channel = channels[channelId];
            
            // Process each job in the queue
            for (size_t jobIndex = 0; jobIndex < queueArray.size(); jobIndex++) {
                JsonVariantConst jobVariant = queueArray[jobIndex];
                if (!jobVariant.is<JsonObjectConst>()) continue;
                JsonObjectConst jobObj = jobVariant;
                totalJobsProcessed++;
                
                Job newJob;
                newJob.commandId = jobObj["commandId"].as<String>();
                newJob.baseId = jobObj["baseId"].as<String>();
                newJob.queueTime = millis();
                
                // Reserve aggressive capacity to reduce fragmentation
                newJob.steps.reserve(8);  // More capacity to handle complex jobs
                
                Serial.println("    ┌─────────────────────────────────────────");
                Serial.printf("    │ 🆔 JOB #%d: %s\n", totalJobsProcessed, newJob.commandId.c_str());
                Serial.printf("    │ 🏷️  BaseId: %s\n", newJob.baseId.c_str());
                Serial.printf("    │ ⏰ Queue time: %lu ms\n", newJob.queueTime);
                
                // Parse steps
                JsonArrayConst stepsArray = jobObj["steps"];
                for (JsonObjectConst stepObj : stepsArray) {
                    Step step;
                    
                    // Parse pin - handle both string and int
                    if (stepObj["pin"].is<int>()) {
                        step.pin = stepObj["pin"].as<int>();
                    } else {
                        step.pin = String(stepObj["pin"].as<const char*>()).toInt();
                    }
                    
                    String modeStr = stepObj["mode"];
                    step.mode = (modeStr == "PWM") ? PWM : DIGITAL;
                    
                    // Parse power - handle null values
                    if (stepObj["power"].isNull()) {
                        if (stepObj.containsKey("dutyCycle") && !stepObj["dutyCycle"].isNull()) {
                            int dutyCycle = stepObj["dutyCycle"];
                            step.power = (step.mode == PWM && dutyCycle > 0) ? ON : OFF;
                        } else {
                            step.power = OFF;
                        }
                    } else {
                        String powerStr = stepObj["power"].as<const char*>();
                        step.power = (powerStr == "ON") ? ON : OFF;
                    }
                    
                    if (stepObj.containsKey("dutyCycle") && !stepObj["dutyCycle"].isNull()) {
                        step.dutyCycle = stepObj["dutyCycle"];
                    }
                    
                    unsigned long durationSec = stepObj["duration"];
                    step.duration = durationSec * 1000UL;
                    
                    step.status = STEP_PENDING;
                    
                    // Reserve execution log capacity to reduce fragmentation
                    step.executionLog.reserve(16);  // More capacity for detailed logging
                    
                    newJob.steps.push_back(step);
                }
                
                Serial.printf("    │ 📝 Steps: %d step(s)\n", newJob.steps.size());
                for (size_t s = 0; s < newJob.steps.size(); s++) {
                    const Step& step = newJob.steps[s];
                    Serial.printf("    │   [%d] Pin %d, %s, %s, ", s, step.pin,
                                 (step.mode == PWM) ? "PWM" : "DIGITAL",
                                 (step.power == ON) ? "ON" : "OFF");
                    if (step.mode == PWM) {
                        Serial.printf("duty=%d%%, ", step.dutyCycle);
                    }
                    Serial.printf("duration=%lus\n", step.duration / 1000);
                }
                Serial.println("    └─────────────────────────────────────────");
                
                bool jobProcessed = false;
                
                // Check if this is a control routine
                if (JobConsolidator::isControlRoutine(newJob)) {
                    Step& firstStep = newJob.steps[0];
                    Serial.printf("    │ ⚠️  RUTINA DE CONTROL detectada en pin %d\n", firstStep.pin);
                    Serial.printf("    │ 🛑 Cancelando jobs existentes en pin %d\n", firstStep.pin);
                    cancelJobsOnPin(firstStep.pin, "Cancelado por rutina de control");
                }
                
                Serial.printf("    │ 🧠 DECISIÓN DEL SCHEDULER:\n");
                if (channel.hasActiveJob()) {
                    Job* currentJob = channel.getCurrentJob();
                    Serial.printf("    │   📊 Canal OCUPADO con job: %s\n", currentJob->commandId.c_str());
                    
                    if (currentJob && JobConsolidator::canConsolidate(newJob, *currentJob)) {
                        Serial.printf("    │   ✅ CONSOLIDACIÓN posible (mismo baseId + step 0 + no completado)\n");
                        Serial.printf("    │   🔄 Fusionando %s → %s\n", newJob.commandId.c_str(), currentJob->commandId.c_str());
#ifdef DEBUG
                        Serial.printf("    │   🔍 DEBUG: current job - stepIndex=%d, isCompleted=%s, steps.size=%d\n", 
                                    currentJob->currentStepIndex, 
                                    currentJob->isCompleted() ? "true" : "false",
                                    currentJob->steps.size());
#endif
                        
                        std::vector<String> logs;
                        JobConsolidator::consolidateJob(*currentJob, newJob, logs);
                        
                        // Mark job as modified through consolidation
                        currentJob->completionType = JOB_COMPLETED_MODIFIED;
                        
                        // Notification only includes summary, detailed logs are in step.executionLog
                        std::vector<String> notificationLogs;
                        notificationLogs.push_back(JobUtils::getCurrentTimestamp() + " - Job fusionado con cambios aplicados");
                        
                        JobNotifier::publishNotification("consolidated", channelId, 
                                          newJob.commandId, currentJob->commandId, notificationLogs);
                        
                        // Mark the incoming (extender) job as processed - it doesn't need to be queued
                        // since its changes have been applied to the current job
                        jobProcessed = true;
                        totalJobsConsolidated++;
                        
                        Serial.printf("    │   ✅ CONSOLIDADO: %s fusionado en canal %d\n", 
                                    newJob.commandId.c_str(), channelId);
                    } else {
                        Serial.printf("    │   ❌ CONSOLIDACIÓN imposible (baseId diferente, step > 0, completado o pin diferente)\n");
#ifdef DEBUG
                        Serial.printf("    │   🔍 DEBUG: baseId match=%s, stepIndex=%d, isCompleted=%s, pinMatch=%s\n",
                                    (newJob.baseId == currentJob->baseId) ? "true" : "false",
                                    currentJob->currentStepIndex,
                                    currentJob->isCompleted() ? "true" : "false",
                                    (!newJob.steps.empty() && !currentJob->steps.empty() && 
                                     newJob.steps[0].pin == currentJob->steps[0].pin) ? "true" : "false");
#endif
                        Serial.printf("    │   📋 ENCOLANDO job para esperar...\n");
                        
                        if (channel.jobQueue.size() < MAX_JOBS_PER_CHANNEL) {
                            channel.jobQueue.push_back(newJob);
                            
                            std::vector<String> logs;
                            logs.push_back(JobUtils::getCurrentTimestamp() + " - Job encolado, rutina actual ya avanzó");
                            
                            JobNotifier::publishNotification("queued", channelId, 
                                              newJob.commandId, "", logs);
                            jobProcessed = true;
                            totalJobsQueued++;
                            
                            Serial.printf("    │   ✅ ENCOLADO: %s en posición %d del canal %d\n", 
                                        newJob.commandId.c_str(), channel.jobQueue.size(), channelId);
                        }
                    }
                } else {
                    Serial.printf("    │   ✅ Canal LIBRE - ejecución directa\n");
                    if (channel.jobQueue.size() < MAX_JOBS_PER_CHANNEL) {
                        channel.jobQueue.push_back(newJob);
                        jobProcessed = true;
                        totalJobsDirectAdded++;
                        
                        Serial.printf("    │   ⚡ EJECUCIÓN DIRECTA: %s en canal %d\n", 
                                    newJob.commandId.c_str(), channelId);
                        Serial.printf("    │   🚀 Job listo para ejecutar inmediatamente\n");
                    } else {
                        Serial.printf("    │   🚫 Canal lleno (max %d jobs)\n", MAX_JOBS_PER_CHANNEL);
                    }
                }
                
                if (!jobProcessed) {
                    Serial.printf("❌ No se pudo procesar job %s\n", newJob.commandId.c_str());
                }
            }
            
            xSemaphoreGive(channels[channelId].channelMutex);
        }
    }
    
    Serial.println("╔══════════════════════════════════════════════════════");
    Serial.println("║  📊 RESUMEN DE PROCESAMIENTO");
    Serial.println("╠══════════════════════════════════════════════════════");
    Serial.printf("║  📋 Total jobs procesados: %d\n", totalJobsProcessed);
    Serial.printf("║  ⚡ Ejecución directa: %d\n", totalJobsDirectAdded);
    Serial.printf("║  📝 Encolados: %d\n", totalJobsQueued);
    Serial.printf("║  🔄 Consolidados: %d\n", totalJobsConsolidated);
    Serial.println("║");
    
    for (int c = 0; c < MAX_CHANNELS; c++) {
        Serial.printf("║  📍 Canal %d: %s (%d jobs en cola)\n", c,
                     channels[c].hasActiveJob() ? "OCUPADO" : "LIBRE",
                     channels[c].jobQueue.size());
    }
    Serial.println("╚══════════════════════════════════════════════════════");
}

void JobScheduler::cancelJobsOnPin(int pin, const String& reason) {
    // Use global mutex for cross-channel operation
    bool ownMutex = xSemaphoreTake(globalMutex, pdMS_TO_TICKS(500));
    
    for (int i = 0; i < MAX_CHANNELS; i++) {
        ChannelState& channel = channels[i];
        
        for (auto& job : channel.jobQueue) {
            bool jobAffected = false;
            for (auto& step : job.steps) {
                // Only cancel steps that are actually IN_PROGRESS on this pin
                if (step.pin == pin && step.status == STEP_IN_PROGRESS) {
                    step.status = STEP_CANCELLED;
                    String logEntry = JobUtils::getCurrentTimestamp() + " - " + reason;
                    step.executionLog.push_back(logEntry);
                    jobAffected = true;
                    
                    // Ensure pin is turned off safely
                    if (pin >= 0 && pin < 40) {
                        PWMManager::writeDuty(pin, 0);
                        PWMManager::detachIfAttached(pin);
                        digitalWrite(pin, HIGH);  // Inverted logic for relay board
                    } else {
                        Serial.printf("❌ ERROR: Pin %d inválido en cancelación\n", pin);
                    }
                    
                    Serial.printf("🛑 Cancelado step IN_PROGRESS en pin %d por rutina de control\n", pin);
                }
                // Don't touch PENDING steps - let them run normally when their turn comes
            }
            
            // Mark entire job as cancelled if any step was affected
            if (jobAffected) {
                job.completionType = JOB_COMPLETED_CANCELLED;
            }
        }
    }
    
    if (ownMutex) {
        xSemaphoreGive(globalMutex);
    }
}

void JobScheduler::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA - JobScheduler");
    
    if (xSemaphoreTake(globalMutex, portMAX_DELAY)) {
        for (int i = 0; i < MAX_CHANNELS; i++) {
            ChannelState& channel = channels[i];
            
            for (auto& job : channel.jobQueue) {
                // Mark job as cancelled
                job.completionType = JOB_COMPLETED_CANCELLED;
                
                for (auto& step : job.steps) {
                    if (step.status == STEP_IN_PROGRESS || step.status == STEP_PENDING) {
                        step.status = STEP_CANCELLED;
                        String logEntry = JobUtils::getCurrentTimestamp() + " - Cancelado por parada de emergencia";
                        step.executionLog.push_back(logEntry);
                        
                        // Turn off the pin immediately
                        if (step.pin >= 0 && step.pin < 40) {
                            digitalWrite(step.pin, HIGH);  // Inverted logic for relay board
                        } else {
                            Serial.printf("❌ ERROR: Pin %d inválido en emergencia\n", step.pin);
                        }
                    }
                }
            }
            
            channel.isRunning = false;
        }
        
        xSemaphoreGive(globalMutex);
    }
}

void JobScheduler::loop() {
    // Maintenance tasks if needed
    // This method is called from the main loop for any housekeeping
}

bool JobScheduler::isChannelBusy(int channelId) {
    if (channelId < 0 || channelId >= MAX_CHANNELS) return false;
    
    if (xSemaphoreTake(channels[channelId].channelMutex, pdMS_TO_TICKS(50))) {  // Short timeout
        bool busy = channels[channelId].isRunning;
        xSemaphoreGive(channels[channelId].channelMutex);
        return busy;
    }
    return false;
}

int JobScheduler::getQueueSize(int channelId) {
    if (channelId < 0 || channelId >= MAX_CHANNELS) return 0;
    
    if (xSemaphoreTake(channels[channelId].channelMutex, pdMS_TO_TICKS(50))) {  // Short timeout
        int size = channels[channelId].jobQueue.size();
        xSemaphoreGive(channels[channelId].channelMutex);
        return size;
    }
    return 0;
}

void JobScheduler::printStatus() {
    Serial.println("📊 JobScheduler Status:");
    for (int i = 0; i < MAX_CHANNELS; i++) {
        Serial.printf("  Canal %d: Running=%s, Queue=%d\n", 
                     i, isChannelBusy(i) ? "Sí" : "No", getQueueSize(i));
    }
}