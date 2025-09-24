#pragma once

#include <Arduino.h>
#include <NTPClient.h>
#include "sensors.h"
#include "actuators.h"

// ========================================
// CONFIGURACIÓN DE CONTROL AUTÓNOMO
// ========================================

// ========================================
// UMBRALES ÓPTIMOS PARA CONTROL AUTÓNOMO
// (Solo rangos óptimos, sin validaciones físicas)
// ========================================

// Temperatura aire (°C) - Óptimo para espinaca DWC
#define TEMP_MIN 16.0f
#define TEMP_MAX 24.0f
#define TEMP_HYSTERESIS 1.0f

// Humedad aire (%) - Óptimo para espinaca DWC  
#define HUMIDITY_MIN 70.0f
#define HUMIDITY_MAX 80.0f
#define HUMIDITY_HYSTERESIS 5.0f

// Luz (%) - Óptimo para espinaca DWC
#define LIGHT_MIN 40.0f
#define LIGHT_MAX 60.0f
#define LIGHT_HYSTERESIS 10.0f

// Horario de luz (Colombia UTC-5)
#define LIGHT_START_HOUR 6   // 6:00 AM
#define LIGHT_END_HOUR 18    // 6:00 PM

// Temperatura agua (°C) - Óptimo para espinaca DWC
#define WATER_TEMP_MIN 18.0f
#define WATER_TEMP_MAX 23.0f
#define WATER_TEMP_HYSTERESIS 1.0f

// pH - Óptimo para espinaca DWC
#define PH_MIN 5.5f
#define PH_MAX 6.5f
#define PH_HYSTERESIS 0.2f

// TDS/EC (ppm) - Óptimo para espinaca DWC  
#define TDS_MIN 560.0f      // ~1 ms/cm
#define TDS_MAX 1120.0f     // ~2 ms/cm
#define TDS_HYSTERESIS 100.0f

// Nivel de agua crítico (cm) - Seguridad bomba/difusor
#define WATER_LEVEL_MIN 9.0f

// Tiempos mínimos de actuadores (ms)
#define HEATER_MIN_ON_TIME (10 * 60 * 1000)    // 10 minutos (aire)
#define WATER_HEATER_MIN_ON_TIME (5 * 60 * 1000)  // 5 minutos (agua)
#define FAN_MIN_ON_TIME (5 * 60 * 1000)        // 5 minutos
#define LIGHT_MIN_TIME (5 * 60 * 1000)         // 5 minutos
#define PUMP_MIN_ON_TIME (3 * 60 * 1000)       // 3 minutos mínimo para recirculación
#define PUMP_CYCLE_TIME (4 * 60 * 60 * 1000)   // 4 horas
#define PUMP_ON_TIME (2 * 60 * 1000)           // 2 minutos
#define AIRSTONE_CYCLE_TIME (60 * 60 * 1000)   // 1 hora
#define AIRSTONE_ON_TIME (3 * 60 * 1000)       // 3 minutos
#define AIRSTONE_PUMP_LEAD (1 * 60 * 1000)     // 1 minuto antes
#define AIRSTONE_PUMP_FOLLOW (2 * 60 * 1000)   // 2 minutos después

// Configuración de promedios
#define SENSOR_READING_INTERVAL (2 * 60 * 1000)  // 2 minutos
#define CONTROL_CYCLE_INTERVAL (4 * 60 * 1000)   // 4 minutos
#define MAX_READINGS 2  // 2 lecturas para promedio

// ========================================
// ESTRUCTURAS DE DATOS
// ========================================

struct SensorReadings {
    float temperature;
    float humidity;
    float lightIndex;
    float waterLevel;
    float waterTemp;
    float ph;
    float tds;
    bool valid;
    unsigned long timestamp;
};

struct ActuatorState {
    bool isOn;
    unsigned long lastChangeTime;
    unsigned long totalOnTime;
    bool forceOff;  // Para seguridad (ej. bajo nivel agua)
};

struct ControlState {
    // Promedios de sensores
    SensorReadings readings[MAX_READINGS];
    int readingIndex;
    int readingCount;
    SensorReadings averageReadings;
    
    // Estados de actuadores
    ActuatorState heater;         // Calefactor aire
    ActuatorState waterHeater;    // Calefactor agua
    ActuatorState fan;
    ActuatorState light;
    ActuatorState humidifier;
    ActuatorState waterPump;
    ActuatorState airStone;
    
    // Rutinas periódicas
    unsigned long lastPumpCycle;
    unsigned long lastAirStoneCycle;
    unsigned long pumpStartTime;
    unsigned long airStoneStartTime;
    
    // Control de temperatura agua
    unsigned long waterHeaterStartTime;
    bool pumpRunningForHeater;    // Bomba activa por calefactor agua
    unsigned long heaterPumpStartTime;
    
    // Sistema
    bool waterLevelOk;
    bool systemEnabled;
    unsigned long lastControlCycle;
};

// ========================================
// CLASE CONTROLADOR AUTÓNOMO
// ========================================

class AutonomousController {
private:
    SensorManager* sensors;
    NTPClient* timeClient;
    ControlState state;
    
    // Funciones internas
    void updateSensorReadings();
    void calculateAverages();
    bool shouldExecuteControl();
    
    // Reglas de control
    void checkWaterLevelSafety();
    void controlTemperature();
    void controlWaterTemperature();
    void controlHumidity();
    void controlLight();
    void runPeriodicRoutines();
    
    // Utilidades
    bool isLightScheduleActive();
    bool hasMinimumTimePassed(const ActuatorState& state, unsigned long minTime);
    void setActuatorState(ActuatorState& state, bool newState, const char* name);
    void applyActuatorStates();
    void logControlDecision(const char* sensor, float value, const char* action);
    void logSafetyAction(const char* reason);
    void logRoutineAction(const char* routine, const char* action);
    
public:
    AutonomousController(SensorManager* sensorManager, NTPClient* ntpClient);
    
    void begin();
    void loop();
    
    // Estado del sistema
    bool isWaterLevelOk() const { return state.waterLevelOk; }
    bool isSystemEnabled() const { return state.systemEnabled; }
    
    // Diagnóstico
    void printSystemStatus();
    void printSensorAverages();
    void printActuatorStates();
};