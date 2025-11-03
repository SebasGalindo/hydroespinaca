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

    // Configure TLS/SSL
    secureClient.setCACert(MQTT_ROOT_CA);

    mqttClient.setServer(MQTT_HOST, MQTT_PORT);
    mqttClient.setCallback(messageCallback);
    mqttClient.setKeepAlive(60);
    mqttClient.setBufferSize(MQTT_MAX_PACKET_SIZE);

    Serial.printf("🔧 MQTT Buffer configurado: %d bytes máximo\n", MQTT_MAX_PACKET_SIZE);
    Serial.printf("🔧 MQTT Buffer disponible: %d bytes\n", mqttClient.getBufferSize());
}


void MQTTHandler::begin() {
    connectWiFi();
    connectMQTT();
}

bool MQTTHandler::connectWiFi() {
    if (WiFi.status() == WL_CONNECTED) {
        return true;
    }

    // Si la conexión no se ha iniciado, la iniciamos una sola vez (no bloqueante)
    if (WiFi.getMode() == WIFI_MODE_NULL || WiFi.status() == WL_DISCONNECTED) {
        Serial.printf("📶 Iniciando conexión a WiFi: %s\n", WIFI_SSID);
        Serial.println("🔑 Credenciales configuradas, iniciando conexión...");
        WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
    }

    // NOTA: El monitoreo de la conexión y los reintentos se hacen en MQTTHandler::loop()
    return false;
}

bool MQTTHandler::connectMQTT() {
    if (mqttClient.connected()) return true;

    // CRITICAL: Only check NTP if it has been initialized first
    if (!ntpInitialized) {
        Serial.println("⚠️  [NET] NTP no inicializado aún - esperando inicialización WiFi");
        return false;
    }

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
        
        // Subscribe to job schedule topic with QoS 1
        bool subscribeResult = mqttClient.subscribe(TOPIC_JOB_SCHEDULE, 1);
        if (subscribeResult) {
            Serial.printf("📥 ✅ Suscrito exitosamente a: %s (QoS 1)\n", TOPIC_JOB_SCHEDULE);
        } else {
            Serial.printf("📥 ❌ Error suscribiéndose a: %s\n", TOPIC_JOB_SCHEDULE);
        }
        
        // Publish online status
        publishStatus("online"); // Use internal timestamp for initial connection

        // Process any buffered events (completions/notifications)
        processBufferedEvents();
        
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
                jobScheduler->emergencyStop();
                delay(1000);
                ESP.restart();
            }
        }

        // ⚠️ CRITICAL: Do NOT return here - job processing must continue
        // Existing jobs can still be processed even without WiFi/MQTT connection
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
        Serial.println("❌ MQTT desconectado - descartando telemetría antigua");
        Serial.println("ℹ️  Razón: Al reconectar, fuzzy-service necesita datos ACTUALES, no históricos");
        Serial.println("═══════════════════════════════════════");
        return false;
    }

    if (payload.length() > MQTT_MAX_PACKET_SIZE) {
        Serial.printf("🚨 ERROR: JSON demasiado grande (%d > %d bytes)\n", payload.length(), MQTT_MAX_PACKET_SIZE);
        Serial.println("❌ Descartando telemetría (demasiado grande)");
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

        Serial.println("❌ Descartando telemetría (publish falló)");
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

    String payload;
    serializeJson(completion, payload);

    Serial.printf("📄 Completion JSON (%d bytes):\n%s\n", payload.length(), payload.c_str());
    Serial.printf("🏷️  Topic: %s\n", TOPIC_COMPLETIONS);

    if (!isConnected()) {
        Serial.println("❌ MQTT desconectado - buffereando completion");
        bufferEvent(payload, "completion");
        return false;
    }

    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false);
    if (success) {
        Serial.printf("✅ Completion enviado exitosamente: %d bytes\n", payload.length());
    } else {
        Serial.println("❌ Error enviando completion - buffereando");
        Serial.printf("📊 Estado MQTT: %d\n", mqttClient.state());
        bufferEvent(payload, "completion");
    }

    return success;
}

bool MQTTHandler::publishCompletionsBatch(const std::vector<DynamicJsonDocument>& completions) {
    if (completions.empty()) return true;

    // If only one completion, use single publish
    if (completions.size() == 1) {
        return publishCompletion(completions[0]);
    }

    Serial.printf("📤 ENVIANDO BATCH DE COMPLETIONS (%d items)\n", completions.size());

    // Create array of completions
    DynamicJsonDocument batchDoc(2048);
    JsonArray completionsArray = batchDoc.createNestedArray("completions");

    for (const auto& completion : completions) {
        JsonObject obj = completionsArray.createNestedObject();
        obj["esp32Id"] = completion["esp32Id"];
        obj["commandId"] = completion["commandId"];
        obj["status"] = completion["status"];
    }

    String payload;
    serializeJson(batchDoc, payload);

    Serial.printf("📄 Batch JSON (%d bytes):\n%s\n", payload.length(), payload.c_str());
    Serial.printf("🏷️  Topic: %s\n", TOPIC_COMPLETIONS);

    if (!isConnected()) {
        Serial.println("❌ MQTT desconectado - buffereando batch");
        bufferEvent(payload, "completion");
        return false;
    }

    bool success = mqttClient.publish(TOPIC_COMPLETIONS, payload.c_str(), false);
    if (success) {
        Serial.printf("✅ Batch enviado exitosamente: %d completions, %d bytes\n",
                     completions.size(), payload.length());
    } else {
        Serial.println("❌ Error enviando batch - buffereando");
        Serial.printf("📊 Estado MQTT: %d\n", mqttClient.state());
        bufferEvent(payload, "completion");
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
        // Remove oldest entry
        eventQueue.pop();
        Serial.printf("📦 Buffer eventos lleno, eliminando %s antiguo\n", type.c_str());
    }

    EventBuffer buffer;
    buffer.payload = payload;
    buffer.timestamp = millis();
    buffer.type = type;
    eventQueue.push(buffer);

    Serial.printf("📦 Evento buffereado [%s] (cola: %d/%d)\n", type.c_str(), eventQueue.size(), MAX_BUFFERED_EVENTS);
}

void MQTTHandler::processBufferedEvents() {
    if (eventQueue.empty()) {
        Serial.println("ℹ️  No hay eventos buffereados para procesar");
        return;
    }

    Serial.printf("📤 Procesando eventos buffereados (%d entradas)\n", eventQueue.size());

    int completionsSent = 0;
    int failures = 0;

    while (!eventQueue.empty() && isConnected()) {
        EventBuffer buffer = eventQueue.front();
        eventQueue.pop();

        const char* topic = TOPIC_COMPLETIONS ;

        bool success = mqttClient.publish(topic, buffer.payload.c_str(), false);
        if (success) {
            if (buffer.type == "completion") {
                completionsSent++;
            }
            Serial.printf("✅ %s buffereado enviado\n", buffer.type.c_str());
        } else {
            Serial.printf("❌ Error enviando %s buffereado\n", buffer.type.c_str());
            failures++;
            // Re-queue the failed event at the end
            eventQueue.push(buffer);
            break; // Stop processing to avoid infinite loop
        }

        delay(100); // Small delay between messages
    }

    Serial.printf("📊 Resumen: Completions=%d, Fallos=%d, Pendientes=%d\n",
                  completionsSent, failures, eventQueue.size());
}