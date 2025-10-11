#include <Arduino.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include "config.h"
#include "sensors.h"
#include "mqtt_handler.h"
#include "job_scheduler.h"

// Global objects
SensorManager sensors;
MQTTHandler mqttHandler(&jobScheduler);

// NTP Client
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0, 60000); // UTC+0 (estándar), update every 60s

// Timing variables
unsigned long lastReadingTime = 0;
unsigned long lastStatusTime = 0;
unsigned long lastWatchdogTime = 0;

// JSON documents with larger buffer for telemetry
DynamicJsonDocument readingsDoc(4096); // Increased for NTP timestamps and physicalIds

String getCurrentISOTimestamp() {
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

void validateReadingsJson(DynamicJsonDocument& doc) {
    Serial.println("🔍 VALIDANDO JSON DE LECTURAS");
    
    // Check required fields
    if (!doc.containsKey("esp32Id")) {
        Serial.println("❌ Campo 'esp32Id' faltante");
    } else {
        Serial.printf("✅ esp32Id: %s\n", doc["esp32Id"].as<String>().c_str());
    }
    
    if (!doc.containsKey("timestamp")) {
        Serial.println("❌ Campo 'timestamp' faltante");
    } else {
        Serial.printf("✅ timestamp: %s\n", doc["timestamp"].as<String>().c_str());
    }
    
    if (!doc.containsKey("readings")) {
        Serial.println("❌ Campo 'readings' faltante");
    } else {
        JsonArray readings = doc["readings"];
        Serial.printf("✅ readings array con %d elementos\n", readings.size());
        
        for (int i = 0; i < readings.size(); i++) {
            JsonObject reading = readings[i];
            Serial.printf("   [%d] physicalId: %s, variableId: %s, value: %.2f\n", 
                         i, 
                         reading["physicalId"].as<String>().c_str(),
                         reading["variableId"].as<String>().c_str(),
                         reading["value"].as<float>());
        }
    }
    
    Serial.printf("📏 Tamaño total del documento: %d bytes\n", doc.memoryUsage());
}

void printSystemInfo() {
    Serial.println("════════════════════════════════════════════════");
    Serial.println("🌱 HydroEspinaca ESP32 v2.0 - INFORMACIÓN DEL SISTEMA");
    Serial.println("════════════════════════════════════════════════");
    Serial.printf("📋 Firmware: %s\n", FIRMWARE_VERSION);
    Serial.printf("🆔 ESP32 ID: %s\n", ESP32_ID);
    Serial.printf("🏷️  MQTT Client ID: %s\n", MQTT_CLIENT_ID);
    Serial.printf("💾 Memoria libre: %u bytes\n", ESP.getFreeHeap());
    Serial.printf("📏 Tamaño de memoria: %u bytes\n", ESP.getHeapSize());
    Serial.printf("⚡ Frecuencia CPU: %u MHz\n", ESP.getCpuFreqMHz());
    Serial.printf("🔧 SDK Version: %s\n", ESP.getSdkVersion());
    
    Serial.println("\n📡 CONFIGURACIÓN MQTT:");
    Serial.printf("🏷️  Topic Readings: %s\n", TOPIC_READINGS);
    Serial.printf("🏷️  Topic Job Schedule: %s\n", TOPIC_JOB_SCHEDULE);  
    Serial.printf("🏷️  Topic Status: %s\n", TOPIC_STATUS);
    Serial.printf("🏷️  Topic Completions: %s\n", TOPIC_COMPLETIONS);
    Serial.printf("🏷️  Topic Notifications: %s\n", TOPIC_NOTIFICATIONS);
    
    Serial.println("════════════════════════════════════════════════");
}

void setup() {
    Serial.begin(115200);
    delay(2000); // Dar tiempo para que se abra el monitor serie
    
    printSystemInfo();
    
    // Initialize components in proper order (NTP first for TLS)
    Serial.println("🔧 Iniciando sincronización NTP...");
    timeClient.begin();
    timeClient.forceUpdate();
    
    // Validate NTP is working before proceeding with TLS
    if (timeClient.isTimeSet()) {
        Serial.printf("📅 [NET] NTP sincronizado: %s\n", timeClient.getFormattedTime().c_str());
    } else {
        Serial.println("⚠️ [NET] NTP no sincronizado - TLS puede fallar");
    }
    
    Serial.println("🔧 Inicializando sensores...");
    sensors.begin();
    
    Serial.println("🔧 Inicializando JobScheduler (controlador genérico de pines)...");
    jobScheduler.begin();
    
    Serial.println("🔧 Inicializando MQTT con TLS...");
    mqttHandler.begin();
    
    Serial.println("🔧 Vinculando JobScheduler con MQTT...");
    jobScheduler.setMQTTHandler(&mqttHandler);

    Serial.println("🔧 Vinculando JobScheduler con SensorManager...");
    jobScheduler.setSensorManager(&sensors);

    Serial.println("🚀 Sistema iniciado correctamente!\n");
    Serial.println("📊 Publicando telemetría cada 2 minutos");
    Serial.println("📡 Escuchando job schedules en: " + String(TOPIC_JOB_SCHEDULE));
    Serial.println("🎛️  Actuadores controlados dinámicamente por backend (pin, tipo, valor, duración)");
    Serial.println("💨 Humidificador controlado por actuator-service mediante rutina especializada");
}

void loop() {
    unsigned long currentTime = millis();
    
    // Update NTP time
    timeClient.update();
    
    // Maintain MQTT connections
    mqttHandler.loop();
    
    // JobScheduler maintenance
    jobScheduler.loop();
    
    // Send telemetry every READING_INTERVAL (120 seconds)
    if (currentTime - lastReadingTime >= READING_INTERVAL) {
        Serial.println("⏰ CICLO DE TELEMETRÍA INICIADO");
        Serial.printf("🕒 Tiempo desde última lectura: %lu ms\n", currentTime - lastReadingTime);
        
        sensors.createReadingsBatch(readingsDoc, getCurrentISOTimestamp);
        
        // Validate JSON before sending
        validateReadingsJson(readingsDoc);
        
        if (mqttHandler.publishReadings(readingsDoc)) {
            Serial.println("🎉 CICLO DE TELEMETRÍA COMPLETADO EXITOSAMENTE");
        } else {
            Serial.println("⚠️  CICLO DE TELEMETRÍA FALLÓ - datos buffereados");
        }
        
        lastReadingTime = currentTime;
        Serial.printf("⏰ Próxima telemetría en %d segundos\n", READING_INTERVAL / 1000);
    }
    
    // Send status every 5 minutes
    if (currentTime - lastStatusTime >= 300000) {
        if (mqttHandler.isConnected()) {
            mqttHandler.publishStatus("running", getCurrentISOTimestamp);
            Serial.println("📤 Estado publicado");
        }
        
        lastStatusTime = currentTime;
    }
    
    // Watchdog and memory monitoring
    if (currentTime - lastWatchdogTime >= 30000) { // Check every 30 seconds
        uint32_t freeHeap = ESP.getFreeHeap();
        Serial.printf("🔍 Watchdog - Free heap: %u bytes\n", freeHeap);
        Serial.printf("🔗 Estado MQTT: %s\n", mqttHandler.isConnected() ? "Conectado" : "Desconectado");
        Serial.printf("📶 Estado WiFi: %s\n", mqttHandler.isWiFiConnected() ? "Conectado" : "Desconectado");

        if (freeHeap < 10000) {
            Serial.println("🚨 Memoria crítica - reiniciando...");
            jobScheduler.emergencyStop();
            delay(1000);
            ESP.restart();
        }

        // 🛡️ Reinicio por fallo prolongado de WiFi (30 minutos sin conexión)
        if (!mqttHandler.isWiFiConnected() &&
            (currentTime - mqttHandler.getLastWifiAttemptTime() > 1800000)) { // 30 minutos sin Wi-Fi
            Serial.println("🚨 [WDT] Reinicio por fallo prolongado de WiFi (30 min).");
            jobScheduler.emergencyStop();
            delay(1000);
            ESP.restart();
        }

        // Print scheduler status periodically
        jobScheduler.printStatus();

        // Print connection diagnostics
        if (!mqttHandler.isConnected()) {
            Serial.println("⚠️  MQTT desconectado - verificando reconexión...");
        }

        lastWatchdogTime = currentTime;
    }
    
    delay(100); // Small delay for stability
}
