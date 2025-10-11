#ifndef JOB_SCHEDULER_H
#define JOB_SCHEDULER_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include "config.h"

// ========================================
// SIMPLIFIED JOB SCHEDULER - SEQUENTIAL EXECUTION
// Backend manages concurrency via pin-locking
// Firmware processes jobs sequentially, one at a time
// ========================================

enum StepStatus {
    STEP_PENDING,
    STEP_IN_PROGRESS,
    STEP_OK,
    STEP_CANCELLED,
    STEP_ERROR
};

enum PinMode {
    DIGITAL,
    PWM
};

enum PowerState {
    OFF,
    ON
};

enum JobCompletionType {
    JOB_COMPLETED_NORMAL,
    JOB_COMPLETED_MODIFIED,  // For consolidation cases
    JOB_COMPLETED_CANCELLED,
    JOB_COMPLETED_ERROR
};

struct Step {
    int pin;
    PinMode mode;
    PowerState power;
    int dutyCycle;  // 0-255 for PWM, null for digital
    unsigned long duration;  // milliseconds
    StepStatus status;
    std::vector<String> executionLog;
    unsigned long startTime;
    unsigned long endTime;   // Calculated end time for this step

    Step() : pin(0), mode(DIGITAL), power(OFF), dutyCycle(0), duration(0),
             status(STEP_PENDING), startTime(0), endTime(0) {}
};

struct Job {
    String commandId;
    String baseId;
    std::vector<Step> steps;
    int currentStepIndex;
    unsigned long queueTime;
    JobCompletionType completionType;

    Job() : currentStepIndex(0), queueTime(0), completionType(JOB_COMPLETED_NORMAL) {}

    bool isCompleted() const {
        return currentStepIndex >= steps.size();
    }

    bool hasError() const {
        for (const auto& step : steps) {
            if (step.status == STEP_ERROR) return true;
        }
        return false;
    }

    bool isCancelled() const {
        for (const auto& step : steps) {
            if (step.status == STEP_CANCELLED) return true;
        }
        return false;
    }

    Step* getCurrentStep() {
        if (currentStepIndex >= steps.size()) return nullptr;
        return &steps[currentStepIndex];
    }
};

class JobScheduler {
private:
    // Sequential execution state
    std::vector<Job> jobQueue;       // Queue of jobs waiting to execute
    Job* currentJob;                 // Currently executing job (nullptr if idle)
    int currentJobIndex;             // Index in jobQueue of current job

    // Dependencies
    class MQTTHandler* mqttHandler;
    class SensorManager* sensorManager;

    // Internal methods
    void processNextJob();           // Start next job from queue
    void processCurrentStep();       // Process current step of active job
    void executeStep(Step& step);    // Execute a single step
    void completeCurrentJob();       // Complete and report current job
    void cancelJobsOnPin(int pin, const String& reason);

    // Specialized routines
    void runHumidifierRoutine(unsigned long durationMs);
    void runHeaterRoutine(unsigned long durationMs);

public:
    JobScheduler();
    ~JobScheduler();

    void begin();
    void setMQTTHandler(class MQTTHandler* handler);
    void setSensorManager(class SensorManager* manager);
    void processJobSchedule(const JsonDocument& payload);
    void emergencyStop();
    void loop();  // Called from main loop - processes jobs sequentially

    // Status methods
    bool isBusy();
    int getQueueSize();
    void printStatus();
};

// Global instance declaration
extern JobScheduler jobScheduler;

#endif
