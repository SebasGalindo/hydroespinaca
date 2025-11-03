#ifndef CONFIG_H
#define CONFIG_H

// Import pin definitions
#include "pins.h"

// Device Info
#define ESP32_ID "6883fff7b079309f3ba4f238"
#define FIRMWARE_VERSION "3.0.0"  // Simplified firmware - sensors only + dynamic actuators
#define MQTT_CLIENT_ID "esp32-001"

// MQTT Topics
#define TOPIC_READINGS "sensor/readings"
#define TOPIC_JOB_SCHEDULE "actuator/job/schedule"
#define TOPIC_STATUS "sensor/esp32-001/status"
#define TOPIC_COMPLETIONS "actuator/routine/completions"

// Timing
#define READING_INTERVAL 120000  // 120 sec
#define RECONNECT_INTERVAL 5000  // 5 sec
#define MQTT_BACKOFF_MAX 30000   // 30 sec max backoff
#define DHT_TYPE DHT22

// Job Scheduler - Sequential execution (simplified)
// Backend manages concurrency via pin-locking. Firmware processes jobs one at a time.
#define STEP_TIMEOUT_TOLERANCE 500  // 500ms tolerance

// 🔒 SAFETY: Maximum absolute duration and cooldown (prevents infinite extensions)
#define MAX_ABSOLUTE_STEP_DURATION (2UL * 60UL * 60UL * 1000UL)  // 2 hours max lifetime
#define PIN_COOLDOWN_DURATION (15UL * 60UL * 1000UL)             // 15 minutes cooldown after forced shutdown

// ========================================
// CONFIGURACIÓN NTC TERMISTOR
// ========================================
// 🔧 AJUSTAR SEGÚN TU HARDWARE

// Valor BETA del termistor (común: 3435, 3950, 4250)
// Verificar en la hoja de datos del NTC
#define NTC_BETA 3950

// Resistencia nominal del NTC a 25°C (común: 10kΩ, 50kΩ, 100kΩ)
#define NTC_NOMINAL_RESISTANCE 10000.0  // 10kΩ

// Temperatura nominal (casi siempre 25°C)
#define NTC_NOMINAL_TEMP 25.0

// Resistencia fija en el divisor de voltaje (medir con multímetro)
#define NTC_R_FIXED 10000.0  // 10kΩ

// Configuración del circuito (descomenta solo UNA opción)
// OPCIÓN A: VCC ──R_FIXED── ADC ──NTC── GND (más común)
// #define NTC_CIRCUIT_A
// OPCIÓN B: VCC ──NTC── ADC ──R_FIXED── GND (menos común)
#define NTC_CIRCUIT_B

// Humidifier Configuration (specialized control via actuator-service)
// Note: Humidifier routine now simulates button press (based on autonomous firmware)
// - PIN 14 (power relay): Controls main power
// - PIN 13 (pulse relay): Simulates button press (1-second pulse)

// Import secrets
#include "secrets.h"

#endif
