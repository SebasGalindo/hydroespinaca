#include "job_consolidator.h"
#include "pwm_manager.h"
#include "job_utils.h"

bool JobConsolidator::canConsolidate(const Job& incoming, const Job& current) {
    // Basic conditions for consolidation
    if (!(incoming.baseId == current.baseId) || 
        (current.currentStepIndex != 0) || 
        current.isCompleted()) {
        return false;
    }
    
    // Check if both jobs use the same pin in their first step
    if (!incoming.steps.empty() && !current.steps.empty()) {
        int incomingPin = incoming.steps[0].pin;
        int currentPin = current.steps[0].pin;
        return (incomingPin == currentPin);
    }
    
    return false;
}

void JobConsolidator::consolidateJob(Job& current, const Job& incoming, std::vector<String>& logs) {
    for (size_t i = 0; i < current.steps.size() && i < incoming.steps.size(); i++) {
        Step& currentStep = current.steps[i];
        const Step& incomingStep = incoming.steps[i];
        
        bool isCurrentlyExecuting = (i == current.currentStepIndex && currentStep.status == STEP_IN_PROGRESS);
        
        // Duration changes with smart extension logic
        if (currentStep.duration != incomingStep.duration) {
            unsigned long now = millis();
            bool isExtension = (incomingStep.duration > currentStep.duration);
            String logEntry = JobUtils::getCurrentTimestamp() + " - Duración " + 
                            (isExtension ? "extendida" : "reducida") + " de " +
                            String(currentStep.duration) + "ms → " + String(incomingStep.duration) + "ms";
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            
            if (isCurrentlyExecuting) {
                // Calculate new end time without restarting the step
                unsigned long newEndTime = now + incomingStep.duration;
                if (isExtension) {
                    // Extend: endTime = max(currentEndTime, newEndTime)  
                    currentStep.endTime = max(currentStep.endTime, newEndTime);
                    Serial.printf("🔄 Step en ejecución EXTENDIDO - Pin %d, nuevo endTime: %lu\n", 
                                currentStep.pin, currentStep.endTime);
                } else {
                    // Reduce: only if newEndTime > now (avoid immediate cutoff)
                    if (newEndTime > now) {
                        currentStep.endTime = newEndTime;
                        Serial.printf("🔄 Step en ejecución REDUCIDO - Pin %d, nuevo endTime: %lu\n", 
                                    currentStep.pin, currentStep.endTime);
                    } else {
                        String warningLog = JobUtils::getCurrentTimestamp() + " - ADVERTENCIA: Reducción ignorada, causaría corte inmediato";
                        logs.push_back(warningLog);
                        currentStep.executionLog.push_back(warningLog);
                        Serial.printf("⚠️ Pin %d: Reducción ignorada para evitar corte inmediato\n", currentStep.pin);
                    }
                }
            } else {
                // Not currently executing, safe to change duration
                currentStep.duration = incomingStep.duration;
            }
        } else if (isCurrentlyExecuting) {
            // Duration reaffirmation: same value but step is running, log for traceability
            String logEntry = JobUtils::getCurrentTimestamp() + " - Duración reafirmada: " + 
                            String(currentStep.duration) + "ms (sin cambios)";
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            Serial.printf("🔄 Step en ejecución REAFIRMADO - Pin %d, duración mantenida: %lums\n", 
                        currentStep.pin, currentStep.duration);
        }
        
        // DutyCycle changes - apply to hardware immediately if step is running
        if (currentStep.dutyCycle != incomingStep.dutyCycle) {
            String logEntry = JobUtils::getCurrentTimestamp() + " - DutyCycle ajustado de " + 
                            String(currentStep.dutyCycle) + "% → " + String(incomingStep.dutyCycle) + "%";
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            
            currentStep.dutyCycle = incomingStep.dutyCycle;
            
            // Apply PWM change immediately if step is currently executing
            if (isCurrentlyExecuting && currentStep.mode == PWM) {
                int duty8bit = map(currentStep.dutyCycle, 0, 100, 0, 255);
                if (currentStep.power == ON && currentStep.dutyCycle > 0) {
                    PWMManager::writeDuty(currentStep.pin, duty8bit);
                    Serial.printf("🔄 PWM Pin %d: duty actualizado en tiempo real a %d%% (%d/255)\n", 
                                currentStep.pin, currentStep.dutyCycle, duty8bit);
                } else {
                    PWMManager::writeDuty(currentStep.pin, 0);
                    Serial.printf("🔄 PWM Pin %d: duty actualizado en tiempo real a 0%% (OFF)\n", 
                                currentStep.pin);
                }
            }
        } else if (isCurrentlyExecuting && currentStep.mode == PWM) {
            // DutyCycle reaffirmation: same value but step is running, log for traceability
            String logEntry = JobUtils::getCurrentTimestamp() + " - DutyCycle reafirmado: " + 
                            String(currentStep.dutyCycle) + "% (sin cambios)";
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            Serial.printf("🔄 PWM Pin %d: duty reafirmado en tiempo real: %d%%\n", 
                        currentStep.pin, currentStep.dutyCycle);
        }
        
        // Power changes - handle carefully for running steps
        if (currentStep.power != incomingStep.power) {
            String powerOld = (currentStep.power == ON) ? "ON" : "OFF";
            String powerNew = (incomingStep.power == ON) ? "ON" : "OFF";
            String logEntry = JobUtils::getCurrentTimestamp() + " - Power cambiado de " + powerOld + " → " + powerNew;
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            
            currentStep.power = incomingStep.power;
            
            // Apply power change immediately if step is currently executing
            if (isCurrentlyExecuting) {
                if (currentStep.mode == DIGITAL) {
                    if (currentStep.power == ON) {
                        digitalWrite(currentStep.pin, LOW);  // Inverted logic for relay board
                        Serial.printf("🔄 DIGITAL Pin %d: Activado en tiempo real (lógica invertida)\n", currentStep.pin);
                    } else {
                        digitalWrite(currentStep.pin, HIGH);  // Inverted logic for relay board
                        Serial.printf("🔄 DIGITAL Pin %d: Desactivado en tiempo real (lógica invertida)\n", currentStep.pin);
                    }
                } else if (currentStep.mode == PWM) {
                    int duty8bit = map(currentStep.dutyCycle, 0, 100, 0, 255);
                    if (currentStep.power == ON && currentStep.dutyCycle > 0) {
                        PWMManager::writeDuty(currentStep.pin, duty8bit);
                        Serial.printf("🔄 PWM Pin %d: Activado en tiempo real duty=%d%%\n", 
                                    currentStep.pin, currentStep.dutyCycle);
                    } else {
                        PWMManager::writeDuty(currentStep.pin, 0);
                        Serial.printf("🔄 PWM Pin %d: Desactivado en tiempo real\n", currentStep.pin);
                    }
                }
            }
        } else if (isCurrentlyExecuting) {
            // Power reaffirmation: same value but step is running, log for traceability
            String powerCurrent = (currentStep.power == ON) ? "ON" : "OFF";
            String logEntry = JobUtils::getCurrentTimestamp() + " - Power reafirmado: " + 
                            powerCurrent + " (sin cambios)";
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            Serial.printf("🔄 Pin %d: power reafirmado en tiempo real: %s\n", 
                        currentStep.pin, powerCurrent.c_str());
        }
        
        // Mode changes (risky for running steps - log but don't apply to hardware)
        if (currentStep.mode != incomingStep.mode) {
            String modeOld = (currentStep.mode == PWM) ? "PWM" : "DIGITAL";
            String modeNew = (incomingStep.mode == PWM) ? "PWM" : "DIGITAL";
            String logEntry = JobUtils::getCurrentTimestamp() + " - Modo cambiado de " + modeOld + " → " + modeNew;
            logs.push_back(logEntry);
            currentStep.executionLog.push_back(logEntry);
            
            if (isCurrentlyExecuting) {
                String warningLog = JobUtils::getCurrentTimestamp() + " - ADVERTENCIA: Cambio de modo en step en ejecución, aplicará en próximo ciclo";
                logs.push_back(warningLog);
                currentStep.executionLog.push_back(warningLog);
                Serial.printf("⚠️ Pin %d: Cambio de modo %s→%s diferido (step en ejecución)\n", 
                            currentStep.pin, modeOld.c_str(), modeNew.c_str());
            }
            
            currentStep.mode = incomingStep.mode;
        }
    }
}

bool JobConsolidator::isControlRoutine(const Job& job) {
    if (job.steps.empty()) {
        return false;
    }
    
    const Step& firstStep = job.steps[0];
    
    // Control routine conditions:
    // 1. DIGITAL mode with power OFF
    // 2. PWM mode with dutyCycle 0
    return (firstStep.mode == DIGITAL && firstStep.power == OFF) ||
           (firstStep.mode == PWM && firstStep.dutyCycle == 0);
}

