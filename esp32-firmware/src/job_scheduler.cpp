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
}

void JobScheduler::setSensorManager(SensorManager* manager) {
    sensorManager = manager;
}

void JobScheduler::begin() {
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
            // SAFETY: Check absolute maximum timeout
            if (now >= step.absoluteMaxEndTime) {
                Serial.printf("SAFETY: Pin %d max timeout - Force shutdown\n", step.pin);

                if (step.mode == DIGITAL) {
                    int offValue = (step.pin == 27) ? LOW : HIGH;
                    digitalWrite(step.pin, offValue);
                } else if (step.mode == PWM) {
                    PWMManager::writeDuty(step.pin, 0);
                    PWMManager::detachIfAttached(step.pin);
                }

                if (step.heaterState) {
                    step.heaterState.reset();
                }
                if (step.humidifierState) {
                    step.humidifierState.reset();
                }

                pinCooldowns[step.pin] = now + PIN_COOLDOWN_DURATION;

                step.status = STEP_OK;
                return;
            }

            // Update specialized routines if needed
            if (step.pin == PIN_RELAY_HEATER && step.heaterState != nullptr) {
                updateHeaterMonitoring(step);
            } else if (step.pin == PIN_HUMID_POWER && step.humidifierState != nullptr) {
                updateHumidifierCycle(step);
            }

            // Check if step duration has elapsed
            if (now >= step.endTime) {
                step.status = STEP_OK;

                // Cleanup: Turn off pin
                if (step.pin == PIN_RELAY_HEATER) {
                    digitalWrite(PIN_RELAY_HEATER, HIGH);
                    if (step.heaterState) {
                        step.heaterState.reset();
                    }
                } else if (step.pin == PIN_HUMID_POWER) {
                    digitalWrite(PIN_HUMID_POWER, HIGH);
                    digitalWrite(PIN_HUMID_RELAY, HIGH);
                    if (step.humidifierState) {
                        step.humidifierState.reset();
                    }
                } else {
                    if (step.mode == DIGITAL) {
                        int offValue = (step.pin == 27) ? LOW : HIGH;
                        digitalWrite(step.pin, offValue);
                    } else if (step.mode == PWM) {
                        PWMManager::writeDuty(step.pin, 0);
                        PWMManager::detachIfAttached(step.pin);
                    }
                }
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

    if (step.pin < 0 || step.pin >= 40) {
        Serial.printf("ERROR: Pin %d out of range\n", step.pin);
        step.status = STEP_ERROR;
        return;
    }

    const unsigned long MIN_WATER_PUMP_TIME = 5000;
    const unsigned long MIN_LED_TIME = 3000;

    if (step.power == ON) {
        if (step.pin == 19 && step.duration < MIN_WATER_PUMP_TIME) {
            step.duration = MIN_WATER_PUMP_TIME;
        } else if (step.pin == 18 && step.duration < MIN_LED_TIME) {
            step.duration = MIN_LED_TIME;
        }
    }

    if (pinCooldowns.count(step.pin) > 0 && now < pinCooldowns[step.pin]) {
        step.status = STEP_CANCELLED;
        return;
    }

    step.status = STEP_IN_PROGRESS;
    step.startTime = now;
    step.endTime = now + step.duration;
    step.absoluteMaxEndTime = now + MAX_ABSOLUTE_STEP_DURATION;

    pinMode(step.pin, OUTPUT);

    // SPECIAL CASE: Heater with temperature monitoring
    if (step.pin == PIN_RELAY_HEATER && step.power == ON) {
        digitalWrite(PIN_RELAY_HEATER, LOW);
        step.heaterState.reset(new HeaterMonitorState());
        return;
    }

    // SPECIAL CASE: Humidifier with fan cycle
    if (step.pin == PIN_HUMID_POWER && step.power == ON) {
        pinMode(PIN_HUMID_RELAY, OUTPUT);
        pinMode(PIN_HUMID_POWER, OUTPUT);

        digitalWrite(PIN_HUMID_RELAY, LOW);
        delay(100);

        digitalWrite(PIN_HUMID_POWER, HIGH);

        step.humidifierState.reset(new HumidifierCycleState());
        step.humidifierState->lastFanCycleTime = now;
        step.humidifierState->fanOn = false;

        return;
    }

    // OFF commands for special pins
    if (step.pin == PIN_RELAY_HEATER && step.power == OFF) {
        digitalWrite(PIN_RELAY_HEATER, HIGH);
        step.status = STEP_OK;
        return;
    }

    if (step.pin == PIN_HUMID_POWER && step.power == OFF) {
        digitalWrite(PIN_HUMID_POWER, HIGH);
        digitalWrite(PIN_HUMID_RELAY, HIGH);
        step.status = STEP_OK;
        return;
    }

    // GENERIC: Digital or PWM control
    if (step.mode == DIGITAL) {
        int pinValue;

        if (step.pin == 27) {
            pinValue = (step.power == ON) ? HIGH : LOW;
        } else {
            pinValue = (step.power == ON) ? LOW : HIGH;
        }

        digitalWrite(step.pin, pinValue);

        if (step.power == OFF) {
            step.status = STEP_OK;
            return;
        }
    } else if (step.mode == PWM) {
        PWMManager::ensureAttached(step.pin);
        PWMManager::writeDuty(step.pin, step.dutyCycle);

        if (step.dutyCycle == 0) {
            PWMManager::detachIfAttached(step.pin);
            step.status = STEP_OK;
            return;
        }
    }
}

void JobScheduler::completeJob(Job& job) {
    if (job.hasError()) {
        job.completionType = JOB_COMPLETED_ERROR;
    } else if (job.isCancelled()) {
        job.completionType = JOB_COMPLETED_CANCELLED;
    }

    JobNotifier::publishCompletion(job);
}

void JobScheduler::reportCompletionsBatch(const std::vector<Job>& jobs) {
    JobNotifier::publishCompletionsBatch(jobs);
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
    for (auto it = activeJobs.begin(); it != activeJobs.end(); ) {
        Job& job = *it;
        bool jobAffected = false;

        for (auto& step : job.steps) {
            if (step.pin == pin && (step.status == STEP_PENDING || step.status == STEP_IN_PROGRESS)) {
                step.status = STEP_CANCELLED;
                jobAffected = true;
            }
        }

        if (jobAffected) {
            job.completionType = JOB_COMPLETED_CANCELLED;
            it = activeJobs.erase(it);
            JobNotifier::publishCompletion(job);
        } else {
            ++it;
        }
    }
}

void JobScheduler::executeOffCommandImmediately(const Step& step) {
    if (step.mode == DIGITAL) {
        pinMode(step.pin, OUTPUT);
        int offValue = (step.pin == 27) ? LOW : HIGH;
        digitalWrite(step.pin, offValue);
    } else if (step.mode == PWM) {
        PWMManager::detachIfAttached(step.pin);
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
    Step* currentStep = existingJob.getCurrentStep();

    if (currentStep == nullptr || newJob.steps.empty()) {
        return false;
    }

    Step& newStep = newJob.steps[0];

    if (currentStep->pin != newStep.pin) {
        return false;
    }

    unsigned long now = millis();
    unsigned long newEndTime = now + newStep.duration;

    if (newEndTime > currentStep->absoluteMaxEndTime) {
        return false;
    }

    currentStep->endTime = newEndTime;
    currentStep->duration = newStep.duration;

    PowerState oldPower = currentStep->power;
    int oldDutyCycle = currentStep->dutyCycle;
    PinMode oldMode = currentStep->mode;

    currentStep->power = newStep.power;
    currentStep->dutyCycle = newStep.dutyCycle;
    currentStep->mode = newStep.mode;

    bool parametersChanged = (oldPower != newStep.power) ||
                             (oldDutyCycle != newStep.dutyCycle) ||
                             (oldMode != newStep.mode);

    if (parametersChanged) {
        if (currentStep->mode == DIGITAL) {
            int pinValue = (currentStep->pin == 27) ?
                          (currentStep->power == ON ? HIGH : LOW) :
                          (currentStep->power == ON ? LOW : HIGH);
            digitalWrite(currentStep->pin, pinValue);
        } else if (currentStep->mode == PWM) {
            PWMManager::ensureAttached(currentStep->pin);
            PWMManager::writeDuty(currentStep->pin, currentStep->dutyCycle);
        }

        if (currentStep->pin == PIN_RELAY_HEATER && currentStep->power == ON) {
            if (!currentStep->heaterState) {
                currentStep->heaterState.reset(new HeaterMonitorState());
            }
        } else if (currentStep->pin == PIN_HUMID_POWER && currentStep->power == ON) {
            if (!currentStep->humidifierState) {
                currentStep->humidifierState.reset(new HumidifierCycleState());
                currentStep->humidifierState->lastFanCycleTime = millis();
                currentStep->humidifierState->fanOn = false;
            }
        }
    }

    return true;
}

// ========================================
// MQTT JOB SCHEDULE PROCESSING
// ========================================

void JobScheduler::processJobSchedule(const JsonDocument& payload) {
    if (!payload.containsKey("esp32Id") || payload["esp32Id"].as<String>() != ESP32_ID) {
        return;
    }

    if (!payload.containsKey("queue")) {
        return;
    }

    JsonArrayConst jobsArray = payload["queue"].as<JsonArrayConst>();


    for (JsonVariantConst jobVariant : jobsArray) {
        JsonObjectConst jobObj = jobVariant.as<JsonObjectConst>();

        Job newJob;
        newJob.commandId = jobObj["commandId"].as<String>();
        newJob.baseId = jobObj["baseId"] | newJob.commandId;
        newJob.queueTime = millis();

        JsonArrayConst stepsArray = jobObj["steps"].as<JsonArrayConst>();
        bool hasOffCommands = false;

        for (JsonVariantConst stepVariant : stepsArray) {
            JsonObjectConst stepObj = stepVariant.as<JsonObjectConst>();

            Step step;
            step.pin = String(stepObj["pin"].as<String>()).toInt();
            step.mode = (stepObj["mode"].as<String>() == "PWM") ? PWM : DIGITAL;

            if (stepObj.containsKey("power") && !stepObj["power"].isNull()) {
                String powerStr = stepObj["power"].as<String>();
                if (powerStr == "null" || powerStr == "") {
                    step.power = OFF;
                } else {
                    step.power = (powerStr == "ON") ? ON : OFF;
                }
            } else {
                step.power = OFF;
            }

            if (stepObj.containsKey("dutyCycle")) {
                double percent = stepObj["dutyCycle"].as<double>();
                step.dutyCycle = (int)((percent / 100.0) * 255.0);

                if (step.mode == PWM && step.dutyCycle > 0) {
                    step.power = ON;
                }
            }

            double durationSec = stepObj["duration"].as<double>();
            step.duration = (unsigned long)(durationSec * 1000.0);
            step.status = STEP_PENDING;

            newJob.steps.emplace_back(std::move(step));

            const Step& addedStep = newJob.steps.back();
            if (isOffCommand(addedStep)) {
                hasOffCommands = true;
            }
        }

        if (hasOffCommands) {

            for (auto& step : newJob.steps) {
                if (isOffCommand(step)) {
                    cancelJobsOnPin(step.pin);
                    executeOffCommandImmediately(step);
                    step.status = STEP_OK;
                }
            }

            newJob.currentStepIndex = newJob.steps.size();
            newJob.completionType = JOB_COMPLETED_NORMAL;
            JobNotifier::publishCompletion(newJob);
        } else {
            int existingJobIndex = findActiveJobIndexByCommandId(newJob.commandId);

            if (existingJobIndex >= 0) {
                Job& existingJob = activeJobs[existingJobIndex];
                extendOrUpdateJob(existingJob, newJob);
            } else {
                activeJobs.emplace_back(std::move(newJob));
            }
        }
    }
}

// ========================================
// EMERGENCY STOP
// ========================================

void JobScheduler::emergencyStop() {
    Serial.println("EMERGENCY STOP");

    activeJobs.clear();

    for (int pin = 0; pin < 40; pin++) {
        pinMode(pin, OUTPUT);

        if (pin == 27) {
            digitalWrite(pin, LOW);
        } else {
            digitalWrite(pin, HIGH);
        }

        PWMManager::detachIfAttached(pin);
    }
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
    // Status method - kept minimal for production
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
    const unsigned long MONITOR_INTERVAL = 3000;

    unsigned long now = millis();

    if (now - step.heaterState->lastMonitorTime < MONITOR_INTERVAL) {
        return;
    }

    step.heaterState->lastMonitorTime = now;

    float currentTemp = sensorManager->readTemperature();

    if (isnan(currentTemp)) {
        return;
    }

    step.heaterState->tempBuffer[step.heaterState->tempBufferIndex] = currentTemp;
    step.heaterState->tempBufferIndex = (step.heaterState->tempBufferIndex + 1) % 10;
    if (!step.heaterState->tempBufferFull && step.heaterState->tempBufferIndex == 0) {
        step.heaterState->tempBufferFull = true;
    }

    float avgTemp = 0.0f;
    int count = step.heaterState->tempBufferFull ? 10 : step.heaterState->tempBufferIndex;
    if (count > 0) {
        for (int i = 0; i < count; i++) {
            avgTemp += step.heaterState->tempBuffer[i];
        }
        avgTemp /= count;
    }

    if (currentTemp >= HEATER_EMERGENCY_TEMP) {
        Serial.printf("HEATER EMERGENCY: %.1f°C - Shutdown\n", currentTemp);
        digitalWrite(PIN_RELAY_HEATER, HIGH);
        if (step.heaterState) {
            step.heaterState.reset();
        }
        step.status = STEP_OK;
        return;
    }

    if (count > 0 && avgTemp >= HEATER_OFF_TEMP) {
        digitalWrite(PIN_RELAY_HEATER, HIGH);
        if (step.heaterState) {
            step.heaterState.reset();
        }
        step.status = STEP_OK;
        return;
    }
}

void JobScheduler::updateHumidifierCycle(Step& step) {
    if (step.humidifierState == nullptr) return;

    const unsigned long FAN_OFF_DURATION = 40000;
    const unsigned long FAN_ON_DURATION = 15000;

    unsigned long now = millis();
    unsigned long elapsed = now - step.humidifierState->lastFanCycleTime;

    if (step.humidifierState->fanOn) {
        if (elapsed >= FAN_ON_DURATION) {
            digitalWrite(PIN_HUMID_POWER, HIGH);
            step.humidifierState->fanOn = false;
            step.humidifierState->lastFanCycleTime = now;
        }
    } else {
        if (elapsed >= FAN_OFF_DURATION) {
            digitalWrite(PIN_HUMID_POWER, LOW);
            step.humidifierState->fanOn = true;
            step.humidifierState->lastFanCycleTime = now;
        }
    }
}
