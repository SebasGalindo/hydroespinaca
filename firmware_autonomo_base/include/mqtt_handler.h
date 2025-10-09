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
    
    // Connection management with exponential backoff
    unsigned long lastReconnectAttempt;
    unsigned long reconnectInterval;
    int reconnectAttempts;
    
    // Telemetry buffering
    std::queue<TelemetryBuffer> telemetryQueue;
    static const int MAX_BUFFERED_TELEMETRY = 10;
    
    static MQTTHandler* instance;
    
    // Internal methods
    bool connectWiFi();
    bool connectMQTT();
    void processBufferedTelemetry();
    String getCurrentTimestamp();
    unsigned long getBackoffInterval();
    
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
    
    // Buffer telemetry when offline
    void bufferTelemetry(const String& payload);
};

#endif