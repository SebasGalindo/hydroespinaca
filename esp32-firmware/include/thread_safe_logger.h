#pragma once

#include <Arduino.h>
#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <freertos/queue.h>

#define LOG_QUEUE_SIZE 32
#define LOG_MSG_MAX_SIZE 256

enum LogLevel {
    LOG_DEBUG,
    LOG_INFO,
    LOG_WARNING,
    LOG_ERROR
};

struct LogMessage {
    LogLevel level;
    unsigned long timestamp;
    char message[LOG_MSG_MAX_SIZE];
    
    LogMessage() : level(LOG_INFO), timestamp(0) {
        memset(message, 0, LOG_MSG_MAX_SIZE);
    }
};

class ThreadSafeLogger {
public:
    static void begin();
    static void logf(LogLevel level, const char* format, ...);
    static void debug(const char* format, ...);
    static void info(const char* format, ...);
    static void warning(const char* format, ...);
    static void error(const char* format, ...);
    
private:
    static QueueHandle_t logQueue;
    static TaskHandle_t loggerTask;
    static void loggerTaskFunction(void* parameter);
    static const char* levelToString(LogLevel level);
    static const char* levelToPrefix(LogLevel level);
};