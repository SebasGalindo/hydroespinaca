#include <Arduino.h>
#include "config.h"
#include "sensors.h"
#include "mqtt_handler.h"
#include "job_scheduler.h"

// Global objects
SensorManager sensors;
MQTTHandler mqttHandler(&jobScheduler);

// Timing variables
unsigned long lastReadingTime = 0;
unsigned long lastStatusTime = 0;
unsigned long lastWatchdogTime = 0;

// JSON documents
DynamicJsonDocument readingsDoc(2048);

void setup() {
    Serial.begin(115200);
    Serial.println("\n🌱 HydroEspinaca ESP32 v2.0 - Iniciando...");
    Serial.printf("📋 Firmware: %s\n", FIRMWARE_VERSION);
    Serial.printf("🆔 ESP32 ID: %s\n", ESP32_ID);
    Serial.printf("🏷️  Client ID: %s\n", MQTT_CLIENT_ID);
    
    // Initialize components in order
    Serial.println("🔧 Inicializando sensores...");
    sensors.begin();
    
    Serial.println("🔧 Inicializando JobScheduler...");
    jobScheduler.begin();
    
    Serial.println("🔧 Inicializando MQTT...");
    mqttHandler.begin();
    
    Serial.println("🔧 Vinculando JobScheduler con MQTT...");
    jobScheduler.setMQTTHandler(&mqttHandler);
    
    Serial.println("🚀 Sistema iniciado correctamente!\n");
    Serial.println("📊 Publicando telemetría cada 60 segundos");
    Serial.println("📡 Escuchando job schedules en: " + String(TOPIC_JOB_SCHEDULE));
}

void loop() {
    unsigned long currentTime = millis();
    
    // Maintain MQTT connections
    mqttHandler.loop();
    
    // JobScheduler maintenance
    jobScheduler.loop();
    
    // Send telemetry every READING_INTERVAL (60 seconds)
    if (currentTime - lastReadingTime >= READING_INTERVAL) {
        Serial.println("📊 Leyendo sensores...");
        sensors.createReadingsBatch(readingsDoc);
        
        if (mqttHandler.publishReadings(readingsDoc)) {
            Serial.println("✅ Telemetría enviada");
        } else {
            Serial.println("📦 Telemetría buffereada (MQTT offline)");
        }
        
        lastReadingTime = currentTime;
    }
    
    // Send status every 5 minutes
    if (currentTime - lastStatusTime >= 300000) {
        if (mqttHandler.isConnected()) {
            mqttHandler.publishStatus("running");
            Serial.println("📤 Estado publicado");
        }
        
        lastStatusTime = currentTime;
    }
    
    // Watchdog and memory monitoring
    if (currentTime - lastWatchdogTime >= 30000) { // Check every 30 seconds
        uint32_t freeHeap = ESP.getFreeHeap();
        Serial.printf("🔍 Free heap: %u bytes\n", freeHeap);
        
        if (freeHeap < 10000) {
            Serial.println("🚨 Memoria crítica - reiniciando...");
            jobScheduler.emergencyStop();
            delay(1000);
            ESP.restart();
        }
        
        // Print scheduler status periodically
        jobScheduler.printStatus();
        
        lastWatchdogTime = currentTime;
    }
    
    delay(100); // Small delay for stability
}
