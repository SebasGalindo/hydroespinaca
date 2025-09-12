#include "thread_safe_logger.h"
#include <stdarg.h>

QueueHandle_t ThreadSafeLogger::logQueue = nullptr;
TaskHandle_t ThreadSafeLogger::loggerTask = nullptr;

void ThreadSafeLogger::begin() {
    // Create log queue
    logQueue = xQueueCreate(LOG_QUEUE_SIZE, sizeof(LogMessage));
    if (logQueue == nullptr) {
        Serial.println("❌ Error creating log queue");
        return;
    }
    
    // Create logger task
    xTaskCreate(
        loggerTaskFunction,
        "LoggerTask",
        4096,  // Stack size
        nullptr,
        1,     // Priority
        &loggerTask
    );
    
    Serial.println("✅ ThreadSafeLogger initialized");
}

void ThreadSafeLogger::logf(LogLevel level, const char* format, ...) {
    if (logQueue == nullptr) {
        // Fallback to Serial if logger not initialized
        va_list args;
        va_start(args, format);
        Serial.printf("[%s] ", levelToPrefix(level));
        Serial.printf(format, args);
        Serial.println();
        va_end(args);
        return;
    }
    
    LogMessage msg;
    msg.level = level;
    msg.timestamp = millis();
    
    va_list args;
    va_start(args, format);
    vsnprintf(msg.message, LOG_MSG_MAX_SIZE - 1, format, args);
    va_end(args);
    
    // Non-blocking queue send to avoid blocking tasks
    if (xQueueSend(logQueue, &msg, 0) != pdTRUE) {
        // Queue full, drop message (could add overflow counter here)
    }
}

void ThreadSafeLogger::debug(const char* format, ...) {
    va_list args;
    va_start(args, format);
    char buffer[LOG_MSG_MAX_SIZE];
    vsnprintf(buffer, LOG_MSG_MAX_SIZE - 1, format, args);
    va_end(args);
    logf(LOG_DEBUG, "%s", buffer);
}

void ThreadSafeLogger::info(const char* format, ...) {
    va_list args;
    va_start(args, format);
    char buffer[LOG_MSG_MAX_SIZE];
    vsnprintf(buffer, LOG_MSG_MAX_SIZE - 1, format, args);
    va_end(args);
    logf(LOG_INFO, "%s", buffer);
}

void ThreadSafeLogger::warning(const char* format, ...) {
    va_list args;
    va_start(args, format);
    char buffer[LOG_MSG_MAX_SIZE];
    vsnprintf(buffer, LOG_MSG_MAX_SIZE - 1, format, args);
    va_end(args);
    logf(LOG_WARNING, "%s", buffer);
}

void ThreadSafeLogger::error(const char* format, ...) {
    va_list args;
    va_start(args, format);
    char buffer[LOG_MSG_MAX_SIZE];
    vsnprintf(buffer, LOG_MSG_MAX_SIZE - 1, format, args);
    va_end(args);
    logf(LOG_ERROR, "%s", buffer);
}

void ThreadSafeLogger::loggerTaskFunction(void* parameter) {
    LogMessage msg;
    
    while (true) {
        // Wait for log message
        if (xQueueReceive(logQueue, &msg, portMAX_DELAY) == pdTRUE) {
            // Print timestamp and level
            Serial.printf("[%lu] %s %s\n", 
                         msg.timestamp, 
                         levelToPrefix(msg.level), 
                         msg.message);
        }
    }
}

const char* ThreadSafeLogger::levelToString(LogLevel level) {
    switch (level) {
        case LOG_DEBUG: return "DEBUG";
        case LOG_INFO: return "INFO";
        case LOG_WARNING: return "WARNING";
        case LOG_ERROR: return "ERROR";
        default: return "UNKNOWN";
    }
}

const char* ThreadSafeLogger::levelToPrefix(LogLevel level) {
    switch (level) {
        case LOG_DEBUG: return "🔍";
        case LOG_INFO: return "ℹ️";
        case LOG_WARNING: return "⚠️";
        case LOG_ERROR: return "❌";
        default: return "❓";
    }
}