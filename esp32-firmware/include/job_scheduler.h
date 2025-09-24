#ifndef JOB_SCHEDULER_H
#define JOB_SCHEDULER_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include "config.h"

// ========================================
// GENERIC PIN CONTROLLER
// Backend specifies: pin, mode (digital/PWM), value, duration
// No hardcoded actuator logic - fully dynamic
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
};

struct ChannelState {
    int channelId;
    std::vector<Job> jobQueue;
    bool isRunning;
    unsigned long lastActivity;
    SemaphoreHandle_t channelMutex;  // Individual mutex per channel
    
    ChannelState() : channelId(0), isRunning(false), lastActivity(0), channelMutex(nullptr) {}
    ChannelState(int id) : channelId(id), isRunning(false), lastActivity(0), channelMutex(nullptr) {}
    
    Job* getCurrentJob() {
        if (jobQueue.empty()) return nullptr;
        return &jobQueue[0];
    }
    
    void removeCompletedJob() {
        if (!jobQueue.empty() && jobQueue[0].isCompleted()) {
            jobQueue.erase(jobQueue.begin());
        }
    }
    
    bool hasActiveJob() const {
        return !jobQueue.empty() && !jobQueue[0].isCompleted();
    }
};

class JobScheduler {
private:
    ChannelState channels[MAX_CHANNELS];
    SemaphoreHandle_t globalMutex;  // For cross-channel operations only
    TaskHandle_t channelTasks[MAX_CHANNELS];
    class MQTTHandler* mqttHandler;
    
    // Forward declarations for tasks
    static void channelTaskWrapper(void* parameter);
    void channelTask(int channelId);
    
    // Internal methods
    void cancelJobsOnPin(int pin, const String& reason);
    void executeStep(Step& step);
    void runHumidifierRoutine(unsigned long durationMs);  // Specialized humidifier control
    
public:
    JobScheduler();
    ~JobScheduler();
    
    void begin();
    void setMQTTHandler(class MQTTHandler* handler);
    void processJobSchedule(const JsonDocument& payload);
    void emergencyStop();
    void loop();  // Called from main loop for maintenance
    
    // Status methods
    bool isChannelBusy(int channelId);
    int getQueueSize(int channelId);
    void printStatus();
};

// Global instance declaration
extern JobScheduler jobScheduler;

#endif