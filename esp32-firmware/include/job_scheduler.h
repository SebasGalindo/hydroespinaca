#ifndef JOB_SCHEDULER_H
#define JOB_SCHEDULER_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include <memory>  // Para std::unique_ptr
#include "config.h"

// ========================================
// SIMPLIFIED JOB SCHEDULER - CONCURRENT EXECUTION
// Backend manages pin-locking for coordination
// Firmware executes multiple jobs simultaneously with non-blocking timers
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
    JOB_COMPLETED_CANCELLED,
    JOB_COMPLETED_ERROR
};

// Heater monitoring state (for non-blocking heater routine)
struct HeaterMonitorState {
    float tempBuffer[10];
    int tempBufferIndex;
    bool tempBufferFull;
    unsigned long lastMonitorTime;

    HeaterMonitorState() : tempBufferIndex(0), tempBufferFull(false), lastMonitorTime(0) {
        for (int i = 0; i < 10; i++) tempBuffer[i] = 0.0f;
    }
};

// Humidifier cycle state (for non-blocking humidifier routine)
struct HumidifierCycleState {
    unsigned long lastFanCycleTime;
    bool fanOn;

    HumidifierCycleState() : lastFanCycleTime(0), fanOn(false) {}
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

    // Specialized routine states (only used for specific pins)
    // 🔒 SEGURIDAD: unique_ptr previene double-delete automáticamente
    std::unique_ptr<HeaterMonitorState> heaterState;     // Only for PIN_RELAY_HEATER
    std::unique_ptr<HumidifierCycleState> humidifierState; // Only for PIN_HUMID_POWER

    Step() : pin(0), mode(DIGITAL), power(OFF), dutyCycle(0), duration(0),
             status(STEP_PENDING), startTime(0), endTime(0),
             heaterState(nullptr), humidifierState(nullptr) {}

    // 🔒 SEGURIDAD: Destructor automático (unique_ptr se encarga de delete)
    ~Step() = default;

    // 🔒 SEGURIDAD: Evitar copia accidental (Rule of Five)
    Step(const Step&) = delete;
    Step& operator=(const Step&) = delete;

    // 🔒 SEGURIDAD: Permitir move (transferencia de propiedad)
    Step(Step&&) = default;
    Step& operator=(Step&&) = default;
};

struct Job {
    String commandId;
    String baseId;
    std::vector<Step> steps;
    int currentStepIndex;
    unsigned long queueTime;
    JobCompletionType completionType;

    Job() : currentStepIndex(0), queueTime(0), completionType(JOB_COMPLETED_NORMAL) {}

    // 🔒 SEGURIDAD: Job es move-only (contiene Steps move-only)
    Job(const Job&) = delete;
    Job& operator=(const Job&) = delete;
    Job(Job&&) = default;
    Job& operator=(Job&&) = default;

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
    // Concurrent execution state
    std::vector<Job> activeJobs;     // Jobs currently executing (multiple simultaneous)

    // Dependencies
    class MQTTHandler* mqttHandler;
    class SensorManager* sensorManager;

    // Internal methods
    void processJob(Job& job);           // Process a single job (called for each active job)
    void processStep(Step& step);        // Process a single step
    void startStep(Step& step);          // Initialize and start a step
    void completeJob(Job& job);          // Complete and report a job
    void reportCompletionsBatch(const std::vector<Job>& jobs);  // Report multiple completions

    // Job consolidation methods
    int findActiveJobIndexByCommandId(const String& commandId);  // Find active job by commandId
    bool extendOrUpdateJob(Job& existingJob, Job& newJob);       // Extend/update existing job

    // Preemption/cancellation methods
    bool isOffCommand(const Step& step);  // Check if step is an OFF command
    void cancelJobsOnPin(int pin);        // Cancel all active jobs using a specific pin
    void executeOffCommandImmediately(const Step& step);  // Execute OFF command instantly

    // Specialized non-blocking routines
    void updateHeaterMonitoring(Step& step);       // Non-blocking heater temperature monitoring
    void updateHumidifierCycle(Step& step);        // Non-blocking humidifier fan cycle

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
