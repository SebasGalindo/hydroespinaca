#ifndef MQTT_HANDLER_H
#define MQTT_HANDLER_H

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <queue>

// Forward declaration
class JobScheduler;

struct TelemetryBuffer {
    String payload;
    unsigned long timestamp;
};

class MQTTHandler {
private:
    WiFiClient wifiClient;
    PubSubClient mqttClient;
    JobScheduler* jobScheduler;
    
    // Connection management with exponential backoff
    unsigned long lastReconnectAttempt;
    unsigned long reconnectInterval;
    int reconnectAttempts;
    
    // Telemetry buffering
    std::queue<TelemetryBuffer> telemetryQueue;
    static const int MAX_BUFFERED_TELEMETRY = 10;
    
    // Callback function
    static void messageCallback(char* topic, byte* payload, unsigned int length);
    static MQTTHandler* instance;
    
    // Internal methods
    bool connectWiFi();
    bool connectMQTT();
    void handleJobSchedule(const String& payload);
    void processBufferedTelemetry();
    String getCurrentTimestamp();
    unsigned long getBackoffInterval();
    
public:
    MQTTHandler(JobScheduler* scheduler);
    void begin();
    void loop();
    
    // Publishing methods
    bool publishReadings(DynamicJsonDocument& readings);
    bool publishStatus(const String& status);
    bool publishCompletion(const DynamicJsonDocument& completion);
    bool publishNotification(const DynamicJsonDocument& notification);
    
    // Connection status
    bool isConnected();
    bool isWiFiConnected();
    
    // Message handling
    void onMessageReceived(char* topic, byte* payload, unsigned int length);
    
    // Buffer telemetry when offline
    void bufferTelemetry(const String& payload);
};

#endif