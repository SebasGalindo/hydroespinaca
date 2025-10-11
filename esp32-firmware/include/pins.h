#pragma once

// ========================================
// SOLO SENSORES - Actuadores controlados dinámicamente por backend
// ========================================

// Sensores básicos
#define PIN_DHT22        4

// I2C para sensores (BH1750, TCS34725, etc.)
#define PIN_I2C_SDA      21
#define PIN_I2C_SCL      22

// TCS34725 - Sensor de luz RGB (conectado vía I2C)
// Calcula LightIndex usando la fórmula:
// LightIndex = (0.5 * (R / (R+B+G+C))) + (0.5 * (B / (R+B+G+C)))
// Este índice combina las componentes roja y azul del espectro
// para obtener un valor de 0-100 que representa la calidad de luz

// Sensores ADC
#define PIN_PH_ADC       32
#define PIN_TDS_ADC      33
#define PIN_NTC_TANK     34

// Sensor ultrasónico
#define PIN_ULTRA_TRIG   26
#define PIN_ULTRA_ECHO   5

// Constantes de calibración para sensores
#define TANK_MAX_DISTANCE_CM 12.48f  // Límite del tanque en cm
#define ADC_RESOLUTION   4096.0f  // 12-bit ADC
#define ADC_VREF        3.3f      // Voltaje de referencia

// ========================================
// EXCEPCIÓN: Actuadores con monitoreo especializado
// ========================================
// Actuadores hardcodeados que requieren monitoreo de sensores

// Humidificador (lógica de 2 relés)
#define PIN_HUMID_RELAY  13     // Relé pulso humidificador
#define PIN_HUMID_POWER  14     // Relé maestro (power)

// Calefactor de ambiente (monitoreo de temperatura)
#define PIN_RELAY_HEATER 17     // Relé del calefactor con monitoreo térmico

// NOTA: Todos los demás actuadores NO están definidos aquí
// El backend enviará el pin específico, tipo (digital/PWM), valor y duración