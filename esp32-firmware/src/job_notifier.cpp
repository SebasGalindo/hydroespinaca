#include "job_notifier.h"
#include "mqtt_handler.h"
#include "config.h"
#include "job_utils.h"

MQTTHandler* JobNotifier::mqttHandler = nullptr;

void JobNotifier::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
    Serial.println("✅ MQTT handler vinculado al JobNotifier");
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

void JobNotifier::publishCompletionsBatch(const std::vector<Job>& jobs) {
    if (jobs.empty()) return;

    // If only one job, use single publish
    if (jobs.size() == 1) {
        publishCompletion(jobs[0]);
        return;
    }

    // Create batch of completions
    std::vector<DynamicJsonDocument> completions;
    completions.reserve(jobs.size());

    for (const auto& job : jobs) {
        StaticJsonDocument<256> doc;
        doc["esp32Id"] = ESP32_ID;
        doc["commandId"] = job.commandId;
        doc["status"] = "completed";
        completions.push_back(doc);
    }

    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletionsBatch(completions);
    } else {
        Serial.printf("📤 Batch de completions (offline): %d items\n", jobs.size());
        for (const auto& job : jobs) {
            Serial.printf("   - %s\n", job.commandId.c_str());
        }
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

