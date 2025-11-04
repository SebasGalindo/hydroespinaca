#include "mqtt_handler.h"
#include "job_scheduler.h"
#include "config.h"
#include <NTPClient.h>
#include <time.h>

// External NTPClient instance from main.cpp
extern NTPClient timeClient;

// Static instance for callback
MQTTHandler* MQTTHandler::instance = nullptr;

MQTTHandler::MQTTHandler(JobScheduler* scheduler)
    : mqttClient(secureClient), jobScheduler(scheduler) {

    instance = this;
    secureClient.setCACert(MQTT_ROOT_CA);
    mqttClient.setServer(MQTT_HOST, MQTT_PORT);
    mqttClient.setCallback(messageCallback);
    mqttClient.setKeepAlive(60);
    mqttClient.setBufferSize(MQTT_MAX_PACKET_SIZE);
}


void MQTTHandler::begin() {
    connectWiFi();
    connectMQTT();
}

bool MQTTHandler::connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) {
        return true;
    }

    if (WiFi.getMode() == WIFI_MODE_NULL || WiFi.status() == WL_DISCONNECTED) {
        WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    }

    return false;
}

bool MQTTHandler::connectMQTT() {
    if (mqttClient.connected()) return true;

    if (!ntpInitialized) {
        return false;
    }

    if (!timeClient.isTimeSet()) {
        timeClient.forceUpdate();
        if (!timeClient.isTimeSet()) {
            return false;
        }
    }

    unsigned long epochTime = timeClient.getEpochTime();
    if (epochTime < 1609459200) {
        return false;
    }

    DynamicJsonDocument lwtDoc(256);
    lwtDoc["esp32Id"] = ESP32_ID;
    lwtDoc["status"] = "offline";
    String lwtPayload;
    serializeJson(lwtDoc, lwtPayload);

    if (mqttClient.connect(MQTT_CLIENT_ID, MQTT_USER, MQTT_PASSWD,
                          TOPIC_STATUS, 1, true, lwtPayload.c_str())) {
        mqttClient.subscribe(TOPIC_JOB_SCHEDULE, 1);
        publishStatus("online");
        processBufferedEvents();
        Serial.println("MQTT connected");
        return true;
    } else {
        return false;
    }
}

void MQTTHandler::loop() {
    unsigned long now = millis();

    if (WiFi.status() != WL_CONNECTED) {
        if (now - lastWifiReconnectAttempt >= currentWifiReconnectDelay) {
            lastWifiReconnectAttempt = now;
            wifiReconnectAttempts++;
            connectWiFi();

            currentWifiReconnectDelay *= 2;
            if (currentWifiReconnectDelay > MAX_WIFI_RECONNECT_DELAY) {
                currentWifiReconnectDelay = MAX_WIFI_RECONNECT_DELAY;
            }

            if (wifiReconnectAttempts > MAX_WIFI_ATTEMPTS) {
                Serial.println("CRITICAL: WiFi failed - Restarting");
                jobScheduler->emergencyStop();
                delay(1000);
                ESP.restart();
            }
        }
    } else {
        if (wifiReconnectAttempts > 0) {
            Serial.printf("WiFi reconnected - IP: %s\n", WiFi.localIP().toString().c_str());
            currentWifiReconnectDelay = 1000;
            wifiReconnectAttempts = 0;
        }

        if (!isConnected()) {
            if (now - lastMqttReconnectAttempt > currentMqttReconnectDelay) {
                lastMqttReconnectAttempt = now;

                if (connectMQTT()) {
                    currentMqttReconnectDelay = 2000;
                } else {
                    currentMqttReconnectDelay *= 2;
                    if (currentMqttReconnectDelay > MAX_MQTT_RECONNECT_DELAY) {
                        currentMqttReconnectDelay = MAX_MQTT_RECONNECT_DELAY;
                    }
                }
            }
        } else {
            mqttClient.loop();
        }
    }
}

bool MQTTHandler::publishReadings(DynamicJsonDocument& readings) {
    String payload;
    serializeJson(readings, payload);

    if (payload.length() == 0) {
        Serial.println("ERROR: Empty JSON");
        return false;
    }

    if (!isConnected()) {
        Serial.println("📊 [TELEMETRY] Skipping - MQTT disconnected");
        return false;
    }

    if (payload.length() > MQTT_MAX_PACKET_SIZE) {
        Serial.printf("ERROR: Payload too large (%d > %d bytes)\n", payload.length(), MQTT_MAX_PACKET_SIZE);
        return false;
    }

    Serial.printf("📊 [TELEMETRY] Publishing %d bytes to %s\n", payload.length(), TOPIC_READINGS);
    Serial.printf("    Payload: %s\n", payload.c_str());

    bool success = mqttClient.publish(TOPIC_READINGS, payload.c_str(), false);

    if (!success) {
        Serial.printf("❌ [TELEMETRY] Publish failed (state: %d)\n", mqttClient.state());
    } else {
        Serial.println("✅ [TELEMETRY] Published successfully");
    }

    return success;
}

bool MQTTHandler::publishStatus(const String& status, String (*timestampFunction)()) {
    if (!isConnected()) return false;
    
    // Get timestamp from external function or fallback to internal
    String timestamp = (timestampFunction != nullptr) ? timestampFunction() : getCurrentTimestamp();
    
    DynamicJsonDocument doc(512);
    doc["status"] = status;
    doc["timestamp"] = timestamp;
    doc["freeHeap"] = ESP.getFreeHeap();
    doc["uptime"] = millis() / 1000;
    
    String payload;
    serializeJson(doc, payload);
    
    return mqttClient.publish(TOPIC_STATUS, payload.c_str(), true); // Retained
}

bool MQTTHandler::publishCompletion(const DynamicJsonDocument& completion) {
    String payload;
    serializeJson(completion, payload);

    Serial.printf("📤 [COMPLETION] Publishing to %s\n", TOPIC_COMPLETIONS);
    Serial.printf("    Payload: %s\n", payload.c_str());

    if (!isConnected()) {
        Serial.println("⚠️  [COMPLETION] MQTT disconnected - Buffering event");
        bufferEvent(payload, "completion");
        return false;
    }

    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false);
    if (!success) {
        Serial.printf("❌ [COMPLETION] Publish failed (state: %d) - Buffering\n", mqttClient.state());
        bufferEvent(payload, "completion");
    } else {
        Serial.println("✅ [COMPLETION] Published successfully");
    }

    return success;
}

bool MQTTHandler::publishCompletionsBatch(const std::vector<DynamicJsonDocument>& completions) {
    if (completions.empty()) return true;

    if (completions.size() == 1) {
        return publishCompletion(completions[0]);
    }

    Serial.printf("📤 [COMPLETION-BATCH] Publishing %d completions\n", completions.size());

    DynamicJsonDocument batchDoc(2048);
    JsonArray completionsArray = batchDoc.createNestedArray("completions");

    for (const auto& completion : completions) {
        JsonObject obj = completionsArray.createNestedObject();
        obj["esp32Id"] = completion["esp32Id"];
        obj["commandId"] = completion["commandId"];
        obj["status"] = completion["status"];
        Serial.printf("    - commandId: %s, status: %s\n",
                     completion["commandId"].as<const char*>(),
                     completion["status"].as<const char*>());
    }

    String payload;
    serializeJson(batchDoc, payload);

    if (!isConnected()) {
        Serial.println("⚠️  [COMPLETION-BATCH] MQTT disconnected - Buffering");
        bufferEvent(payload, "completion");
        return false;
    }

    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false);
    if (!success) {
        Serial.printf("❌ [COMPLETION-BATCH] Publish failed (state: %d)\n", mqttClient.state());
        bufferEvent(payload, "completion");
    } else {
        Serial.println("✅ [COMPLETION-BATCH] Published successfully");
    }

    return success;
}

// publishNotification() removed - no longer needed with concurrent execution (no consolidation)

void MQTTHandler::messageCallback(char* topic, byte* payload, unsigned int length) {
    if (instance) {
        instance->onMessageReceived(topic, payload, length);
    }
}

void MQTTHandler::onMessageReceived(char* topic, byte* payload, unsigned int length) {
    String message = "";
    for (int i = 0; i < length; i++) {
        message += (char)payload[i];
    }

    Serial.printf("📥 [MQTT-RX] Received %d bytes on topic: %s\n", length, topic);
    Serial.printf("    Payload: %s\n", message.c_str());

    if (String(topic) == TOPIC_JOB_SCHEDULE) {
        handleJobSchedule(message);
    }
}

void MQTTHandler::handleJobSchedule(const String& payload) {
    Serial.println("🔧 [JOB-SCHEDULE] Parsing command payload...");

    DynamicJsonDocument doc(4096);
    DeserializationError error = deserializeJson(doc, payload);

    if (error) {
        Serial.printf("❌ [JOB-SCHEDULE] JSON parse failed: %s\n", error.c_str());
        return;
    }

    if (!doc.containsKey("esp32Id")) {
        Serial.println("❌ [JOB-SCHEDULE] Missing esp32Id");
        return;
    }

    String receivedId = doc["esp32Id"].as<String>();
    if (receivedId != ESP32_ID) {
        Serial.printf("⚠️  [JOB-SCHEDULE] Wrong esp32Id (expected: %s, got: %s)\n", ESP32_ID, receivedId.c_str());
        return;
    }

    Serial.printf("✅ [JOB-SCHEDULE] Valid command for ESP32 ID: %s\n", ESP32_ID);

    if (!jobScheduler) {
        Serial.println("❌ [JOB-SCHEDULE] JobScheduler unavailable");
        return;
    }

    jobScheduler->processJobSchedule(doc);
}

bool MQTTHandler::isConnected() {
    return mqttClient.connected();
}

bool MQTTHandler::isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}

String MQTTHandler::getCurrentTimestamp() {
    // Return fallback timestamp if NTP is not initialized
    if (!ntpInitialized) {
        return "1970-01-01T00:00:00Z";  // Epoch fallback
    }

    if (!timeClient.isTimeSet()) {
        timeClient.forceUpdate();
    }

    unsigned long epochTime = timeClient.getEpochTime();

    // Convert to tm struct for formatting
    time_t rawtime = epochTime;
    struct tm * timeinfo = gmtime(&rawtime);

    char isoBuffer[32];
    strftime(isoBuffer, sizeof(isoBuffer), "%Y-%m-%dT%H:%M:%SZ", timeinfo);

    return String(isoBuffer);
}

void MQTTHandler::bufferEvent(const String& payload, const String& type) {
    if (eventQueue.size() >= MAX_BUFFERED_EVENTS) {
        eventQueue.pop();
    }

    EventBuffer buffer;
    buffer.payload = payload;
    buffer.timestamp = millis();
    buffer.type = type;
    eventQueue.push(buffer);
}

void MQTTHandler::processBufferedEvents() {
    if (eventQueue.empty()) {
        return;
    }

    while (!eventQueue.empty() && isConnected()) {
        EventBuffer buffer = eventQueue.front();
        eventQueue.pop();

        const char* topic = TOPIC_COMPLETIONS;

        bool success = mqttClient.publish(topic, buffer.payload.c_str(), false);
        if (!success) {
            eventQueue.push(buffer);
            break;
        }

        delay(100);
    }
}