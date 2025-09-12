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
#ifdef DEBUG
            Serial.printf("🎯 Asignando canal LEDC %d para pin %d\n", ch, pin);
#endif
            return ch;
        }
    }
    
    Serial.printf("❌ ERROR: No hay canales LEDC libres para pin %d\n", pin);
    return -1;
}

void PWMManager::freeLedcChannelForPin(int pin) {
    if (pin < 0 || pin >= 40) return;
    
    int ch = pwmChannelOfPin[pin];
    if (ch >= 0 && ch < 8) {
        ledcChannelUsed[ch] = false;
        pinOfLedcChannel[ch] = -1;
        pwmChannelOfPin[pin] = -1;
#ifdef DEBUG
        Serial.printf("🔓 Liberando canal LEDC %d (era pin %d)\n", ch, pin);
#endif
    }
}

void PWMManager::ensureAttached(int pin) {
    if (pin < 0 || pin >= 40) {
        Serial.printf("❌ ERROR: Pin %d fuera de rango (0-39)\n", pin);
        return;
    }
    
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
#ifdef DEBUG
        Serial.printf("🔧 PWM configurado - Pin %d → Canal LEDC %d\n", pin, ch);
#endif
    }
    taskEXIT_CRITICAL(&pwmMux);
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
    
    taskENTER_CRITICAL(&pwmMux);
    if (pwmAttached[pin]) {
        int ch = pwmChannelOfPin[pin];
        if (ch >= 0 && ch < 8) {
            ledcWrite(ch, 0);
            ledcDetachPin(pin);
            Serial.printf("🧹 PWM detach - Pin %d (canal %d)\n", pin, ch);
        }
        pwmAttached[pin] = false;
        freeLedcChannelForPin(pin);
    }
    taskEXIT_CRITICAL(&pwmMux);
}