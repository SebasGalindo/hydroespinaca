#ifndef MQTT_HANDLER_H
#define MQTT_HANDLER_H

// Increase MQTT buffer size before including PubSubClient
#define MQTT_MAX_PACKET_SIZE 1024

#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <queue>

// Forward declaration
class JobScheduler;

// Buffer structure for completions and notifications (not telemetry)
struct EventBuffer {
    String payload;
    unsigned long timestamp;
    String type;  // "completion" or "notification"
};

class MQTTHandler {
private:
    WiFiClientSecure secureClient;
    PubSubClient mqttClient;
    JobScheduler* jobScheduler;

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

    // Event buffering (completions and notifications only - NOT telemetry)
    std::queue<EventBuffer> eventQueue;
    static const int MAX_BUFFERED_EVENTS = 20;  // Increased for critical events
    
    // Callback function
    static void messageCallback(char* topic, byte* payload, unsigned int length);
    static MQTTHandler* instance;
    
    // Internal methods
    bool connectWiFi();
    bool connectMQTT();
    void handleJobSchedule(const String& payload);
    void processBufferedEvents();
    void bufferEvent(const String& payload, const String& type);
    String getCurrentTimestamp();
    
public:
    MQTTHandler(JobScheduler* scheduler);
    void begin();
    void loop();
    
    // Publishing methods
    bool publishReadings(DynamicJsonDocument& readings);
    bool publishStatus(const String& status, String (*timestampFunction)() = nullptr);
    bool publishCompletion(const DynamicJsonDocument& completion);
    bool publishNotification(const DynamicJsonDocument& notification);
    
    // Connection status
    bool isConnected();
    bool isWiFiConnected();

    // WiFi watchdog support
    unsigned long getLastWifiAttemptTime() const { return lastWifiReconnectAttempt; }

    // Message handling
    void onMessageReceived(char* topic, byte* payload, unsigned int length);
};

#endif