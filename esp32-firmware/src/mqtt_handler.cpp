#include "mqtt_handler.h"
#include "config.h"

// Static instance for callback
MQTTHandler* MQTTHandler::instance = nullptr;

MQTTHandler::MQTTHandler(ActuatorManager* actuators) 
    : mqttClient(wifiClient), actuatorManager(actuators), 
      lastReconnectAttempt(0), shouldReconnect(false) {
    instance = this;
    mqttClient.setServer(MQTT_SERVER, MQTT_PORT);
    mqttClient.setCallback(messageCallback);
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
    
    if (mqttClient.connect(MQTT_CLIENT_ID, MQTT_USER, MQTT_PASSWORD)) {
        Serial.println("✅ MQTT conectado!");
        
        // Suscribirse a topics
        mqttClient.subscribe(TOPIC_COMMANDS);
        Serial.printf("📥 Suscrito a: %s\n", TOPIC_COMMANDS);
        
        // Publicar estado inicial
        publishStatus("online");
        
        return true;
    } else {
        Serial.printf("❌ Error MQTT: %d\n", mqttClient.state());
        return false;
    }
}

void MQTTHandler::loop() {
    // Mantener conexión WiFi
    if (!isWiFiConnected()) {
        connectWiFi();
    }
    
    // Mantener conexión MQTT
    if (!isConnected()) {
        unsigned long now = millis();
        if (now - lastReconnectAttempt > RECONNECT_INTERVAL) {
            lastReconnectAttempt = now;
            connectMQTT();
        }
    } else {
        mqttClient.loop();
    }
}

bool MQTTHandler::publishReadings(DynamicJsonDocument& readings) {
    if (!isConnected()) return false;
    
    String payload;
    serializeJson(readings, payload);
    
    bool success = mqttClient.publish(TOPIC_READINGS, payload.c_str());
    if (success) {
        Serial.printf("📤 Datos enviados: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando datos");
    }
    
    return success;
}

bool MQTTHandler::publishStatus(const String& status) {
    if (!isConnected()) return false;
    
    DynamicJsonDocument doc(512);
    doc["esp32Id"] = ESP32_ID;
    doc["status"] = status;
    
    // ISO 8601 timestamp
    char timestamp[32];
    unsigned long currentTime = millis() / 1000;
    sprintf(timestamp, "2025-07-29T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    doc["timestamp"] = timestamp;
    
    doc["freeHeap"] = ESP.getFreeHeap();
    doc["uptime"] = millis() / 1000;
    
    String payload;
    serializeJson(doc, payload);
    
    return mqttClient.publish(TOPIC_STATUS, payload.c_str());
}

bool MQTTHandler::publishState(DynamicJsonDocument& state) {
    if (!isConnected()) return false;
    
    String payload;
    serializeJson(state, payload);
    
    return mqttClient.publish(TOPIC_STATUS, payload.c_str());
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
    
    Serial.printf("📥 Mensaje recibido en %s: %s\n", topic, message.c_str());
    
    if (String(topic) == TOPIC_COMMANDS) {
        handleCommand(message);
    }
}

void MQTTHandler::handleCommand(const String& payload) {
    DynamicJsonDocument command(1024);
    DeserializationError error = deserializeJson(command, payload);
    
    if (error) {
        Serial.printf("❌ Error parseando JSON: %s\n", error.c_str());
        return;
    }
    
    // Procesar comando
    if (actuatorManager->processCommand(command)) {
        Serial.println("✅ Comando ejecutado");
        
        // Publicar nuevo estado
        DynamicJsonDocument stateDoc(1024);
        actuatorManager->getStateAsJson(stateDoc);
        publishState(stateDoc);
    } else {
        Serial.println("❌ Error ejecutando comando");
    }
}

bool MQTTHandler::isConnected() {
    return mqttClient.connected();
}

bool MQTTHandler::isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}