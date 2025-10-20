#include "actuators.h"

// ========================================
// VARIABLES ESTÁTICAS
// ========================================
int ActuatorController::currentFanSpeed = 0;
bool ActuatorController::fanPwmAttached = false;

// ========================================
// INICIALIZACIÓN
// ========================================
void ActuatorController::begin() {
    Serial.println("🔧 Inicializando controlador de actuadores...");
    
    // Configurar todos los pines como OUTPUT
    pinMode(PIN_RELAY_HEATER, OUTPUT);
    pinMode(PIN_LED_COLOUR, OUTPUT);
    pinMode(PIN_AIR_STONE, OUTPUT);
    pinMode(PIN_WATER_PUMP, OUTPUT);
    pinMode(PIN_FAN, OUTPUT);
    pinMode(PIN_HEATER_WATER, OUTPUT);

    // HUMIDIFICADOR ULTRASÓNICO
    // PIN 13: Generador de niebla
    // PIN 14: Ventilador interno
    pinMode(PIN_HUMID_RELAY, OUTPUT);
    pinMode(PIN_HUMID_POWER, OUTPUT);

    // Inicializar todos los relés en estado APAGADO (HIGH por lógica invertida)
    digitalWrite(PIN_RELAY_HEATER, HIGH);
    digitalWrite(PIN_LED_COLOUR, HIGH);
    digitalWrite(PIN_AIR_STONE, HIGH);
    digitalWrite(PIN_WATER_PUMP, HIGH);
    digitalWrite(PIN_FAN, LOW);  // Fan is non-inverted: LOW = OFF
    digitalWrite(PIN_HEATER_WATER, HIGH);

    // HUMIDIFICADOR - Inicialmente apagado
    digitalWrite(PIN_HUMID_RELAY, HIGH);   // PIN 13 OFF
    digitalWrite(PIN_HUMID_POWER, HIGH);   // PIN 14 OFF
    
    Serial.println("✅ Actuadores inicializados - Todos apagados");
}

// ========================================
// CALEFACTOR DE AMBIENTE
// ========================================
void ActuatorController::turnHeaterOn() {
    digitalWrite(PIN_RELAY_HEATER, LOW);  // ACTIVO LOW
    Serial.println("🔥 Calefactor ENCENDIDO");
}

void ActuatorController::turnHeaterOff() {
    digitalWrite(PIN_RELAY_HEATER, HIGH);  // ACTIVO LOW
    Serial.println("🔥 Calefactor APAGADO");
}

bool ActuatorController::isHeaterOn() {
    return digitalRead(PIN_RELAY_HEATER) == LOW;
}

// ========================================
// LUZ COLORES / BOMBILLO
// ========================================
void ActuatorController::turnLightOn() {
    digitalWrite(PIN_LED_COLOUR, LOW);  // ACTIVO LOW
    Serial.println("💡 Luz ENCENDIDA");
}

void ActuatorController::turnLightOff() {
    digitalWrite(PIN_LED_COLOUR, HIGH);  // ACTIVO LOW
    Serial.println("💡 Luz APAGADA");
}

bool ActuatorController::isLightOn() {
    return digitalRead(PIN_LED_COLOUR) == LOW;
}

// ========================================
// PIEDRA DIFUSORA (AIRE)
// ========================================
void ActuatorController::turnAirStoneOn() {
    digitalWrite(PIN_AIR_STONE, LOW);  // ACTIVO LOW
    Serial.println("💨 Piedra difusora ENCENDIDA");
}

void ActuatorController::turnAirStoneOff() {
    digitalWrite(PIN_AIR_STONE, HIGH);  // ACTIVO LOW
    Serial.println("💨 Piedra difusora APAGADA");
}

bool ActuatorController::isAirStoneOn() {
    return digitalRead(PIN_AIR_STONE) == LOW;
}

// ========================================
// BOMBA DE AGUA
// ========================================
void ActuatorController::turnWaterPumpOn() {
    digitalWrite(PIN_WATER_PUMP, LOW);  // ACTIVO LOW
    Serial.println("🌊 Bomba de agua ENCENDIDA");
}

void ActuatorController::turnWaterPumpOff() {
    digitalWrite(PIN_WATER_PUMP, HIGH);  // ACTIVO LOW
    Serial.println("🌊 Bomba de agua APAGADA");
}

bool ActuatorController::isWaterPumpOn() {
    return digitalRead(PIN_WATER_PUMP) == LOW;
}

// ========================================
// CALENTADOR DE AGUA
// ========================================
void ActuatorController::turnWaterHeaterOn() {
    digitalWrite(PIN_HEATER_WATER, LOW);  // ACTIVO LOW
    int pinState = digitalRead(PIN_HEATER_WATER);
    Serial.printf("🌡️ Calentador de agua ENCENDIDO (PIN %d = %s)\n",
                  PIN_HEATER_WATER, pinState == LOW ? "LOW ✅" : "HIGH ⚠️");
}

void ActuatorController::turnWaterHeaterOff() {
    digitalWrite(PIN_HEATER_WATER, HIGH);  // ACTIVO LOW
    Serial.println("🌡️ Calentador de agua APAGADO");
}

bool ActuatorController::isWaterHeaterOn() {
    return digitalRead(PIN_HEATER_WATER) == LOW;
}

// ========================================
// VENTILADOR (Digital)
// ========================================
void ActuatorController::turnFanOn() {
    if (fanPwmAttached) {
        detachFanPWM();
    }
    digitalWrite(PIN_FAN, HIGH);  // Fan is non-inverted: HIGH = ON
    currentFanSpeed = 100;
    Serial.println("🌪️ Ventilador ENCENDIDO (digital)");
}

void ActuatorController::turnFanOff() {
    if (fanPwmAttached) {
        detachFanPWM();
    }
    digitalWrite(PIN_FAN, LOW);  // Fan is non-inverted: LOW = OFF
    currentFanSpeed = 0;
    Serial.println("🌪️ Ventilador APAGADO");
}

bool ActuatorController::isFanOn() {
    return currentFanSpeed > 0;
}

// ========================================
// VENTILADOR (PWM)
// ========================================
void ActuatorController::setFanSpeed(int speedPercent) {
    if (speedPercent < 0) speedPercent = 0;
    if (speedPercent > 100) speedPercent = 100;
    
    if (speedPercent == 0) {
        turnFanOff();
        return;
    }
    
    // Asegurar que PWM esté configurado
    if (!fanPwmAttached) {
        attachFanPWM();
    }
    
    // Convertir porcentaje a duty cycle (0-255)
    int dutyCycle = map(speedPercent, 0, 100, 0, 255);
    ledcWrite(0, dutyCycle);  // Canal 0 para ventilador
    
    currentFanSpeed = speedPercent;
    Serial.printf("🌪️ Ventilador PWM: %d%% (duty=%d/255)\n", speedPercent, dutyCycle);
}

int ActuatorController::getFanSpeed() {
    return currentFanSpeed;
}

void ActuatorController::attachFanPWM() {
    ledcSetup(0, 5000, 8);  // Canal 0, 5kHz, 8-bit resolution
    ledcAttachPin(PIN_FAN, 0);
    fanPwmAttached = true;
    Serial.println("🔧 PWM del ventilador configurado");
}

void ActuatorController::detachFanPWM() {
    ledcDetachPin(PIN_FAN);
    pinMode(PIN_FAN, OUTPUT);
    fanPwmAttached = false;
    Serial.println("🔧 PWM del ventilador desconfigurado");
}

// ========================================
// HUMIDIFICADOR ULTRASÓNICO
// ========================================
// PIN 13: Generador de niebla ultrasónico (activo en bajo)
// PIN 14: Ventilador interno del humidificador (activo en bajo)

void ActuatorController::turnHumidifierMasterOn() {
    digitalWrite(PIN_HUMID_POWER, LOW);  // PIN 14: ACTIVO LOW
    Serial.println("🌬️ Ventilador humidificador (PIN 14) ENCENDIDO");
}

void ActuatorController::turnHumidifierMasterOff() {
    digitalWrite(PIN_HUMID_POWER, HIGH);  // PIN 14: ACTIVO LOW
    Serial.println("🌬️ Ventilador humidificador (PIN 14) APAGADO");
}

void ActuatorController::turnHumidifierRelayOn() {
    digitalWrite(PIN_HUMID_RELAY, LOW);  // PIN 13: ACTIVO LOW
    Serial.println("🌫️ Generador de niebla (PIN 13) ENCENDIDO");
}

void ActuatorController::turnHumidifierRelayOff() {
    digitalWrite(PIN_HUMID_RELAY, HIGH);  // PIN 13: ACTIVO LOW
    Serial.println("🌫️ Generador de niebla (PIN 13) APAGADO");
}

// ========================================
// UTILIDADES
// ========================================
void ActuatorController::emergencyStop() {
    Serial.println("🚨 PARADA DE EMERGENCIA - Apagando todos los actuadores");
    
    turnHeaterOff();
    turnLightOff();
    turnAirStoneOff();
    turnWaterPumpOff();
    turnFanOff();
    turnWaterHeaterOff();
    turnHumidifierMasterOff();
    turnHumidifierRelayOff();
    
    Serial.println("✅ Todos los actuadores apagados");
}

void ActuatorController::printStatus() {
    Serial.println("📊 Estado de actuadores:");
    Serial.printf("  🔥 Calefactor: %s\n", isHeaterOn() ? "ON" : "OFF");
    Serial.printf("  💡 Luz: %s\n", isLightOn() ? "ON" : "OFF");
    Serial.printf("  💨 Piedra difusora: %s\n", isAirStoneOn() ? "ON" : "OFF");
    Serial.printf("  🌊 Bomba agua: %s\n", isWaterPumpOn() ? "ON" : "OFF");
    Serial.printf("  🌪️ Ventilador: %s (%d%%)\n", isFanOn() ? "ON" : "OFF", currentFanSpeed);
    Serial.printf("  🌡️ Calentador agua: %s\n", isWaterHeaterOn() ? "ON" : "OFF");
    Serial.printf("  🌫️ Generador niebla (PIN 13): %s\n", digitalRead(PIN_HUMID_RELAY) == LOW ? "ON" : "OFF");
    Serial.printf("  🌬️ Ventilador humid (PIN 14): %s\n", digitalRead(PIN_HUMID_POWER) == LOW ? "ON" : "OFF");
}