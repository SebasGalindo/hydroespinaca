#include "mqtt_handler.h"
#include "config.h"
#include <NTPClient.h>
#include <time.h>

// External NTPClient instance from main.cpp
extern NTPClient timeClient;

// Static instance for callback
MQTTHandler* MQTTHandler::instance = nullptr;

MQTTHandler::MQTTHandler() 
    : mqttClient(secureClient), 
      lastReconnectAttempt(0), reconnectInterval(RECONNECT_INTERVAL), reconnectAttempts(0) {
    
    instance = this;
    
    // Configure TLS/SSL
    secureClient.setCACert(MQTT_ROOT_CA);
    
    mqttClient.setServer(MQTT_HOST, MQTT_PORT);
    // No callback needed for autonomous mode - only publishing telemetry
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
    
    // Ensure NTP is synchronized before attempting TLS connection
    if (!timeClient.isTimeSet()) {
        Serial.println("[NET] Sincronizando NTP antes de conectar TLS...");
        timeClient.forceUpdate();
        
        // Wait up to 10 seconds for NTP sync
        int ntpRetries = 0;
        while (!timeClient.isTimeSet() && ntpRetries < 10) {
            delay(1000);
            timeClient.update();
            ntpRetries++;
            Serial.printf("[NET] Intento NTP %d/10...\n", ntpRetries);
        }
        
        if (!timeClient.isTimeSet()) {
            Serial.println("❌ [NET] Error: NTP no sincronizado - TLS requiere hora correcta");
            Serial.println("❌ [NET] Conexión TLS cancelada");
            return false;
        }
        
        Serial.printf("✅ [NET] NTP sincronizado: %s\n", timeClient.getFormattedTime().c_str());
    }
    
    // Validate that we have a reasonable time (after year 2021)
    unsigned long epochTime = timeClient.getEpochTime();
    if (epochTime < 1609459200) {  // January 1, 2021
        Serial.printf("❌ [NET] Hora inválida para TLS: %lu (requiere > 2021)\n", epochTime);
        Serial.println("❌ [NET] Conexión TLS cancelada");
        return false;
    }
    
    Serial.printf("✅ [NET] Hora válida para TLS: %lu\n", epochTime);
    
    Serial.printf("[MQTT] Conectando a MQTT broker TLS: %s:%d\n", MQTT_HOST, MQTT_PORT);
    Serial.printf("[MQTT] Usuario: %s, Cliente: %s\n", MQTT_USER, MQTT_CLIENT_ID);
    Serial.println("[MQTT] Verificando certificado TLS...");
    
    // Set Last Will Testament (LWT)
    DynamicJsonDocument lwtDoc(256);
    lwtDoc["esp32Id"] = ESP32_ID;
    lwtDoc["status"] = "offline";
    String lwtPayload;
    serializeJson(lwtDoc, lwtPayload);
    
    if (mqttClient.connect(MQTT_CLIENT_ID, MQTT_USER, MQTT_PASSWD, 
                          TOPIC_STATUS, 1, true, lwtPayload.c_str())) {
        Serial.println("✅ [MQTT] MQTT conectado con TLS!");
        
        // Reset reconnect attempts on successful connection
        reconnectAttempts = 0;
        reconnectInterval = RECONNECT_INTERVAL;
        
        // Subscribe to job schedule topic with QoS 1
        // Autonomous mode: no subscriptions needed, only publish telemetry
        Serial.println("🎛️  Modo autónomo: sin suscripciones MQTT activas");
        
        // Publish online status
        publishStatus("online"); // Use internal timestamp for initial connection
        
        // Process any buffered telemetry
        processBufferedTelemetry();
        
        return true;
    } else {
        int mqttState = mqttClient.state();
        Serial.printf("❌ [MQTT] connect state = %d\n", mqttState);
        
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

// publishCompletion and publishNotification removed - autonomous mode only publishes telemetry

// MQTT callback functions removed - autonomous mode only publishes telemetry

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