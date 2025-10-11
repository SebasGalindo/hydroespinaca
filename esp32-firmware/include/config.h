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
#define TOPIC_NOTIFICATIONS "actuator/routine/notifications"

// Timing
#define READING_INTERVAL 120000  // 120 sec
#define RECONNECT_INTERVAL 5000  // 5 sec
#define MQTT_BACKOFF_MAX 30000   // 30 sec max backoff
#define DHT_TYPE DHT22

// Job Scheduler - Sequential execution (simplified)
// Backend manages concurrency via pin-locking. Firmware processes jobs one at a time.
#define STEP_TIMEOUT_TOLERANCE 500  // 500ms tolerance

// Humidifier Configuration (specialized control via actuator-service)
// Note: Humidifier routine now simulates button press (based on autonomous firmware)
// - PIN 14 (power relay): Controls main power
// - PIN 13 (pulse relay): Simulates button press (1-second pulse)

// Import secrets
#include "secrets.h"

#endif
