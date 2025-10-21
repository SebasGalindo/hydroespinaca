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
    StaticJsonDocument<256> doc;

    doc["esp32Id"] = ESP32_ID;
    doc["commandId"] = job.commandId;
    doc["status"] = "completed";

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

