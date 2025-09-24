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

// Job Scheduler - Generic pin controller
#define MAX_CHANNELS 2
#define MAX_JOBS_PER_CHANNEL 10
#define STEP_TIMEOUT_TOLERANCE 500  // 500ms tolerance

// Humidifier Configuration (specialized control)
#define HUMID_WARMUP_TIME 2000      // 2 seconds warmup before pulses
#define HUMID_PULSE_ON_TIME 2000    // 2 seconds pulse ON
#define HUMID_PULSE_INTERVAL 30000  // 30 seconds interval between pulses

// Import secrets
#include "secrets.h"

#endif
