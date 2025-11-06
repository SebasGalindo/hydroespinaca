#pragma once

// ========================================
// SOLO SENSORES - Actuadores controlados dinámicamente por backend
// ========================================

// Sensores básicos
#define PIN_DHT22        4

// I2C para sensores (BH1750, TCS34725, etc.)
#define PIN_I2C_SDA      21
#define PIN_I2C_SCL      22

// Sensores ADC
#define PIN_PH_ADC       32
#define PIN_TDS_ADC      33
#define PIN_NTC_TANK     35

// Sensor ultrasónico HC-SR04 (5V logic)
#define PIN_ULTRA_TRIG   26     // Trigger (5V tolerante)
#define PIN_ULTRA_ECHO   5      // Echo (⚠️ REQUIERE divisor resistivo 1kΩ+2kΩ para 5V→3.3V)

// Constantes de calibración para sensores
#define TANK_HEIGHT_CM 40.0f         // Altura total del tanque en cm
#define TANK_MAX_DISTANCE_CM 12.48f  // Límite del tanque en cm
#define ADC_RESOLUTION   4096.0f  // 12-bit ADC
#define ADC_VREF        3.3f      // Voltaje de referencia

// ========================================
// EXCEPCIÓN: Actuadores con monitoreo especializado
// ========================================
// Actuadores hardcodeados que requieren monitoreo de sensores

// Humidificador ultrasónico (control con 2 relés ACTIVOS LOW)
#define PIN_HUMID_RELAY  13     // Generador de niebla ultrasónico (activo en bajo)
#define PIN_HUMID_POWER  14     // Ventilador interno del humidificador (activo en bajo)

// Calefactor de ambiente (monitoreo de temperatura)
#define PIN_RELAY_HEATER 17     // Relé del calefactor con monitoreo térmico

// NOTA: Todos los demás actuadores NO están definidos aquí
// El backend enviará el pin específico, tipo (digital/PWM), valor y duración