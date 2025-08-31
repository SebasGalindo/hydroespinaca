#include "mqtt_handler.h"
#include "job_scheduler.h"
#include "config.h"

// Static instance for callback
MQTTHandler* MQTTHandler::instance = nullptr;

MQTTHandler::MQTTHandler(JobScheduler* scheduler) 
    : mqttClient(wifiClient), jobScheduler(scheduler), 
      lastReconnectAttempt(0), reconnectInterval(RECONNECT_INTERVAL), reconnectAttempts(0) {
    instance = this;
    mqttClient.setServer(MQTT_SERVER, MQTT_PORT);
    mqttClient.setCallback(messageCallback);
    mqttClient.setKeepAlive(60);
}

void MQTTHandler::begin() {
    connectWiFi();
    connectMQTT();
}

bool MQTTHandler::connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) return true;
    
    Serial.printf("📶 Conectando a WiFi: %s", WIFI_SSID);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
        delay(500);
        Serial.print(".");
        attempts++;
    }
    
    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("\n✅ WiFi conectado! IP: %s\n", WiFi.localIP().toString().c_str());
        return true;
    } else {
        Serial.println("\n❌ Error conectando WiFi");
        return false;
    }
}

bool MQTTHandler::connectMQTT() {
    if (mqttClient.connected()) return true;
    
    Serial.printf("🔗 Conectando a MQTT broker: %s:%d\n", MQTT_SERVER, MQTT_PORT);
    
    // Set Last Will Testament (LWT)
    DynamicJsonDocument lwtDoc(256);
    lwtDoc["esp32Id"] = ESP32_ID;
    lwtDoc["status"] = "offline";
    String lwtPayload;
    serializeJson(lwtDoc, lwtPayload);
    
    if (mqttClient.connect(MQTT_CLIENT_ID, MQTT_USER, MQTT_PASSWORD, 
                          TOPIC_STATUS, 1, true, lwtPayload.c_str())) {
        Serial.println("✅ MQTT conectado!");
        
        // Reset reconnect attempts on successful connection
        reconnectAttempts = 0;
        reconnectInterval = RECONNECT_INTERVAL;
        
        // Subscribe to job schedule topic with QoS 1
        mqttClient.subscribe(TOPIC_JOB_SCHEDULE, 1);
        Serial.printf("📥 Suscrito a: %s (QoS 1)\n", TOPIC_JOB_SCHEDULE);
        
        // Publish online status
        publishStatus("online");
        
        // Process any buffered telemetry
        processBufferedTelemetry();
        
        return true;
    } else {
        Serial.printf("❌ Error MQTT: %d\n", mqttClient.state());
        reconnectAttempts++;
        reconnectInterval = getBackoffInterval();
        return false;
    }
}

void MQTTHandler::loop() {
    // Maintain WiFi connection
    if (!isWiFiConnected()) {
        connectWiFi();
    }
    
    // Maintain MQTT connection with exponential backoff
    if (!isConnected()) {
        unsigned long now = millis();
        if (now - lastReconnectAttempt > reconnectInterval) {
            lastReconnectAttempt = now;
            connectMQTT();
        }
    } else {
        mqttClient.loop();
    }
}

bool MQTTHandler::publishReadings(DynamicJsonDocument& readings) {
    String payload;
    serializeJson(readings, payload);
    
    if (!isConnected()) {
        // Buffer telemetry when offline
        bufferTelemetry(payload);
        return false;
    }
    
    bool success = mqttClient.publish(TOPIC_READINGS, payload.c_str(), false); // QoS 0
    if (success) {
        Serial.printf("📤 Telemetría enviada: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando telemetría");
        bufferTelemetry(payload);
    }
    
    return success;
}

bool MQTTHandler::publishStatus(const String& status) {
    if (!isConnected()) return false;
    
    DynamicJsonDocument doc(512);
    doc["esp32Id"] = ESP32_ID;
    doc["status"] = status;
    doc["timestamp"] = getCurrentTimestamp();
    doc["freeHeap"] = ESP.getFreeHeap();
    doc["uptime"] = millis() / 1000;
    
    String payload;
    serializeJson(doc, payload);
    
    return mqttClient.publish(TOPIC_STATUS, payload.c_str(), true); // Retained
}

bool MQTTHandler::publishCompletion(const DynamicJsonDocument& completion) {
    if (!isConnected()) return false;
    
    String payload;
    serializeJson(completion, payload);
    
    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false); // QoS 1 would be ideal
    if (success) {
        Serial.printf("📤 Completion enviado: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando completion");
    }
    
    return success;
}

bool MQTTHandler::publishNotification(const DynamicJsonDocument& notification) {
    if (!isConnected()) return false;
    
    String payload;
    serializeJson(notification, payload);
    
    bool success = mqttClient.publish(TOPIC_NOTIFICATIONS, payload.c_str(), false); // QoS 1 would be ideal
    if (success) {
        Serial.printf("📤 Notificación enviada: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando notificación");
    }
    
    return success;
}

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
    
    Serial.printf("📥 Mensaje recibido en %s: %d bytes\n", topic, length);
    
    if (String(topic) == TOPIC_JOB_SCHEDULE) {
        handleJobSchedule(message);
    }
}

void MQTTHandler::handleJobSchedule(const String& payload) {
    DynamicJsonDocument doc(4096); // Larger buffer for job schedules
    DeserializationError error = deserializeJson(doc, payload);
    
    if (error) {
        Serial.printf("❌ Error parseando JobSchedule JSON: %s\n", error.c_str());
        return;
    }
    
    // Validate ESP32 ID matches
    if (doc["esp32Id"].as<String>() != ESP32_ID) {
        Serial.println("❌ JobSchedule no es para este ESP32");
        return;
    }
    
    Serial.println("✅ Procesando JobSchedule");
    if (jobScheduler) {
        jobScheduler->processJobSchedule(doc);
    }
}

bool MQTTHandler::isConnected() {
    return mqttClient.connected();
}

bool MQTTHandler::isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}

String MQTTHandler::getCurrentTimestamp() {
    unsigned long currentTime = millis() / 1000;
    char timestamp[32];
    sprintf(timestamp, "2025-08-31T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    return String(timestamp);
}

unsigned long MQTTHandler::getBackoffInterval() {
    // Exponential backoff: 5s, 10s, 20s, 30s (max)
    unsigned long intervals[] = {5000, 10000, 20000, 30000};
    int index = min(reconnectAttempts - 1, 3);
    return intervals[index];
}

void MQTTHandler::bufferTelemetry(const String& payload) {
    if (telemetryQueue.size() >= MAX_BUFFERED_TELEMETRY) {
        // Remove oldest entry
        telemetryQueue.pop();
        Serial.println("📦 Buffer telemetría lleno, eliminando entrada antigua");
    }
    
    TelemetryBuffer buffer;
    buffer.payload = payload;
    buffer.timestamp = millis();
    telemetryQueue.push(buffer);
    
    Serial.printf("📦 Telemetría buffereada (cola: %d)\n", telemetryQueue.size());
}

void MQTTHandler::processBufferedTelemetry() {
    Serial.printf("📤 Enviando telemetría buffereada (%d entradas)\n", telemetryQueue.size());
    
    while (!telemetryQueue.empty() && isConnected()) {
        TelemetryBuffer buffer = telemetryQueue.front();
        telemetryQueue.pop();
        
        bool success = mqttClient.publish(TOPIC_READINGS, buffer.payload.c_str(), false);
        if (success) {
            Serial.printf("📤 Telemetría buffereada enviada\n");
        } else {
            Serial.println("❌ Error enviando telemetría buffereada");
            break; // Stop processing if publish fails
        }
        
        delay(100); // Small delay between messages
    }
}