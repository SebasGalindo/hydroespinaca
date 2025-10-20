#pragma once

#include <Arduino.h>
#include "pins.h"

// ========================================
// CONTROLADOR BÁSICO DE ACTUADORES
// ========================================
// Funciones simples ON/OFF para cada actuador
// Lógica de relés: ACTIVO LOW (HIGH=OFF, LOW=ON)

class ActuatorController {
public:
    // Inicialización
    static void begin();
    
    // ========================================
    // RELÉS BÁSICOS (Digital ON/OFF)
    // ========================================
    
    // Calefactor del ambiente
    static void turnHeaterOn();
    static void turnHeaterOff();
    static bool isHeaterOn();
    
    // Luz colores / bombillo
    static void turnLightOn();
    static void turnLightOff();
    static bool isLightOn();
    
    // Piedra difusora (aire)
    static void turnAirStoneOn();
    static void turnAirStoneOff();
    static bool isAirStoneOn();
    
    // Bomba de agua
    static void turnWaterPumpOn();
    static void turnWaterPumpOff();
    static bool isWaterPumpOn();
    
    // Calentador de agua
    static void turnWaterHeaterOn();
    static void turnWaterHeaterOff();
    static bool isWaterHeaterOn();
    
    // ========================================
    // VENTILADOR (PWM + Digital)
    // ========================================
    
    // Ventilador digital
    static void turnFanOn();
    static void turnFanOff();
    static bool isFanOn();
    
    // Ventilador PWM (0-100%)
    static void setFanSpeed(int speedPercent);
    static int getFanSpeed();
    
    // ========================================
    // HUMIDIFICADOR ULTRASÓNICO
    // ========================================
    // PIN 13: Generador de niebla (activo en bajo)
    // PIN 14: Ventilador interno (activo en bajo)
    
    // Control directo de pines (usado por control.cpp)
    static void turnHumidifierMasterOn();   // PIN 14 ON (ventilador)
    static void turnHumidifierMasterOff();  // PIN 14 OFF (ventilador)
    static void turnHumidifierRelayOn();    // PIN 13 ON (generador niebla)
    static void turnHumidifierRelayOff();   // PIN 13 OFF (generador niebla)
    
    // ========================================
    // UTILIDADES
    // ========================================
    
    // Apagar todos los actuadores (emergencia)
    static void emergencyStop();
    
    // Estado de todos los actuadores
    static void printStatus();

private:
    static int currentFanSpeed;
    static bool fanPwmAttached;
    
    // Helpers internos
    static void attachFanPWM();
    static void detachFanPWM();
};