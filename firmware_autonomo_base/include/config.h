#ifndef CONFIG_H
#define CONFIG_H

// Import pin definitions
#include "pins.h"

// Device Info
#define ESP32_ID "6883fff7b079309f3ba4f238"
#define FIRMWARE_VERSION "4.0.0"  // Autonomous firmware - basic actuator control
#define MQTT_CLIENT_ID "esp32-001"

// MQTT Topics (autonomous mode - only telemetry)
#define TOPIC_READINGS "sensor/readings"
#define TOPIC_STATUS "sensor/esp32-001/status"

// Timing
#define READING_INTERVAL 120000  // 120 sec
#define RECONNECT_INTERVAL 5000  // 5 sec
#define MQTT_BACKOFF_MAX 30000   // 30 sec max backoff
#define DHT_TYPE DHT22

// Import secrets
#include "secrets.h"

#endif
