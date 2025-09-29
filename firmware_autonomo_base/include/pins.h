#pragma once

// ========================================
// PINES DE SENSORES
// ========================================

// Sensores básicos
#define PIN_DHT22        4      // DHT22 Temperatura & Humedad (GPIO4)

// I2C para sensores (TCS34725, etc.)
#define PIN_I2C_SDA      21     // I2C SDA
#define PIN_I2C_SCL      22     // I2C SCL

// TCS34725 - Sensor de luz RGB (conectado vía I2C)
// Calcula LightIndex usando la fórmula:
// LightIndex = (0.5 * (R / (R+B+G+C))) + (0.5 * (B / (R+B+G+C)))

// Sensores ADC
#define PIN_PH_ADC       32     // Sensor pH (usa divisor 1.5k + 2k, entrada ADC)
#define PIN_TDS_ADC      33     // Conductividad eléctrica (EC en mS/cm)
#define PIN_NTC_TANK     34     // NTC 10K tanque (entrada ADC)

// Módulo de nivel de agua analógico
#define PIN_WATER_LEVEL_ADC  26     // Módulo nivel agua resistivo (ADC)

// Constantes de calibración para sensores
#define ADC_RESOLUTION   4096.0f  // 12-bit ADC
#define ADC_VREF        3.3f      // Voltaje de referencia

// Calibración módulo nivel de agua analógico
#define ADC_MIN_VALUE        427     // Valor ADC para nivel mínimo
#define ADC_MAX_VALUE       1828     // Valor ADC para nivel máximo
#define MIN_WATER_LEVEL_CM  0.63f    // Nivel mínimo en cm
#define MAX_WATER_LEVEL_CM  2.64f    // Nivel máximo en cm

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
#define PIN_FAN          27     // Ventiladores (puede ser PWM)
#define PIN_HEATER_WATER 25     // Calentador agua

// Humidificador (control con 2 relés)
#define PIN_HUMID_RELAY  13     // Relé pulso humidificador
#define PIN_HUMID_POWER  14     // Relé maestro (power)

// ========================================
// CONFIGURACIÓN DE RELÉS
// ========================================
// IMPORTANTE: Los relés son ACTIVO LOW (lógica invertida)
// - HIGH = Relé APAGADO
// - LOW = Relé ENCENDIDO