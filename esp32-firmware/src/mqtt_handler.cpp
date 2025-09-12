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
    : mqttClient(wifiClient), jobScheduler(scheduler), 
      lastReconnectAttempt(0), reconnectInterval(RECONNECT_INTERVAL), reconnectAttempts(0) {
    
    instance = this;
    mqttClient.setServer(MQTT_SERVER, MQTT_PORT);
    mqttClient.setCallback(messageCallback);
    mqttClient.setKeepAlive(60);

    // 👇 Aquí fuerzas el buffer real de la instancia
    mqttClient.setBufferSize(1024);

    // Logs de verificación
    Serial.printf("🔧 MQTT Buffer configurado (macro): %d bytes máximo\n", MQTT_MAX_PACKET_SIZE);
    Serial.printf("🔧 MQTT Buffer disponible (real): %d bytes\n", mqttClient.getBufferSize());
}


void MQTTHandler::begin() {
    connectWiFi();
    connectMQTT();
}

bool MQTTHandler::connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("📶 WiFi ya conectado - IP: %s\n", WiFi.localIP().toString().c_str());
        return true;
    }
    
    Serial.printf("📶 Conectando a WiFi: %s\n", WIFI_SSID);
    Serial.println("🔑 Credenciales configuradas, iniciando conexión...");
    
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 20) {
        delay(500);
        Serial.print(".");
        attempts++;
    }
    
    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("\n✅ WiFi conectado exitosamente!\n");
        Serial.printf("📍 IP asignada: %s\n", WiFi.localIP().toString().c_str());
        Serial.printf("📡 Intensidad señal: %d dBm\n", WiFi.RSSI());
        Serial.printf("🌐 Gateway: %s\n", WiFi.gatewayIP().toString().c_str());
        return true;
    } else {
        Serial.printf("\n❌ Error conectando WiFi después de %d intentos\n", attempts);
        Serial.printf("📊 Estado WiFi: %d\n", WiFi.status());
        return false;
    }
}

bool MQTTHandler::connectMQTT() {
    if (mqttClient.connected()) return true;
    
    Serial.printf("🔗 Conectando a MQTT broker: %s:%d\n", MQTT_SERVER, MQTT_PORT);
    Serial.printf("🔑 Usuario: %s, Cliente: %s\n", MQTT_USER, MQTT_CLIENT_ID);
    
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
        bool subscribeResult = mqttClient.subscribe(TOPIC_JOB_SCHEDULE, 1);
        if (subscribeResult) {
            Serial.printf("📥 ✅ Suscrito exitosamente a: %s (QoS 1)\n", TOPIC_JOB_SCHEDULE);
        } else {
            Serial.printf("📥 ❌ Error suscribiéndose a: %s\n", TOPIC_JOB_SCHEDULE);
        }
        
        // Publish online status
        publishStatus("online"); // Use internal timestamp for initial connection
        
        // Process any buffered telemetry
        processBufferedTelemetry();
        
        return true;
    } else {
        int mqttState = mqttClient.state();
        Serial.printf("❌ Error MQTT - Estado: %d\n", mqttState);
        
        // Decodificar estado del error para diagnóstico
        switch (mqttState) {
            case -4: Serial.println("   📋 Detalles: MQTT_CONNECTION_TIMEOUT"); break;
            case -3: Serial.println("   📋 Detalles: MQTT_CONNECTION_LOST"); break;
            case -2: Serial.println("   📋 Detalles: MQTT_CONNECT_FAILED"); break;
            case -1: Serial.println("   📋 Detalles: MQTT_DISCONNECTED"); break;
            case 1: Serial.println("   📋 Detalles: MQTT_CONNECT_BAD_PROTOCOL"); break;
            case 2: Serial.println("   📋 Detalles: MQTT_CONNECT_BAD_CLIENT_ID"); break;
            case 3: Serial.println("   📋 Detalles: MQTT_CONNECT_UNAVAILABLE"); break;
            case 4: Serial.println("   📋 Detalles: MQTT_CONNECT_BAD_CREDENTIALS"); break;
            case 5: Serial.println("   📋 Detalles: MQTT_CONNECT_UNAUTHORIZED"); break;
            default: Serial.printf("   📋 Detalles: Estado desconocido (%d)\n", mqttState); break;
        }
        
        reconnectAttempts++;
        reconnectInterval = getBackoffInterval();
        Serial.printf("⏰ Reintento #%d programado en %lu ms\n", reconnectAttempts, reconnectInterval);
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
    Serial.println("═══════════════════════════════════════");
    Serial.println("📤 ENVIANDO TELEMETRÍA DE SENSORES");
    
    // Memory check before processing
    uint32_t freeHeapBefore = ESP.getFreeHeap();
    Serial.printf("💾 Memoria libre antes: %u bytes\n", freeHeapBefore);
    
    String payload;
    serializeJson(readings, payload);
    
    // Verify JSON integrity
    if (payload.length() == 0) {
        Serial.println("❌ ERROR CRÍTICO: JSON vacío generado");
        Serial.println("═══════════════════════════════════════");
        return false;
    }
    
    Serial.printf("📄 JSON generado (%d bytes):\n", payload.length());
    Serial.println("════ PAYLOAD COMPLETO ════");
    Serial.println(payload);
    Serial.println("═══════════════════════════");
    
    Serial.printf("🏷️  Topic destino: '%s' (longitud: %d)\n", TOPIC_READINGS, strlen(TOPIC_READINGS));
    Serial.printf("🔗 Estado MQTT: %s\n", isConnected() ? "Conectado" : "Desconectado");
    Serial.printf("📊 Buffer MQTT máximo: %d bytes\n", MQTT_MAX_PACKET_SIZE);
    
    if (!isConnected()) {
        Serial.println("❌ MQTT desconectado - buffereando telemetría");
        bufferTelemetry(payload);
        Serial.println("═══════════════════════════════════════");
        return false;
    }
    
    if (payload.length() > MQTT_MAX_PACKET_SIZE) {
        Serial.printf("🚨 ERROR: JSON demasiado grande (%d > %d bytes)\n", payload.length(), MQTT_MAX_PACKET_SIZE);
        Serial.println("📦 Buffereando para reintento con compresión");
        bufferTelemetry(payload);
        Serial.println("═══════════════════════════════════════");
        return false;
    }
    
    Serial.println("🚀 Intentando publicar...");
    bool success = mqttClient.publish(TOPIC_READINGS, payload.c_str(), false); // QoS 0
    
    // Memory check after processing  
    uint32_t freeHeapAfter = ESP.getFreeHeap();
    Serial.printf("💾 Memoria libre después: %u bytes (diferencia: %d)\n", freeHeapAfter, (int)(freeHeapBefore - freeHeapAfter));
    
    if (success) {
        Serial.printf("✅ Telemetría enviada exitosamente: %d bytes\n", payload.length());
        Serial.printf("📊 Estado del cliente MQTT después del envío: %d\n", mqttClient.state());
    } else {
        Serial.println("❌ ERROR ENVIANDO TELEMETRÍA");
        int mqttState = mqttClient.state();
        Serial.printf("📊 Estado del cliente MQTT: %d\n", mqttState);
        
        // Decode MQTT error state
        switch (mqttState) {
            case -4: Serial.println("   🕒 Error: MQTT_CONNECTION_TIMEOUT (-4)"); break;
            case -3: Serial.println("   💔 Error: MQTT_CONNECTION_LOST (-3)"); break;
            case -2: Serial.println("   🔴 Error: MQTT_CONNECT_FAILED (-2)"); break;
            case -1: Serial.println("   ⚠️  Error: MQTT_DISCONNECTED (-1)"); break;
            case 0: Serial.println("   ✅ Estado: MQTT_CONNECTED (0) - pero publish falló"); break;
            case 1: Serial.println("   🔧 Error: MQTT_CONNECT_BAD_PROTOCOL (1)"); break;
            case 2: Serial.println("   🆔 Error: MQTT_CONNECT_BAD_CLIENT_ID (2)"); break;
            case 3: Serial.println("   🚫 Error: MQTT_CONNECT_UNAVAILABLE (3)"); break;
            case 4: Serial.println("   🔑 Error: MQTT_CONNECT_BAD_CREDENTIALS (4)"); break;
            case 5: Serial.println("   🚪 Error: MQTT_CONNECT_UNAUTHORIZED (5)"); break;
            default: Serial.printf("   ❓ Error desconocido: %d\n", mqttState); break;
        }
        
        Serial.printf("📊 Buffer MQTT disponible: %d bytes\n", mqttClient.getBufferSize());
        Serial.printf("📊 Longitud payload: %d bytes\n", payload.length());
        Serial.printf("📊 Longitud topic: %d bytes\n", strlen(TOPIC_READINGS));
        
        Serial.println("📦 Buffereando telemetría para reintento");
        bufferTelemetry(payload);
    }
    
    Serial.println("═══════════════════════════════════════");
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
    Serial.println("📤 ENVIANDO COMPLETION");
    
    if (!isConnected()) {
        Serial.println("❌ MQTT desconectado - no se puede enviar completion");
        return false;
    }
    
    String payload;
    serializeJson(completion, payload);
    
    Serial.printf("📄 Completion JSON (%d bytes):\n%s\n", payload.length(), payload.c_str());
    Serial.printf("🏷️  Topic: %s\n", TOPIC_COMPLETIONS);
    
    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false); // QoS 1 would be ideal
    if (success) {
        Serial.printf("✅ Completion enviado exitosamente: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando completion");
        Serial.printf("📊 Estado MQTT: %d\n", mqttClient.state());
    }
    
    return success;
}

bool MQTTHandler::publishNotification(const DynamicJsonDocument& notification) {
    Serial.println("📤 ENVIANDO NOTIFICACIÓN");
    
    if (!isConnected()) {
        Serial.println("❌ MQTT desconectado - no se puede enviar notificación");
        return false;
    }
    
    String payload;
    serializeJson(notification, payload);
    
    Serial.printf("📄 Notificación JSON (%d bytes):\n%s\n", payload.length(), payload.c_str());
    Serial.printf("🏷️  Topic: %s\n", TOPIC_NOTIFICATIONS);
    
    bool success = mqttClient.publish(TOPIC_NOTIFICATIONS, payload.c_str(), false); // QoS 1 would be ideal
    if (success) {
        Serial.printf("✅ Notificación enviada exitosamente: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando notificación");
        Serial.printf("📊 Estado MQTT: %d\n", mqttClient.state());
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
    
    Serial.println("═══════════════════════════════════════");
    Serial.printf("📥 MENSAJE RECIBIDO:\n");
    Serial.printf("🏷️  Topic: %s\n", topic);
    Serial.printf("📏 Tamaño: %d bytes\n", length);
    Serial.printf("📄 Payload completo:\n%s\n", message.c_str());
    Serial.println("═══════════════════════════════════════");
    
    if (String(topic) == TOPIC_JOB_SCHEDULE) {
        Serial.println("✅ Topic coincide con TOPIC_JOB_SCHEDULE - procesando...");
        handleJobSchedule(message);
    } else {
        Serial.printf("❌ Topic desconocido. Esperado: %s, Recibido: %s\n", TOPIC_JOB_SCHEDULE, topic);
    }
}

void MQTTHandler::handleJobSchedule(const String& payload) {
    Serial.println("🔄 Iniciando procesamiento de JobSchedule...");
    
    DynamicJsonDocument doc(4096); // Larger buffer for job schedules
    DeserializationError error = deserializeJson(doc, payload);
    
    if (error) {
        Serial.printf("❌ Error parseando JobSchedule JSON: %s\n", error.c_str());
        Serial.printf("📄 Payload problemático (primeros 200 chars): %.200s...\n", payload.c_str());
        return;
    }
    
    Serial.println("✅ JSON parseado correctamente");
    
    // Check if esp32Id exists
    if (!doc.containsKey("esp32Id")) {
        Serial.println("❌ Campo 'esp32Id' no encontrado en el payload");
        return;
    }
    
    String receivedId = doc["esp32Id"].as<String>();
    Serial.printf("🆔 ID recibido: '%s', ID esperado: '%s'\n", receivedId.c_str(), ESP32_ID);
    
    // Validate ESP32 ID matches
    if (receivedId != ESP32_ID) {
        Serial.printf("❌ JobSchedule no es para este ESP32 (recibido: %s, esperado: %s)\n", 
                     receivedId.c_str(), ESP32_ID);
        return;
    }
    
    Serial.println("✅ Validación de ESP32 ID exitosa");
    
    // Check if jobScheduler is available
    if (!jobScheduler) {
        Serial.println("❌ JobScheduler no está disponible");
        return;
    }
    
    Serial.println("✅ Enviando a JobScheduler para procesamiento...");
    jobScheduler->processJobSchedule(doc);
    Serial.println("🎉 JobSchedule procesado - revisa logs detallados arriba");
}

bool MQTTHandler::isConnected() {
    return mqttClient.connected();
}

bool MQTTHandler::isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}

String MQTTHandler::getCurrentTimestamp() {
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