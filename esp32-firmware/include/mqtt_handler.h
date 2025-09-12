#ifndef MQTT_HANDLER_H
#define MQTT_HANDLER_H

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include "actuators.h"

class MQTTHandler {
private:
    WiFiClient wifiClient;
    PubSubClient mqttClient;
    ActuatorManager* actuatorManager;
    
    // Connection management
    unsigned long lastReconnectAttempt;
    bool shouldReconnect;
    
    // Callback function
    static void messageCallback(char* topic, byte* payload, unsigned int length);
    static MQTTHandler* instance; // For static callback
    
    // Internal methods
    bool connectWiFi();
    bool connectMQTT();
    void handleCommand(const String& payload);
    
public:
    MQTTHandler(ActuatorManager* actuators);
    void begin();
    void loop();
    
    // Publishing
    bool publishReadings(DynamicJsonDocument& readings);
    bool publishStatus(const String& status);
    bool publishState(DynamicJsonDocument& state);
    
    // Connection status
    bool isConnected();
    bool isWiFiConnected();
    
    // Message handling
    void onMessageReceived(char* topic, byte* payload, unsigned int length);
};

#endif