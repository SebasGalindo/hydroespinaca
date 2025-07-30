#ifndef CONFIG_H
#define CONFIG_H

// Device Info
#define ESP32_ID "6883f3ba4f238"
#define FIRMWARE_VERSION "1.0.0"

// MQTT Topics
#define TOPIC_READINGS "hydroespinaca/readings"
#define TOPIC_COMMANDS "hydroespinaca/commands"
#define TOPIC_STATUS "hydroespinaca/status"

// Sensor Pins
#define DHT_PIN 4
#define PH_SENSOR_PIN 32
#define EC_SENSOR_PIN 33
#define WATER_LEVEL_PIN 34
#define LDR_PIN 35

// Actuator Pins
#define PUMP_PIN 25
#define LED_GROW_PIN 26
#define VALVE_NUTRIENTS_PIN 27
#define FAN_PIN 14

// Timing
#define READING_INTERVAL 60000  // 60 sec
#define RECONNECT_INTERVAL 5000 // 5 sec
#define DHT_TYPE DHT22

// Import secrets
#include "secrets.h"

#endif
