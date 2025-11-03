#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include "job_scheduler.h"

class MQTTHandler; // Forward declaration

class JobNotifier {
public:
    static void setMQTTHandler(MQTTHandler* handler);
    static void publishCompletion(const Job& job);
    static void publishCompletionsBatch(const std::vector<Job>& jobs);
    static bool hasInternetConnectivity();

private:
    static MQTTHandler* mqttHandler;
};