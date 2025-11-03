#pragma once

// ========================================
// PINES DE SENSORES
// ========================================

// Sensores básicos
#define PIN_DHT22        4      // DHT22 Temperatura & Humedad (GPIO4)

// I2C para sensores (TCS34725, etc.)
#define PIN_I2C_SDA      21     // I2C SDA
#define PIN_I2C_SCL      22     // I2C SCL

// Sensores ADC
#define PIN_PH_ADC       32     // Sensor pH (usa divisor 1.5k + 2k, entrada ADC)
#define PIN_TDS_ADC      33     // Conductividad eléctrica (EC en mS/cm)
#define PIN_NTC_TANK     34     // NTC 10K tanque (entrada ADC)

// Sensor ultrasónico
#define PIN_ULTRA_TRIG   26     // Ultrasonido TRIG
#define PIN_ULTRA_ECHO   5      // Ultrasonido ECHO (usar divisor)

// Constantes de calibración para sensores
#define TANK_MAX_DISTANCE_CM 12.48f  // Límite del tanque en cm
#define ADC_RESOLUTION   4096.0f  // 12-bit ADC
#define ADC_VREF        3.3f      // Voltaje de referencia

// TDS Sensor constants
#define TDS_SAMPLE_COUNT 30      // Buffer size for TDS averaging

// ========================================
// PINES DE ACTUADORES / RELÉS
// ========================================

// Relés básicos (ACTIVO LOW - lógica invertida)
#define PIN_RELAY_HEATER 17     // Relé calefactor
#define PIN_LED_COLOUR   18     // Luz colores / bombillo
#define PIN_AIR_STONE    15     // Piedra difusora (salida)
#define PIN_WATER_PUMP   19     // Bomba de agua
#define PIN_FAN             27  // Ventiladores (puede ser PWM)
#define PIN_HEATER_WATER 25     // Calentador agua

// Humidificador ultrasónico (control con 2 relés ACTIVOS LOW)
#define PIN_HUMID_RELAY  13     // Generador de niebla ultrasónico (activo en bajo)
#define PIN_HUMID_POWER  14     // Ventilador interno del humidificador (activo en bajo)

// ========================================
// CONFIGURACIÓN DE RELÉS
// ========================================
// IMPORTANTE: Los relés son ACTIVO LOW (lógica invertida)
// - HIGH = Relé APAGADO
// - LOW = Relé ENCENDIDO