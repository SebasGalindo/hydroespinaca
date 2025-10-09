#include "mqtt_handler.h"
#include "config.h"
#include "actuators.h"
#include <NTPClient.h>
#include <time.h>

// External NTPClient instance from main.cpp
extern NTPClient timeClient;

// Static instance for callback
MQTTHandler* MQTTHandler::instance = nullptr;

MQTTHandler::MQTTHandler() 
    : mqttClient(secureClient) {
    
    instance = this;
    
    // Configure TLS/SSL
    secureClient.setCACert(MQTT_ROOT_CA);
    
    mqttClient.setServer(MQTT_HOST, MQTT_PORT);
    mqttClient.setKeepAlive(60);
    mqttClient.setBufferSize(1024);

    Serial.printf("🔧 MQTT Buffer configurado: %d bytes máximo\n", MQTT_MAX_PACKET_SIZE);
    Serial.printf("🔧 MQTT Buffer disponible: %d bytes\n", mqttClient.getBufferSize());
}


void MQTTHandler::begin() {
    connectWiFi();
    // connectMQTT() will be called from loop() once WiFi is connected
}

bool MQTTHandler::connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) {
        // La lógica de impresión se maneja mejor en loop()
        return true;
    }
    
    // Si la conexión no se ha iniciado, la iniciamos una sola vez.
    if (WiFi.getMode() == WIFI_MODE_NULL || WiFi.status() == WL_DISCONNECTED) {
        Serial.printf("📶 Iniciando conexión a WiFi: %s\n", WIFI_SSID);
        Serial.println("� Credenciales configuradas, iniciando conexión...");
        WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    }
    
    // NOTA: El monitoreo de la conexión y los reintentos se hacen en MQTTHandler::loop()
    return false;
}

bool MQTTHandler::connectMQTT() {
    if (mqttClient.connected()) return true;
    
    // Check NTP sync status (non-blocking)
    if (!timeClient.isTimeSet()) {
        Serial.println("[NET] NTP no sincronizado - intentando actualizar...");
        timeClient.forceUpdate();
        
        // If still not set, skip TLS connection
        if (!timeClient.isTimeSet()) {
            Serial.println("⚠️  [NET] NTP no disponible - esperando sincronización");
            return false;
        }
    }
    
    // Validate that we have a reasonable time (after year 2021)
    unsigned long epochTime = timeClient.getEpochTime();
    if (epochTime < 1609459200) {  // January 1, 2021
        Serial.printf("⚠️  [NET] Hora inválida para TLS: %lu (requiere > 2021)\n", epochTime);
        return false;
    }
    
    Serial.printf("[MQTT] Conectando a MQTT broker TLS: %s:%d\n", MQTT_HOST, MQTT_PORT);
    Serial.printf("[MQTT] Usuario: %s, Cliente: %s\n", MQTT_USER, MQTT_CLIENT_ID);
    
    // Set Last Will Testament (LWT)
    DynamicJsonDocument lwtDoc(256);
    lwtDoc["esp32Id"] = ESP32_ID;
    lwtDoc["status"] = "offline";
    String lwtPayload;
    serializeJson(lwtDoc, lwtPayload);
    
    if (mqttClient.connect(MQTT_CLIENT_ID, MQTT_USER, MQTT_PASSWD, 
                          TOPIC_STATUS, 1, true, lwtPayload.c_str())) {
        Serial.println("✅ [MQTT] MQTT conectado con TLS!");
        Serial.println("🎛️  Modo autónomo: sin suscripciones MQTT activas");
        
        // Publish online status
        publishStatus("online");
        
        // Process any buffered telemetry
        processBufferedTelemetry();
        
        return true;
    } else {
        int mqttState = mqttClient.state();
        Serial.printf("❌ [MQTT] Fallo de conexión, estado: %d\n", mqttState);
        
        // Decode error state for diagnostics
        switch (mqttState) {
            case -4: Serial.println("   📋 MQTT_CONNECTION_TIMEOUT"); break;
            case -3: Serial.println("   📋 MQTT_CONNECTION_LOST"); break;
            case -2: Serial.println("   📋 MQTT_CONNECT_FAILED"); break;
            case -1: Serial.println("   📋 MQTT_DISCONNECTED"); break;
            case 1: Serial.println("   📋 MQTT_CONNECT_BAD_PROTOCOL"); break;
            case 2: Serial.println("   📋 MQTT_CONNECT_BAD_CLIENT_ID"); break;
            case 3: Serial.println("   📋 MQTT_CONNECT_UNAVAILABLE"); break;
            case 4: Serial.println("   📋 MQTT_CONNECT_BAD_CREDENTIALS"); break;
            case 5: Serial.println("   📋 MQTT_CONNECT_UNAUTHORIZED"); break;
            default: Serial.printf("   📋 Estado desconocido (%d)\n", mqttState); break;
        }
        
        return false;
    }
}

void MQTTHandler::loop() {
    unsigned long now = millis();
    
    // 1. WiFi connection management with exponential backoff (non-blocking)
    if (WiFi.status() != WL_CONNECTED) {
        // Check if it's time to retry WiFi connection
        if (now - lastWifiReconnectAttempt >= currentWifiReconnectDelay) {
            lastWifiReconnectAttempt = now;
            wifiReconnectAttempts++;
            
            Serial.printf("❌ WiFi desconectado (Estado: %d). Reintento #%d\n", 
                          WiFi.status(), wifiReconnectAttempts);
            
            // Attempt to reconnect
            connectWiFi(); 
            
            // Exponential backoff: 1s, 2s, 4s, 8s... up to MAX
            currentWifiReconnectDelay *= 2;
            if (currentWifiReconnectDelay > MAX_WIFI_RECONNECT_DELAY) {
                currentWifiReconnectDelay = MAX_WIFI_RECONNECT_DELAY;
            }

            // Critical condition: too many failed attempts
            if (wifiReconnectAttempts > MAX_WIFI_ATTEMPTS) {
                Serial.println("🚨 FALLO PERSISTENTE DE WIFI. Forzando ESP.restart().");
                ActuatorController::emergencyStop();
                delay(1000); 
                ESP.restart(); 
            }
        }
        
        // ⚠️ CRITICAL: Do NOT return here - autonomous control must continue
        // WiFi is optional for autonomous operation
    } else {
        // WiFi connected - reset backoff counters
        if (wifiReconnectAttempts > 0) {
            Serial.printf("✅ WiFi reconectado. IP: %s, RSSI: %d dBm\n", 
                         WiFi.localIP().toString().c_str(), WiFi.RSSI());
            currentWifiReconnectDelay = 1000; 
            wifiReconnectAttempts = 0; 
        }
        
        // 2. MQTT connection management (only when WiFi is available)
        if (!isConnected()) {
            if (now - lastMqttReconnectAttempt > currentMqttReconnectDelay) {
                lastMqttReconnectAttempt = now;
                
                Serial.printf("⚠️ MQTT desconectado. Intentando reconexión...\n");

                if (connectMQTT()) {
                    // Success: reset delay
                    currentMqttReconnectDelay = 2000;
                } else {
                    // Failure: exponential backoff
                    currentMqttReconnectDelay *= 2;
                    if (currentMqttReconnectDelay > MAX_MQTT_RECONNECT_DELAY) {
                        currentMqttReconnectDelay = MAX_MQTT_RECONNECT_DELAY;
                    }
                }
            }
        } else {
            // MQTT connected - process messages
            mqttClient.loop();
        }
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