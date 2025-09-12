#pragma once

#include <Arduino.h>
#include <ArduinoJson.h>
#include <vector>
#include "job_scheduler.h"

class MQTTHandler; // Forward declaration

class JobNotifier {
public:
    static void setMQTTHandler(MQTTHandler* handler);
    static void publishNotification(const String& decision, int channelId, 
                                   const String& affectedCommand, const String& targetCommand,
                                   const std::vector<String>& logs);
    static void publishCompletion(const Job& job);
    static bool hasInternetConnectivity();
    
private:
    static MQTTHandler* mqttHandler;
};