#ifndef ACTUATORS_H
#define ACTUATORS_H

#include <Arduino.h>
#include <ArduinoJson.h>

struct ActuatorState {
    bool pump;
    bool ledGrow;
    bool valveNutrients;
    bool fan;
    int ledIntensity;  // PWM 0-255
    int fanSpeed;      // PWM 0-255
};

class ActuatorManager {
private:
    ActuatorState currentState;
    
public:
    ActuatorManager();
    void begin();
    
    // Individual controls
    void setPump(bool state);
    void setLedGrow(bool state, int intensity = 255);
    void setNutrientsValve(bool state);
    void setFan(bool state, int speed = 255);
    
    // Batch control from JSON
    bool processCommand(const DynamicJsonDocument& command);
    
    // State management
    ActuatorState getCurrentState();
    void getStateAsJson(DynamicJsonDocument& doc);
    
    // Safety functions
    void emergencyStop();
    bool isInSafeState();
};

#endif