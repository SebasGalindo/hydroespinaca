#include <Arduino.h>
#include "config.h"
#include "sensors.h"
#include "actuators.h"
#include "mqtt_handler.h"

// Global objects
SensorManager sensors;
ActuatorManager actuators;
MQTTHandler mqttHandler(&actuators);

// Timing variables
unsigned long lastReadingTime = 0;
unsigned long lastStatusTime = 0;

// JSON documents (reutilizables para evitar allocaciones constantes)
DynamicJsonDocument readingsDoc(2048);  // Buffer para lecturas de sensores
DynamicJsonDocument stateDoc(1024);     // Buffer para estado de actuadores

void setup() {
    Serial.begin(115200);
    Serial.println("\n🌱 HydroEspinaca ESP32 - Iniciando...");
    Serial.printf("📋 Firmware: %s\n", FIRMWARE_VERSION);
    Serial.printf("🆔 ESP32 ID: %s\n", ESP32_ID);
    
    // Initialize components
    sensors.begin();
    actuators.begin();
    mqttHandler.begin();
    
    Serial.println("🚀 Sistema iniciado correctamente!\n");
}

void loop() {
    unsigned long currentTime = millis();
    
    // Mantener conexiones MQTT
    mqttHandler.loop();
    
    // Enviar lecturas cada READING_INTERVAL
    if (currentTime - lastReadingTime >= READING_INTERVAL) {
        if (mqttHandler.isConnected()) {
            Serial.println("📊 Leyendo sensores...");
            
            sensors.createReadingsBatch(readingsDoc);
            
            if (mqttHandler.publishReadings(readingsDoc)) {
                Serial.println("✅ Datos enviados correctamente");
            }
        } else {
            Serial.println("⚠️  MQTT desconectado - saltando lectura");
        }
        
        lastReadingTime = currentTime;
    }
    
    // Enviar estado cada 5 minutos
    if (currentTime - lastStatusTime >= 300000) { // 5 minutos
        if (mqttHandler.isConnected()) {
            actuators.getStateAsJson(stateDoc);
            mqttHandler.publishState(stateDoc);
            mqttHandler.publishStatus("running");
        }
        
        lastStatusTime = currentTime;
    }
    
    // Watchdog y manejo de memoria
    if (ESP.getFreeHeap() < 10000) {
        Serial.println("⚠️  Memoria baja - reiniciando...");
        ESP.restart();
    }
    
    delay(100); // Small delay para estabilidad
}
