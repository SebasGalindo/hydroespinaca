#include "job_notifier.h"
#include "mqtt_handler.h"
#include "config.h"
#include "job_utils.h"

MQTTHandler* JobNotifier::mqttHandler = nullptr;

void JobNotifier::setMQTTHandler(MQTTHandler* handler) {
    mqttHandler = handler;
}


void JobNotifier::publishCompletion(const Job& job) {
    if (job.commandId.isEmpty()) {
        Serial.println("ERROR: Job without commandId - skipping completion");
        return;
    }

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

    if (jobs.size() == 1) {
        publishCompletion(jobs[0]);
        return;
    }

    std::vector<DynamicJsonDocument> completions;
    completions.reserve(jobs.size());

    for (const auto& job : jobs) {
        if (job.commandId.isEmpty()) {
            continue;
        }

        StaticJsonDocument<256> doc;
        doc["esp32Id"] = ESP32_ID;
        doc["commandId"] = job.commandId;
        doc["status"] = "completed";
        completions.push_back(doc);
    }

    if (completions.empty()) {
        return;
    }

    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletionsBatch(completions);
    }
}

bool JobNotifier::hasInternetConnectivity() {
    if (!mqttHandler) {
        return false;
    }

    if (!mqttHandler->isWiFiConnected()) {
        return false;
    }

    if (!mqttHandler->isConnected()) {
        return false;
    }

    return true;
}

