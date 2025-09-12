#pragma once

#include <Arduino.h>
#include <freertos/FreeRTOS.h>
#include <freertos/task.h>

class PWMManager {
public:
    static void ensureAttached(int pin);
    static void writeDuty(int pin, int duty);
    static void detachIfAttached(int pin);
    static void initialize();

private:
    static portMUX_TYPE pwmMux;
    static bool pwmAttached[40];
    static int pwmChannelOfPin[40];
    static bool ledcChannelUsed[8];
    static int pinOfLedcChannel[8];
    
    static int allocateLedcChannelForPin(int pin);
    static void freeLedcChannelForPin(int pin);
};