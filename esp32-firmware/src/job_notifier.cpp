#include "job_notifier.h"
#include "mqtt_handler.h"
#include "config.h"
#include "job_utils.h"

MQTTHandler* JobNotifier::mqttHandler = nullptr;

void JobNotifier::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
    Serial.println("✅ MQTT handler vinculado al JobNotifier");
}

void JobNotifier::publishNotification(const String& decision, int channelId, 
                                     const String& affectedCommand, const String& targetCommand,
                                     const std::vector<String>& logs) {
    StaticJsonDocument<1024> doc;
    
    doc["esp32Id"] = ESP32_ID;
    doc["timestamp"] = JobUtils::getCurrentTimestamp();
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
    
    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishNotification(doc);
    } else {
        String payload;
        serializeJson(doc, payload);
        Serial.println("📤 Notification (offline): " + payload);
    }
}

void JobNotifier::publishCompletion(const Job& job) {
    StaticJsonDocument<1536> doc;
    
    doc["esp32Id"] = ESP32_ID;
    doc["commandId"] = job.commandId;
    
    String completionTypeStr = "completed";
    if (job.completionType == JOB_COMPLETED_MODIFIED) completionTypeStr = "modified";
    else if (job.completionType == JOB_COMPLETED_CANCELLED) completionTypeStr = "cancelled";
    else if (job.completionType == JOB_COMPLETED_ERROR) completionTypeStr = "error";
    doc["completionType"] = completionTypeStr;
    
    doc["connectivityStatus"] = hasInternetConnectivity() ? "connected" : "disconnected";
    
    JsonArray stepsArray = doc.createNestedArray("steps");
    
    for (const auto& step : job.steps) {
        JsonObject stepObj = stepsArray.createNestedObject();
        stepObj["pin"] = step.pin;
        
        String statusStr = "ok";
        if (step.status == STEP_CANCELLED) statusStr = "cancelled";
        else if (step.status == STEP_ERROR) statusStr = "error";
        else if (step.status == STEP_IN_PROGRESS) statusStr = "running";
        else if (step.status == STEP_PENDING) statusStr = "pending";
        
        // For modified jobs, if step is still active, show as "running" not "cancelled"
        if (job.completionType == JOB_COMPLETED_MODIFIED && step.status == STEP_IN_PROGRESS) {
            statusStr = "running";
        }
        
        stepObj["status"] = statusStr;
        
        JsonArray logArray = stepObj.createNestedArray("executionLog");
        for (const auto& logEntry : step.executionLog) {
            logArray.add(logEntry);
        }
    }
    
    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletion(doc);
    } else {
        String payload;
        serializeJson(doc, payload);
        Serial.println("📤 Completion (offline): " + payload);
    }
}

bool JobNotifier::hasInternetConnectivity() {
    if (!mqttHandler) {
        Serial.println("⚠️ CONECTIVIDAD: MQTT handler no disponible");
        return false;
    }
    
    if (!mqttHandler->isWiFiConnected()) {
        Serial.println("❌ CONECTIVIDAD: WiFi desconectado");
        return false;
    }
    
    if (!mqttHandler->isConnected()) {
        Serial.println("❌ CONECTIVIDAD: MQTT desconectado");
        return false;
    }
    
    return true;
}

