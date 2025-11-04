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
        Serial.println("❌ [NOTIFIER] ERROR: Job without commandId - skipping completion");
        Serial.printf("    Job details: baseId=%s, stepCount=%d, currentStep=%d\n",
                     job.baseId.c_str(),
                     job.steps.size(),
                     job.currentStepIndex);
        // Print stack trace to debug where this is being called from
        Serial.println("    This indicates a bug in job creation/processing!");
        return;
    }

    Serial.printf("📢 [NOTIFIER] Preparing completion for commandId: %s\n", job.commandId.c_str());

    StaticJsonDocument<256> doc;

    doc["esp32Id"] = ESP32_ID;
    doc["commandId"] = job.commandId;
    doc["status"] = "completed";

    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletion(doc);
    } else {
        String payload;
        serializeJson(doc, payload);
        Serial.println("📤 [NOTIFIER] Completion (offline): " + payload);
    }
}

void JobNotifier::publishCompletionsBatch(const std::vector<Job>& jobs) {
    if (jobs.empty()) return;

    Serial.printf("📢 [NOTIFIER] Processing batch of %d job(s)\n", jobs.size());

    if (jobs.size() == 1) {
        publishCompletion(jobs[0]);
        return;
    }

    std::vector<DynamicJsonDocument> completions;
    completions.reserve(jobs.size());

    for (const auto& job : jobs) {
        if (job.commandId.isEmpty()) {
            Serial.println("⚠️  [NOTIFIER] Skipping job with empty commandId in batch");
            Serial.printf("    baseId=%s, stepCount=%d\n", job.baseId.c_str(), job.steps.size());
            continue;
        }

        StaticJsonDocument<256> doc;
        doc["esp32Id"] = ESP32_ID;
        doc["commandId"] = job.commandId;
        doc["status"] = "completed";
        completions.push_back(doc);
    }

    if (completions.empty()) {
        Serial.println("⚠️  [NOTIFIER] No valid completions to send (all had empty commandId)");
        return;
    }

    Serial.printf("📢 [NOTIFIER] Sending %d valid completion(s)\n", completions.size());

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

