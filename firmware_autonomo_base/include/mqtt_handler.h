#ifndef MQTT_HANDLER_H
#define MQTT_HANDLER_H

// Increase MQTT buffer size before including PubSubClient
#define MQTT_MAX_PACKET_SIZE 1024

#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <queue>

struct TelemetryBuffer {
    String payload;
    unsigned long timestamp;
};

class MQTTHandler {
private:
    WiFiClientSecure secureClient;
    PubSubClient mqttClient;
    
    // WiFi connection backoff
    unsigned long lastWifiReconnectAttempt = 0;
    unsigned long currentWifiReconnectDelay = 1000; // Start at 1 second
    const unsigned long MAX_WIFI_RECONNECT_DELAY = 120000; // Max 2 minutes
    const int MAX_WIFI_ATTEMPTS = 50; // Max attempts before restart
    int wifiReconnectAttempts = 0;
    
    // MQTT connection backoff
    unsigned long lastMqttReconnectAttempt = 0;
    unsigned long currentMqttReconnectDelay = 2000; // Start at 2 seconds
    const unsigned long MAX_MQTT_RECONNECT_DELAY = 120000; // Max 2 minutes
    
    // Telemetry buffering
    std::queue<TelemetryBuffer> telemetryQueue;
    static const int MAX_BUFFERED_TELEMETRY = 10;
    
    static MQTTHandler* instance;
    
    // Internal methods
    bool connectWiFi();
    bool connectMQTT();
    void processBufferedTelemetry();
    String getCurrentTimestamp();
    
public:
    MQTTHandler();
    void begin();
    void loop();
    
    // Publishing methods
    bool publishReadings(DynamicJsonDocument& readings);
    bool publishStatus(const String& status, String (*timestampFunction)() = nullptr);
    
    // Connection status
    bool isConnected();
    bool isWiFiConnected();
    
    // WiFi watchdog support
    unsigned long getLastWifiAttemptTime() const { return lastWifiReconnectAttempt; }
    
    // Buffer telemetry when offline
    void bufferTelemetry(const String& payload);
};

#endif