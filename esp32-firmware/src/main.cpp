#include <Arduino.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include "config.h"
#include "sensors.h"
#include "mqtt_handler.h"
#include "job_scheduler.h"

// NTP Client
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0, 60000);

// Global objects
SensorManager sensors(&timeClient);
MQTTHandler mqttHandler(&jobScheduler);

// Timing variables
unsigned long lastReadingTime = 0;
unsigned long lastStatusTime = 0;
unsigned long lastWatchdogTime = 0;

// NTP initialization flag
bool ntpInitialized = false;

// JSON documents
DynamicJsonDocument readingsDoc(4096);

String getCurrentISOTimestamp() {
    if (!ntpInitialized) {
        return "1970-01-01T00:00:00Z";
    }

    if (!timeClient.isTimeSet()) {
        timeClient.forceUpdate();
    }

    unsigned long epochTime = timeClient.getEpochTime();
    time_t rawtime = epochTime;
    struct tm * timeinfo = gmtime(&rawtime);

    char isoBuffer[32];
    strftime(isoBuffer, sizeof(isoBuffer), "%Y-%m-%dT%H:%M:%SZ", timeinfo);

    return String(isoBuffer);
}

void setup() {
    Serial.begin(115200);
    delay(2000);

    Serial.printf("HydroEspinaca ESP32 %s - ID: %s\n", FIRMWARE_VERSION, ESP32_ID);
    Serial.printf("Free heap: %u bytes\n", ESP.getFreeHeap());

    sensors.begin();
    jobScheduler.begin();
    mqttHandler.begin();
    jobScheduler.setMQTTHandler(&mqttHandler);
    jobScheduler.setSensorManager(&sensors);

    Serial.println("System initialized");
}

void loop() {
    unsigned long currentTime = millis();

    // Initialize NTP only after WiFi is connected
    if (!ntpInitialized && mqttHandler.isWiFiConnected()) {
        timeClient.begin();
        timeClient.forceUpdate();
        if (timeClient.isTimeSet()) {
            ntpInitialized = true;
        }
    }

    if (ntpInitialized) {
        timeClient.update();
    }

    mqttHandler.loop();
    jobScheduler.loop();

    // Send telemetry every READING_INTERVAL (120 seconds)
    if (currentTime - lastReadingTime >= READING_INTERVAL) {
        sensors.createReadingsBatch(readingsDoc, getCurrentISOTimestamp);
        mqttHandler.publishReadings(readingsDoc);
        lastReadingTime = currentTime;
    }

    // Send status every 5 minutes
    if (currentTime - lastStatusTime >= 300000) {
        if (mqttHandler.isConnected()) {
            mqttHandler.publishStatus("running", getCurrentISOTimestamp);
        }
        lastStatusTime = currentTime;
    }

    // Watchdog and memory monitoring every 30 seconds
    if (currentTime - lastWatchdogTime >= 30000) {
        uint32_t freeHeap = ESP.getFreeHeap();

        // Critical memory check
        if (freeHeap < 10000) {
            Serial.printf("CRITICAL: Low memory %u bytes - Restarting\n", freeHeap);
            jobScheduler.emergencyStop();
            delay(1000);
            ESP.restart();
        }

        // WiFi prolonged failure check (30 min)
        if (!mqttHandler.isWiFiConnected() &&
            (currentTime - mqttHandler.getLastWifiAttemptTime() > 1800000)) {
            Serial.println("CRITICAL: WiFi down >30min - Restarting");
            jobScheduler.emergencyStop();
            delay(1000);
            ESP.restart();
        }

        lastWatchdogTime = currentTime;
    }

    delay(100);
}
