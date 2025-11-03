#include "pwm_manager.h"

// Static member definitions
portMUX_TYPE PWMManager::pwmMux = portMUX_INITIALIZER_UNLOCKED;
bool PWMManager::pwmAttached[40] = {false};
int PWMManager::pwmChannelOfPin[40] = {-1};
bool PWMManager::ledcChannelUsed[8] = {false};
int PWMManager::pinOfLedcChannel[8] = {-1};

void PWMManager::initialize() {
    for (int i = 0; i < 40; i++) {
        pwmAttached[i] = false;
        pwmChannelOfPin[i] = -1;
    }
    for (int i = 0; i < 8; i++) {
        ledcChannelUsed[i] = false;
        pinOfLedcChannel[i] = -1;
    }
}

int PWMManager::allocateLedcChannelForPin(int pin) {
    if (pwmChannelOfPin[pin] >= 0) {
        return pwmChannelOfPin[pin];
    }

    for (int ch = 0; ch < 8; ch++) {
        if (!ledcChannelUsed[ch]) {
            ledcChannelUsed[ch] = true;
            pinOfLedcChannel[ch] = pin;
            pwmChannelOfPin[pin] = ch;
            // NOTE: No Serial.printf here - called from critical section
            return ch;
        }
    }

    // NOTE: No Serial.printf here - called from critical section
    return -1;
}

void PWMManager::freeLedcChannelForPin(int pin) {
    if (pin < 0 || pin >= 40) return;

    int ch = pwmChannelOfPin[pin];
    if (ch >= 0 && ch < 8) {
        ledcChannelUsed[ch] = false;
        pinOfLedcChannel[ch] = -1;
        pwmChannelOfPin[pin] = -1;
        // NOTE: No Serial.printf here - called from critical section
    }
}

void PWMManager::ensureAttached(int pin) {
    if (pin < 0 || pin >= 40) {
        Serial.printf("❌ ERROR: Pin %d fuera de rango (0-39)\n", pin);
        return;
    }

    int attachedChannel = -1;
    bool wasAttached = false;

    taskENTER_CRITICAL(&pwmMux);
    if (!pwmAttached[pin]) {
        int ch = pwmChannelOfPin[pin];
        if (ch < 0) {
            ch = allocateLedcChannelForPin(pin);
            pwmChannelOfPin[pin] = ch;
        }
        ledcSetup(ch, 5000, 8);
        ledcAttachPin(pin, ch);
        pwmAttached[pin] = true;
        attachedChannel = ch;  // Save for logging outside critical section
        wasAttached = true;
    }
    taskEXIT_CRITICAL(&pwmMux);

    // Log OUTSIDE critical section to avoid watchdog timeout
#ifdef DEBUG
    if (wasAttached && attachedChannel >= 0) {
        Serial.printf("🔧 PWM configurado - Pin %d → Canal LEDC %d\n", pin, attachedChannel);
    }
#endif
}

void PWMManager::writeDuty(int pin, int duty) {
    if (pin < 0 || pin >= 40) {
        Serial.printf("❌ ERROR: Pin %d fuera de rango (0-39) para PWM\n", pin);
        return;
    }
    
    ensureAttached(pin);
    int channel = pwmChannelOfPin[pin];
    
    if (channel < 0 || channel >= 8) {
        Serial.printf("❌ ERROR: Canal LEDC %d inválido para pin %d\n", channel, pin);
        return;
    }
    
    taskENTER_CRITICAL(&pwmMux);
    ledcWrite(channel, constrain(duty, 0, 255));
    taskEXIT_CRITICAL(&pwmMux);
    Serial.printf("⚡ PWM Pin %d: duty=%d (canal %d)\n", pin, duty, channel);
}

void PWMManager::detachIfAttached(int pin) {
    if (pin < 0 || pin >= 40) return;

    int detachedChannel = -1;

    taskENTER_CRITICAL(&pwmMux);
    if (pwmAttached[pin]) {
        int ch = pwmChannelOfPin[pin];
        if (ch >= 0 && ch < 8) {
            ledcWrite(ch, 0);
            ledcDetachPin(pin);
            detachedChannel = ch;  // Save for logging outside critical section
        }
        pwmAttached[pin] = false;
        freeLedcChannelForPin(pin);
    }
    taskEXIT_CRITICAL(&pwmMux);

    // Log OUTSIDE critical section to avoid watchdog timeout
    if (detachedChannel >= 0) {
        Serial.printf("🧹 PWM detach - Pin %d (canal %d)\n", pin, detachedChannel);
    }
}