#include "actuators.h"
#include "config.h"

ActuatorManager::ActuatorManager() {
    // Initialize state
    currentState.pump = false;
    currentState.ledGrow = false;
    currentState.valveNutrients = false;
    currentState.fan = false;
    currentState.ledIntensity = 0;
    currentState.fanSpeed = 0;
}

void ActuatorManager::begin() {
    // Configure pins
    pinMode(PUMP_PIN, OUTPUT);
    pinMode(LED_GROW_PIN, OUTPUT);
    pinMode(VALVE_NUTRIENTS_PIN, OUTPUT);
    pinMode(FAN_PIN, OUTPUT);
    
    // Initialize all OFF
    emergencyStop();
    
    Serial.println("✅ Actuadores inicializados");
}

void ActuatorManager::setPump(bool state) {
    currentState.pump = state;
    digitalWrite(PUMP_PIN, state ? HIGH : LOW);
    Serial.printf("💧 Bomba: %s\n", state ? "ON" : "OFF");
}

void ActuatorManager::setLedGrow(bool state, int intensity) {
    currentState.ledGrow = state;
    currentState.ledIntensity = state ? constrain(intensity, 0, 255) : 0;
    
    if (state) {
        analogWrite(LED_GROW_PIN, currentState.ledIntensity);
    } else {
        digitalWrite(LED_GROW_PIN, LOW);
    }
    
    Serial.printf("💡 LED Grow: %s (Intensidad: %d)\n", 
                  state ? "ON" : "OFF", currentState.ledIntensity);
}

void ActuatorManager::setNutrientsValve(bool state) {
    currentState.valveNutrients = state;
    digitalWrite(VALVE_NUTRIENTS_PIN, state ? HIGH : LOW);
    Serial.printf("🧪 Válvula nutrientes: %s\n", state ? "ABIERTA" : "CERRADA");
}

void ActuatorManager::setFan(bool state, int speed) {
    currentState.fan = state;
    currentState.fanSpeed = state ? constrain(speed, 0, 255) : 0;
    
    if (state) {
        analogWrite(FAN_PIN, currentState.fanSpeed);
    } else {
        digitalWrite(FAN_PIN, LOW);
    }
    
    Serial.printf("🌀 Ventilador: %s (Velocidad: %d)\n", 
                  state ? "ON" : "OFF", currentState.fanSpeed);
}

bool ActuatorManager::processCommand(const DynamicJsonDocument& command) {
    try {
        if (command.containsKey("pump")) {
            setPump(command["pump"]);
        }
        
        if (command.containsKey("ledGrow")) {
            bool state = command["ledGrow"];
            int intensity = command.containsKey("ledIntensity") ? 
                           command["ledIntensity"] : 255;
            setLedGrow(state, intensity);
        }
        
        if (command.containsKey("valveNutrients")) {
            setNutrientsValve(command["valveNutrients"]);
        }
        
        if (command.containsKey("fan")) {
            bool state = command["fan"];
            int speed = command.containsKey("fanSpeed") ? 
                       command["fanSpeed"] : 255;
            setFan(state, speed);
        }
        
        if (command.containsKey("emergencyStop") && command["emergencyStop"]) {
            emergencyStop();
        }
        
        return true;
    } catch (...) {
        Serial.println("❌ Error procesando comando");
        return false;
    }
}

ActuatorState ActuatorManager::getCurrentState() {
    return currentState;
}

void ActuatorManager::getStateAsJson(DynamicJsonDocument& doc) {
    // Clear document
    doc.clear();
    
    doc["esp32Id"] = ESP32_ID;
    
    // ISO 8601 timestamp
    char timestamp[32];
    unsigned long currentTime = millis() / 1000;
    sprintf(timestamp, "2025-07-29T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    doc["timestamp"] = timestamp;
    
    JsonObject state = doc.createNestedObject("state");
    state["pump"] = currentState.pump;
    state["ledGrow"] = currentState.ledGrow;
    state["ledIntensity"] = currentState.ledIntensity;
    state["valveNutrients"] = currentState.valveNutrients;
    state["fan"] = currentState.fan;
    state["fanSpeed"] = currentState.fanSpeed;
}

void ActuatorManager::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA");
    
    currentState.pump = false;
    currentState.ledGrow = false;
    currentState.valveNutrients = false;
    currentState.fan = false;
    currentState.ledIntensity = 0;
    currentState.fanSpeed = 0;
    
    digitalWrite(PUMP_PIN, LOW);
    digitalWrite(LED_GROW_PIN, LOW);
    digitalWrite(VALVE_NUTRIENTS_PIN, LOW);
    digitalWrite(FAN_PIN, LOW);
}

bool ActuatorManager::isInSafeState() {
    // Implementar lógica de seguridad según tu sistema
    return true;
}