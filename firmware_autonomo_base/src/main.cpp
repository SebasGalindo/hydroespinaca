#include <Arduino.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include "config.h"
#include "sensors.h"
#include "mqtt_handler.h"
#include "actuators.h"
#include "control.h"

// NTP Client
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0, 60000); // UTC+0 (estándar), update every 60s

// Global objects
SensorManager sensors(&timeClient);
MQTTHandler mqttHandler;
AutonomousController controller(&sensors, &timeClient);

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
            Serial.printf("   [%d] physicalId: %s, variableCode: %s, value: %.2f\n", 
                         i, 
                         reading["physicalId"].as<String>().c_str(),
                         reading["variableCode"].as<String>().c_str(),
                         reading["value"].as<float>());
        }
    }
    
    Serial.printf("📏 Tamaño total del documento: %d bytes\n", doc.memoryUsage());
}

void printSystemInfo() {
    Serial.println("════════════════════════════════════════════════");
    Serial.println("🌱 HydroEspinaca ESP32 AUTÓNOMO - INFORMACIÓN DEL SISTEMA");
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
    Serial.printf("🏷️  Topic Status: %s\n", TOPIC_STATUS);
    
    Serial.println("════════════════════════════════════════════════");
}

void setup() {
    Serial.begin(115200);
    delay(2000); // Dar tiempo para que se abra el monitor serie
    
    printSystemInfo();
    
    Serial.println("🔧 Inicializando sensores...");
    sensors.begin();
    
    Serial.println("🔧 Inicializando controlador de actuadores...");
    ActuatorController::begin();
    
    Serial.println("🔧 Conectando WiFi y configurando MQTT...");
    mqttHandler.begin();  // Non-blocking WiFi connection start
    
    // Initialize NTP (will sync in loop)
    Serial.println("🔧 Inicializando sincronización NTP...");
    timeClient.begin();
    Serial.println("📅 NTP se sincronizará en segundo plano (no bloqueante)");
    
    Serial.println("🔧 Inicializando controlador autónomo...");
    controller.begin();
    
    Serial.println("🚀 Sistema autónomo iniciado correctamente!\n");
    Serial.println("📊 Publicando telemetría cada 2 minutos");
    Serial.println("🤖 Control autónomo activo cada 4 minutos");
    Serial.println("🎛️  Reglas: temperatura aire/agua, humedad, luz, rutinas periódicas");
    Serial.println("🛡️  Sistema robusto: funciona sin WiFi/NTP si es necesario");
}

void loop() {
    unsigned long currentTime = millis();
    
    // 👇 Esto debe ir siempre primero: Garantiza reconexión no bloqueante.
    mqttHandler.loop();
    
    // Update NTP time
    timeClient.update();
    
    // Run autonomous control system
    controller.loop();
    
    // Run heater fast monitoring (3-second intervals when heater is ON)
    controller.heaterFastMonitoring();
    
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
            ActuatorController::emergencyStop();
            delay(1000);
            ESP.restart();
        }
        
        // 🛡️ AÑADIR: Condición de reinicio por fallo prolongado de WiFi
        if (!mqttHandler.isWiFiConnected() && 
            (currentTime - mqttHandler.getLastWifiAttemptTime() > 1800000)) { // 30 minutos sin Wi-Fi
            Serial.println("🚨 [WDT] Reinicio por fallo prolongado de WiFi (30 min).");
            ActuatorController::emergencyStop();
            delay(1000);
            ESP.restart();
        }
        
        // Print system status periodically
        ActuatorController::printStatus();
        controller.printSystemStatus();
        
        // Print connection diagnostics
        if (!mqttHandler.isConnected()) {
            Serial.println("⚠️  MQTT desconectado - verificando reconexión...");
        }
        
        lastWatchdogTime = currentTime;
    }
    
    delay(100); // Small delay for stability
}
