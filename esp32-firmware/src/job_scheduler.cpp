#include "job_scheduler.h"
#include "mqtt_handler.h"
#include "config.h"

// Global instance
JobScheduler jobScheduler;

JobScheduler::JobScheduler() : schedulerMutex(nullptr), mqttHandler(nullptr) {
    // Initialize channels
    for (int i = 0; i < MAX_CHANNELS; i++) {
        channels[i] = ChannelState(i);
        channelTasks[i] = nullptr;
    }
}

JobScheduler::~JobScheduler() {
    if (schedulerMutex) {
        vSemaphoreDelete(schedulerMutex);
    }
}

void JobScheduler::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
    Serial.println("✅ MQTT handler vinculado al JobScheduler");
}

void JobScheduler::begin() {
    // Create mutex for thread-safe access
    schedulerMutex = xSemaphoreCreateMutex();
    if (schedulerMutex == nullptr) {
        Serial.println("❌ Error creating scheduler mutex");
        return;
    }
    
    // Create tasks for each channel
    for (int i = 0; i < MAX_CHANNELS; i++) {
        String taskName = "ChannelTask" + String(i);
        xTaskCreate(
            channelTaskWrapper,
            taskName.c_str(),
            4096,  // Stack size
            &channels[i],  // Parameter
            1,     // Priority
            &channelTasks[i]
        );
    }
    
    Serial.printf("✅ JobScheduler inicializado - %d canales\n", MAX_CHANNELS);
}

void JobScheduler::channelTaskWrapper(void* parameter) {
    ChannelState* channel = (ChannelState*)parameter;
    jobScheduler.channelTask(channel->channelId);
}

void JobScheduler::channelTask(int channelId) {
    Serial.printf("🔄 Canal %d iniciado\n", channelId);
    
    while (true) {
        if (xSemaphoreTake(schedulerMutex, portMAX_DELAY)) {
            ChannelState& channel = channels[channelId];
            
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
                            channel.isRunning = true;
                            
                            Serial.printf("▶️ Canal %d: Iniciando step %d del job %s\n", 
                                        channelId, currentJob->currentStepIndex, 
                                        currentJob->commandId.c_str());
                            
                            xSemaphoreGive(schedulerMutex);
                            executeStep(currentStep);
                            xSemaphoreTake(schedulerMutex, portMAX_DELAY);
                            
                        } else if (currentStep.status == STEP_IN_PROGRESS) {
                            // Check if step should timeout
                            unsigned long elapsed = millis() - currentStep.startTime;
                            if (elapsed >= currentStep.duration + STEP_TIMEOUT_TOLERANCE) {
                                currentStep.status = STEP_OK;
                                String logEntry = getCurrentTimestamp() + " - Step completado por timeout";
                                currentStep.executionLog.push_back(logEntry);
                                
                                Serial.printf("⏰ Canal %d: Step timeout, avanzando\n", channelId);
                            }
                        }
                        
                        // Check if current step is done
                        if (currentStep.status == STEP_OK || currentStep.status == STEP_CANCELLED || currentStep.status == STEP_ERROR) {
                            currentJob->currentStepIndex++;
                            
                            if (currentJob->isCompleted()) {
                                // Job completed - publish completion
                                channel.isRunning = false;
                                Serial.printf("✅ Canal %d: Job %s completado\n", 
                                            channelId, currentJob->commandId.c_str());
                                
                                xSemaphoreGive(schedulerMutex);
                                publishCompletion(*currentJob);
                                xSemaphoreTake(schedulerMutex, portMAX_DELAY);
                                
                                channel.removeCompletedJob();
                            }
                        }
                    }
                }
            } else {
                channel.isRunning = false;
            }
            
            xSemaphoreGive(schedulerMutex);
        }
        
        vTaskDelay(pdMS_TO_TICKS(100)); // Check every 100ms
    }
}

void JobScheduler::executeStep(Step& step) {
    Serial.printf("🔧 Ejecutando step en pin %d, mode %d, power %d, duty %d, duration %lu\n",
                  step.pin, step.mode, step.power, step.dutyCycle, step.duration);
    
    // Configure pin
    pinMode(step.pin, OUTPUT);
    
    if (step.mode == DIGITAL) {
        digitalWrite(step.pin, step.power == ON ? HIGH : LOW);
    } else if (step.mode == PWM) {
        if (step.power == ON) {
            analogWrite(step.pin, step.dutyCycle);
        } else {
            analogWrite(step.pin, 0);
        }
    }
    
    // Wait for duration (non-blocking)
    unsigned long startTime = millis();
    while (millis() - startTime < step.duration) {
        vTaskDelay(pdMS_TO_TICKS(10));
        
        // Check if step was cancelled externally
        if (step.status == STEP_CANCELLED) {
            break;
        }
    }
    
    // Turn off the pin after duration
    digitalWrite(step.pin, LOW);
    
    if (step.status == STEP_IN_PROGRESS) {
        step.status = STEP_OK;
    }
}

void JobScheduler::processJobSchedule(const DynamicJsonDocument& payload) {
    if (!payload.containsKey("jobSchedule")) {
        Serial.println("❌ No jobSchedule en el payload");
        return;
    }
    
    JsonArrayConst jobScheduleArray = payload["jobSchedule"];
    
    for (size_t i = 0; i < jobScheduleArray.size(); i++) {
        JsonVariantConst channelVariant = jobScheduleArray[i];
        if (!channelVariant.is<JsonObject>()) continue;
        JsonObjectConst channelObj = channelVariant;
        if (!channelObj.containsKey("channel") || !channelObj.containsKey("queue")) {
            continue;
        }
        
        int channelId = channelObj["channel"];
        if (channelId < 0 || channelId >= MAX_CHANNELS) {
            Serial.printf("❌ Canal inválido: %d\n", channelId);
            continue;
        }
        
        JsonArrayConst queueArray = channelObj["queue"];
        
        if (xSemaphoreTake(schedulerMutex, portMAX_DELAY)) {
            ChannelState& channel = channels[channelId];
            
            // Process each job in the queue
            for (JsonObjectConst jobObj : queueArray) {
                Job newJob;
                newJob.commandId = jobObj["commandId"].as<String>();
                newJob.baseId = jobObj["baseId"].as<String>();
                newJob.queueTime = millis();
                
                // Parse steps
                JsonArrayConst stepsArray = jobObj["steps"];
                for (JsonObjectConst stepObj : stepsArray) {
                    Step step;
                    step.pin = stepObj["pin"];
                    
                    String modeStr = stepObj["mode"];
                    step.mode = (modeStr == "PWM") ? PWM : DIGITAL;
                    
                    String powerStr = stepObj["power"];
                    step.power = (powerStr == "ON") ? ON : OFF;
                    
                    if (stepObj.containsKey("dutyCycle") && !stepObj["dutyCycle"].isNull()) {
                        step.dutyCycle = stepObj["dutyCycle"];
                    }
                    
                    step.duration = stepObj["duration"];
                    step.status = STEP_PENDING;
                    
                    newJob.steps.push_back(step);
                }
                
                // Apply scheduler rules
                bool jobProcessed = false;
                
                // Check for control routine (power OFF or dutyCycle 0 on first step)
                if (!newJob.steps.empty()) {
                    Step& firstStep = newJob.steps[0];
                    if (firstStep.power == OFF || firstStep.dutyCycle == 0) {
                        // This is a control routine - cancel jobs on the same pin
                        cancelJobsOnPin(firstStep.pin, "Cancelado por rutina de control");
                    }
                }
                
                // Check for consolidation
                if (channel.hasActiveJob()) {
                    Job* currentJob = channel.getCurrentJob();
                    if (currentJob && canConsolidate(newJob, *currentJob)) {
                        // Consolidate
                        std::vector<String> logs;
                        consolidateJob(*currentJob, newJob, logs);
                        
                        publishNotification("consolidated", channelId, 
                                          newJob.commandId, currentJob->commandId, logs);
                        jobProcessed = true;
                        
                        Serial.printf("🔄 Consolidado job %s en canal %d\n", 
                                    newJob.commandId.c_str(), channelId);
                    } else {
                        // Queue the job
                        if (channel.jobQueue.size() < MAX_JOBS_PER_CHANNEL) {
                            channel.jobQueue.push_back(newJob);
                            
                            std::vector<String> logs;
                            logs.push_back(getCurrentTimestamp() + " - Job encolado, rutina actual ya avanzó");
                            
                            publishNotification("queued", channelId, 
                                              newJob.commandId, "", logs);
                            jobProcessed = true;
                            
                            Serial.printf("📝 Encolado job %s en canal %d\n", 
                                        newJob.commandId.c_str(), channelId);
                        }
                    }
                } else {
                    // Channel is free - add job directly
                    if (channel.jobQueue.size() < MAX_JOBS_PER_CHANNEL) {
                        channel.jobQueue.push_back(newJob);
                        jobProcessed = true;
                        
                        Serial.printf("✅ Job %s añadido directamente al canal %d\n", 
                                    newJob.commandId.c_str(), channelId);
                    }
                }
                
                if (!jobProcessed) {
                    Serial.printf("❌ No se pudo procesar job %s\n", newJob.commandId.c_str());
                }
            }
            
            xSemaphoreGive(schedulerMutex);
        }
    }
}

bool JobScheduler::canConsolidate(const Job& incoming, const Job& current) {
    return (incoming.baseId == current.baseId) && (current.currentStepIndex == 0);
}

void JobScheduler::consolidateJob(Job& current, const Job& incoming, std::vector<String>& logs) {
    // Replace current job parameters with incoming ones
    for (size_t i = 0; i < current.steps.size() && i < incoming.steps.size(); i++) {
        Step& currentStep = current.steps[i];
        const Step& incomingStep = incoming.steps[i];
        
        if (currentStep.duration != incomingStep.duration) {
            String logEntry = getCurrentTimestamp() + " - Duración cambiada de " + 
                            String(currentStep.duration) + "ms → " + String(incomingStep.duration) + "ms";
            logs.push_back(logEntry);
            currentStep.duration = incomingStep.duration;
        }
        
        if (currentStep.dutyCycle != incomingStep.dutyCycle) {
            String logEntry = getCurrentTimestamp() + " - DutyCycle ajustado de " + 
                            String(currentStep.dutyCycle) + "% → " + String(incomingStep.dutyCycle) + "%";
            logs.push_back(logEntry);
            currentStep.dutyCycle = incomingStep.dutyCycle;
        }
        
        // Update other parameters as needed
        currentStep.power = incomingStep.power;
        currentStep.mode = incomingStep.mode;
    }
}

void JobScheduler::cancelJobsOnPin(int pin, const String& reason) {
    for (int i = 0; i < MAX_CHANNELS; i++) {
        ChannelState& channel = channels[i];
        
        for (auto& job : channel.jobQueue) {
            for (auto& step : job.steps) {
                if (step.pin == pin && step.status == STEP_IN_PROGRESS) {
                    step.status = STEP_CANCELLED;
                    String logEntry = getCurrentTimestamp() + " - " + reason;
                    step.executionLog.push_back(logEntry);
                    
                    Serial.printf("🛑 Cancelado step en pin %d por rutina de control\n", pin);
                }
            }
        }
    }
}

String JobScheduler::getCurrentTimestamp() {
    unsigned long currentTime = millis() / 1000;
    char timestamp[32];
    sprintf(timestamp, "2025-08-31T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    return String(timestamp);
}

void JobScheduler::publishNotification(const String& decision, int channelId, 
                                     const String& affectedCommand, const String& targetCommand,
                                     const std::vector<String>& logs) {
    DynamicJsonDocument doc(1024);
    
    doc["esp32Id"] = ESP32_ID;
    doc["timestamp"] = getCurrentTimestamp();
    doc["decision"] = decision;
    doc["channelId"] = channelId;
    doc["affectedCommand"] = affectedCommand;
    
    if (!targetCommand.isEmpty()) {
        doc["targetCommand"] = targetCommand;
    }
    
    JsonArray logArray = doc.createNestedArray("executionLog");
    for (const auto& logEntry : logs) {
        logArray.add(logEntry);
    }
    
    // Publish via MQTT if available, otherwise print to serial
    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishNotification(doc);
    } else {
        String payload;
        serializeJson(doc, payload);
        Serial.println("📤 Notification (offline): " + payload);
    }
}

void JobScheduler::publishCompletion(const Job& job) {
    DynamicJsonDocument doc(2048);
    
    doc["esp32Id"] = ESP32_ID;
    doc["commandId"] = job.commandId;
    
    if (!job.baseId.isEmpty()) {
        doc["baseId"] = job.baseId;
    }
    
    JsonArray stepsArray = doc.createNestedArray("steps");
    
    for (const auto& step : job.steps) {
        JsonObject stepObj = stepsArray.createNestedObject();
        stepObj["pin"] = step.pin;
        
        String statusStr = "ok";
        if (step.status == STEP_CANCELLED) statusStr = "cancelled";
        else if (step.status == STEP_ERROR) statusStr = "error";
        
        stepObj["status"] = statusStr;
        
        JsonArray logArray = stepObj.createNestedArray("executionLog");
        for (const auto& logEntry : step.executionLog) {
            logArray.add(logEntry);
        }
    }
    
    // Publish via MQTT if available, otherwise print to serial
    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletion(doc);
    } else {
        String payload;
        serializeJson(doc, payload);
        Serial.println("📤 Completion (offline): " + payload);
    }
}

void JobScheduler::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA - JobScheduler");
    
    if (xSemaphoreTake(schedulerMutex, portMAX_DELAY)) {
        for (int i = 0; i < MAX_CHANNELS; i++) {
            ChannelState& channel = channels[i];
            
            for (auto& job : channel.jobQueue) {
                for (auto& step : job.steps) {
                    if (step.status == STEP_IN_PROGRESS || step.status == STEP_PENDING) {
                        step.status = STEP_CANCELLED;
                        String logEntry = getCurrentTimestamp() + " - Cancelado por parada de emergencia";
                        step.executionLog.push_back(logEntry);
                        
                        // Turn off the pin immediately
                        digitalWrite(step.pin, LOW);
                    }
                }
            }
            
            channel.isRunning = false;
        }
        
        xSemaphoreGive(schedulerMutex);
    }
}

void JobScheduler::loop() {
    // Maintenance tasks if needed
    // This method is called from the main loop for any housekeeping
}

bool JobScheduler::isChannelBusy(int channelId) {
    if (channelId < 0 || channelId >= MAX_CHANNELS) return false;
    
    if (xSemaphoreTake(schedulerMutex, 100)) {  // Short timeout
        bool busy = channels[channelId].isRunning;
        xSemaphoreGive(schedulerMutex);
        return busy;
    }
    return false;
}

int JobScheduler::getQueueSize(int channelId) {
    if (channelId < 0 || channelId >= MAX_CHANNELS) return 0;
    
    if (xSemaphoreTake(schedulerMutex, 100)) {  // Short timeout
        int size = channels[channelId].jobQueue.size();
        xSemaphoreGive(schedulerMutex);
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