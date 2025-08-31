#ifndef JOB_SCHEDULER_H
#define JOB_SCHEDULER_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include "config.h"

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

struct Step {
    int pin;
    PinMode mode;
    PowerState power;
    int dutyCycle;  // 0-255 for PWM, null for digital
    unsigned long duration;  // milliseconds
    StepStatus status;
    std::vector<String> executionLog;
    unsigned long startTime;
    
    Step() : pin(0), mode(DIGITAL), power(OFF), dutyCycle(0), duration(0), 
             status(STEP_PENDING), startTime(0) {}
};

struct Job {
    String commandId;
    String baseId;
    std::vector<Step> steps;
    int currentStepIndex;
    unsigned long queueTime;
    
    Job() : currentStepIndex(0), queueTime(0) {}
    
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
    
    ChannelState() : channelId(0), isRunning(false), lastActivity(0) {}
    ChannelState(int id) : channelId(id), isRunning(false), lastActivity(0) {}
    
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
    SemaphoreHandle_t schedulerMutex;
    TaskHandle_t channelTasks[MAX_CHANNELS];
    class MQTTHandler* mqttHandler;
    
    // Forward declarations for tasks
    static void channelTaskWrapper(void* parameter);
    void channelTask(int channelId);
    
    // Internal methods
    bool canConsolidate(const Job& incoming, const Job& current);
    void consolidateJob(Job& current, const Job& incoming, std::vector<String>& logs);
    void cancelJobsOnPin(int pin, const String& reason);
    String getCurrentTimestamp();
    void executeStep(Step& step);
    void publishNotification(const String& decision, int channelId, 
                           const String& affectedCommand, const String& targetCommand,
                           const std::vector<String>& logs);
    void publishCompletion(const Job& job);
    
public:
    JobScheduler();
    ~JobScheduler();
    
    void begin();
    void setMQTTHandler(class MQTTHandler* handler);
    void processJobSchedule(const DynamicJsonDocument& payload);
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