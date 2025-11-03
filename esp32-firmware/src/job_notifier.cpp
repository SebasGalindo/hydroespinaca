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
    // 🔒 VALIDACIÓN CRÍTICA: Verificar que commandId sea válido antes de publicar
    if (job.commandId.isEmpty()) {
        Serial.println("═══════════════════════════════════════");
        Serial.println("🚨 ERROR CRÍTICO: Job finalizado SIN commandId VÁLIDO");
        Serial.println("🚫 NO SE ENVIARÁ COMPLETION para evitar crash del actuator-service");
        Serial.println("═══════════════════════════════════════");
        Serial.println("📊 DEBUG INFO:");
        Serial.printf("   - commandId: \"%s\" (vacío=%s)\n",
                     job.commandId.c_str(),
                     job.commandId.isEmpty() ? "SÍ" : "NO");
        Serial.printf("   - baseId: \"%s\"\n", job.baseId.c_str());
        Serial.printf("   - Steps: %d\n", job.steps.size());
        if (!job.steps.empty()) {
            Serial.printf("   - Pin: %d\n", job.steps[0].pin);
            Serial.printf("   - Power: %s\n", job.steps[0].power == ON ? "ON" : "OFF");
        }
        Serial.println("═══════════════════════════════════════");
        Serial.println("⚠️  ACCIÓN REQUERIDA: Investigar por qué este Job no tiene commandId");
        Serial.println("⚠️  Posible causa: JSON recibido no incluía campo 'commandId'");
        Serial.println("═══════════════════════════════════════");
        return;  // ABORTAR envío
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

    // If only one job, use single publish (with validation)
    if (jobs.size() == 1) {
        publishCompletion(jobs[0]);
        return;
    }

    // Create batch of completions (with validation)
    std::vector<DynamicJsonDocument> completions;
    completions.reserve(jobs.size());

    int invalidCount = 0;

    for (const auto& job : jobs) {
        // 🔒 VALIDACIÓN CRÍTICA: Verificar que commandId sea válido
        if (job.commandId.isEmpty()) {
            Serial.println("🚨 [BATCH] Job sin commandId - OMITIDO del batch");
            Serial.printf("   - baseId: \"%s\", Steps: %d\n", job.baseId.c_str(), job.steps.size());
            invalidCount++;
            continue;  // OMITIR este job del batch
        }

        StaticJsonDocument<256> doc;
        doc["esp32Id"] = ESP32_ID;
        doc["commandId"] = job.commandId;
        doc["status"] = "completed";
        completions.push_back(doc);
    }

    // Log warnings if any invalid jobs were found
    if (invalidCount > 0) {
        Serial.println("═══════════════════════════════════════");
        Serial.printf("⚠️  [BATCH] %d job(s) omitido(s) por commandId inválido\n", invalidCount);
        Serial.println("⚠️  Estos jobs NO se reportaron para evitar crash del service");
        Serial.println("═══════════════════════════════════════");
    }

    // If all jobs were invalid, abort
    if (completions.empty()) {
        Serial.println("🚨 [BATCH] Todos los jobs tenían commandId inválido - batch abortado");
        return;
    }

    if (mqttHandler && mqttHandler->isConnected()) {
        mqttHandler->publishCompletionsBatch(completions);
    } else {
        Serial.printf("📤 Batch de completions (offline): %d items\n", completions.size());
        for (const auto& job : jobs) {
            if (!job.commandId.isEmpty()) {
                Serial.printf("   - %s\n", job.commandId.c_str());
            }
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

