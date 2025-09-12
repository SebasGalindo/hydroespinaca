#ifndef CONFIG_H
#define CONFIG_H

// Build Configuration
// #define DEBUG  // Uncomment for debug logs (comment out for production)

// Device Info
#define ESP32_ID "6883fff7b079309f3ba4f238"
#define FIRMWARE_VERSION "2.0.0"
#define MQTT_CLIENT_ID "esp32-001"

// MQTT Topics
#define TOPIC_READINGS "sensor/readings"
#define TOPIC_JOB_SCHEDULE "actuator/job/schedule"
#define TOPIC_STATUS "sensor/esp32-001/status"
#define TOPIC_COMPLETIONS "actuator/routine/completions"
#define TOPIC_NOTIFICATIONS "actuator/routine/notifications"

// Sensor Pins
#define DHT_PIN 4
#define I2C_SDA_PIN 21
#define I2C_SCL_PIN 22
// Commented sensors (stubbed)
// #define TDS_SENSOR_PIN 32
// #define PH_SENSOR_PIN 33
// #define NTC_SENSOR_PIN 35
// #define ULTRASONIC_TRIG_PIN 14
// #define ULTRASONIC_ECHO_PIN 26


// Timing
#define READING_INTERVAL 120000  // 120 sec (optimized for stability)
#define RECONNECT_INTERVAL 5000 // 5 sec
#define MQTT_BACKOFF_MAX 30000  // 30 sec max backoff
#define DHT_TYPE DHT22

// Job Scheduler (optimized for memory/stability)
#define MAX_CHANNELS 2
#define MAX_JOBS_PER_CHANNEL 10
#define STEP_TIMEOUT_TOLERANCE 500  // 500ms tolerance

// Import secrets
#include "secrets.h"

#endif
