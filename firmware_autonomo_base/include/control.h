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

// ========================================
// NUEVA CONFIGURACIÓN DE VARIABLES DE CONTROL
// ========================================

// Temperatura aire (°C) - Actualizada según especificaciones
#define TEMP_MIN 18.0f
#define TEMP_MAX 24.0f
#define TEMP_HYSTERESIS 1.0f
#define TEMP_REST_TIME_MS (5 * 60 * 1000)  // 5 min reposo

// Humedad aire (%) - Actualizada según especificaciones
#define HUMIDITY_MIN 60.0f               // Cambiado de 50% a 60%
#define HUMIDITY_MAX 75.0f               // Cambiado de 70% a 75%
#define HUMIDITY_HYSTERESIS 3.0f         // Cambiado de 5% a 3%
#define HUMIDITY_REST_TIME_MS (3 * 60 * 1000)  // 3 min reposo (humidificador)

// Temperatura agua (°C) - Rango seguro para espinaca DWC
#define WATER_TEMP_MIN 18.0f             // Mínimo seguro para espinaca
#define WATER_TEMP_MAX 23.0f             // Máximo seguro para espinaca
#define WATER_TEMP_HYSTERESIS 1.0f
#define WATER_TEMP_REST_TIME_MS (5 * 60 * 1000)  // 5 min reposo

// pH - Solo monitoreo (sin control automático)
#define PH_MIN 6.0f                      // Cambiado de 5.5 a 6.0
#define PH_MAX 7.0f                      // Cambiado de 6.5 a 7.0
#define PH_HYSTERESIS 0.2f

// EC - Solo monitoreo (sin control automático)
#define EC_MIN 1.8f         
#define EC_MAX 2.3f         
#define EC_HYSTERESIS 0.2f

// Nivel de agua (cm) - Fail-safe para proteger bomba
#define WATER_LEVEL_MIN 7.0f             // Apagar bomba si nivel > 7cm (tanque vacío)
#define WATER_LEVEL_RECOVERY 6.0f        // Permitir bomba si nivel < 6cm (tanque lleno)
#define WATER_LEVEL_REST_TIME_MS (15 * 60 * 1000)  // 15 min reposo

// Luz - Control con máquina de estados avanzada
#define DARKNESS_THRESHOLD 3000         // TCS34725 Clear < 3000 = oscuridad total
#define LIGHT_ON_THRESHOLD   65.0f      // Encender si la calidad de luz está por debajo
#define LIGHT_OFF_THRESHOLD  67.0f      // Apagar si la calidad supera este valor
#define LIGHT_MIN_ON_MS      (15 * 60 * 1000UL) // Mínimo 15 min encendida antes de poder apagarse
#define LIGHT_MAX_ON_MS      (45 * 60 * 1000UL) // Máximo 45 min seguidos
#define LIGHT_REST_MS        (8 * 60 * 1000UL)  // Tiempo de descanso obligatorio

// Horario de luz (Colombia UTC-5) - Control activo con sensores robustos
#define LIGHT_START_HOUR 6   // 6:00 AM
#define LIGHT_END_HOUR 18    // 6:00 PM

// Tiempos mínimos de actuadores (ms)
#define HEATER_MIN_ON_TIME (10 * 60 * 1000)    // 10 minutos (aire)
#define WATER_HEATER_MIN_ON_TIME (5 * 60 * 1000)  // 5 minutos (agua)
#define FAN_MIN_ON_TIME (5 * 60 * 1000)        // 5 minutos
#define LIGHT_MIN_TIME (5 * 60 * 1000)         // 5 minutos
#define PUMP_MIN_ON_TIME (3 * 60 * 1000)       // 3 minutos mínimo bomba

// ========================================
// CONFIGURACIÓN DE RUTINAS PERIÓDICAS INDEPENDIENTES
// ========================================

// CRONOGRAMA DE RECIRCULACIÓN (cada 4h) - INDEPENDIENTE
#define PUMP_INTERVAL_S 14400                  // 4 horas = 14400 segundos
#define PUMP_DURATION_S 480                    // 8 minutos = 480 segundos
#define PUMP_INTERVAL_MINUTES 240              // 4 horas = 240 minutos

// CRONOGRAMA DE AIREACIÓN AUTÓNOMA (cada 30 min) - INDEPENDIENTE  
#define AIR_PERIODIC_INTERVAL_S 1800           // 30 minutos = 1800 segundos
#define AIR_PERIODIC_ON_S 300                  // 5 minutos = 300 segundos
#define AIR_PERIODIC_INTERVAL_MINUTES 30       // 30 minutos

// RUTINA ESPECIAL DE AIREACIÓN PARA RECIRCULACIÓN
#define AIR_LEAD_S 90                          // 90 segundos antes de motobomba
#define POST_AIR_HOLD_S 45                     // 45 segundos después de motobomba

// Conversiones a milisegundos para timers
#define PUMP_DURATION_MS (PUMP_DURATION_S * 1000)
#define AIR_PERIODIC_ON_MS (AIR_PERIODIC_ON_S * 1000)  
#define AIR_LEAD_MS (AIR_LEAD_S * 1000)
#define POST_AIR_HOLD_MS (POST_AIR_HOLD_S * 1000)

// NUEVA REGLA: Emergencia térmica (>25°C)
#define TEMP_EMERGENCY_THRESHOLD 25.0f     // 25°C activa emergencia
#define EMERGENCY_PUMP_DURATION_S 300      // 5 minutos de recirculación
#define EMERGENCY_PUMP_DURATION_MS (EMERGENCY_PUMP_DURATION_S * 1000)
#define EMERGENCY_MIN_INTERVAL_MS (10 * 60 * 1000)  // Mínimo 10min entre emergencias

// Configuración de promedios
#define SENSOR_READING_INTERVAL (2 * 60 * 1000)  // 2 minutos
#define CONTROL_CYCLE_INTERVAL (4 * 60 * 1000)   // 4 minutos
#define MAX_READINGS 2  // 2 lecturas para promedio

// ========================================
// ESTRUCTURAS DE DATOS
// ========================================

// NUEVA MÁQUINA DE ESTADOS PARA BOMBA DE AIRE
enum AirStoneMode {
    AIR_MODE_AUTONOMOUS,           // Modo autónomo: ciclo cada 30min
    AIR_MODE_RECIRCULATION,        // Modo recirculación: sigue ciclo de bomba
    AIR_MODE_IDLE                  // Modo inactivo: esperando próximo ciclo
};

// MÁQUINA DE ESTADOS PARA CONTROL DE LUZ ARTIFICIAL
enum LightState {
    LIGHT_OFF,                     // Apagada (fuera de horario o calidad suficiente)
    LIGHT_ON,                      // Encendida (complementando luz natural)
    LIGHT_RESTING                  // En descanso obligatorio (después de ciclo)
};

struct SensorReadings {
    float temperature;
    float humidity;
    float lightIndex;       // Fórmula C para calidad espectral
    uint16_t clearChannel;  // TCS34725 Clear channel para detección oscuridad  
    float waterLevel;       // Water level height in cm (tank_height - sensor_distance)
    float waterTemp;
    float ph;
    float tds;              // Actually EC in mS/cm (variable name kept for compatibility)
    bool valid;
    unsigned long timestamp;
};

// ========================================
// ESTRUCTURA DE CONTROL SIMPLIFICADA CON HISTÉRESIS Y REPOSO
// ========================================

struct VariableConfig {
    float minValue;           // Valor mínimo del rango óptimo
    float maxValue;           // Valor máximo del rango óptimo
    float hysteresis;         // Histéresis para evitar oscilaciones
    unsigned long restTimeMs; // Tiempo mínimo de reposo entre activaciones
    bool hasActuator;         // Si tiene actuadores asociados (pH/EC = false)
};

struct ActuatorState {
    bool isOn;                // Estado actual del actuador
    unsigned long lastChangeTime;    // Última vez que cambió de estado
    unsigned long lastRestTime;      // Última vez que se apagó (para reposo)
    unsigned long totalOnTime;       // Tiempo total encendido
    bool forceOff;           // Forzar apagado por seguridad (nivel agua)
};

struct ControlState {
    // Promedios de sensores
    SensorReadings readings[MAX_READINGS];
    int readingIndex;
    int readingCount;
    SensorReadings averageReadings;
    
    // NUEVA CONFIGURACIÓN DE VARIABLES DE CONTROL
    VariableConfig tempConfig;
    VariableConfig humidityConfig;
    VariableConfig waterTempConfig;
    VariableConfig phConfig;
    VariableConfig ecConfig;
    VariableConfig waterLevelConfig;
    
    // Estados de actuadores con debounce y reposo
    ActuatorState heater;         // Calefactor aire
    ActuatorState waterHeater;    // Calefactor agua
    ActuatorState fan;
    ActuatorState light;
    ActuatorState waterPump;
    ActuatorState airStone;
    ActuatorState humidifier;     // Humidificador con nueva estructura
    
    // Fail-safe por nivel de agua (waterLevelOk se maneja en checkWaterLevelSafety)
    unsigned long waterLevelLastCheck;  // Última verificación nivel
    
    // ========================================
    // CRONOGRAMA INDEPENDIENTE DE RECIRCULACIÓN (cada 4h)
    // ========================================
    unsigned long nextRecirculationTime;  // Próximo horario de recirculación
    bool recirculationActive;             // Ciclo completo de recirculación activo
    unsigned long recirculationStartTime; // Inicio del ciclo completo
    
    // Fases de la rutina especial de aireación para recirculación
    bool recircAirLeadActive;             // Aire 90s antes de bomba
    bool recircPumpActive;                // Bomba 8min con aire continuo
    bool recircAirPostActive;             // Aire 45s después de bomba
    
    unsigned long recircAirLeadStartTime; // Timestamp inicio lead air
    unsigned long recircPumpStartTime;    // Timestamp inicio bomba
    unsigned long recircAirPostStartTime; // Timestamp inicio post air
    
    // ========================================
    // CRONOGRAMA INDEPENDIENTE DE AIREACIÓN AUTÓNOMA (cada 30min)
    // ========================================
    unsigned long nextAutonomousAirTime; // Próximo horario de aireación autónoma
    bool autonomousAirActive;             // Ciclo de aireación autónoma activo
    unsigned long autonomousAirStartTime; // Timestamp inicio aireación autónoma
    
    // ========================================
    // EXCLUSIÓN MUTUA Y PRIORIDADES
    // ========================================
    AirStoneMode airStoneMode;            // Modo actual del sistema de aireación
    
    // NUEVA REGLA: Emergencia térmica 
    bool thermalEmergencyActive;          // Emergencia por >25°C activa
    unsigned long lastEmergencyTime;      // Último timestamp de emergencia
    unsigned long emergencyStartTime;     // Inicio de recirculación de emergencia
    
    // Control de temperatura agua (independiente de bomba)
    unsigned long waterHeaterStartTime;
    
    // NUEVA MÁQUINA DE ESTADOS PARA CONTROL DE LUZ
    LightState lightState;                 // Estado actual de la máquina de estados
    unsigned long lightStateStartTime;     // Timestamp del inicio del estado actual
    unsigned long lightOnStartTime;        // Timestamp cuando se encendió la luz
    unsigned long lightRestStartTime;      // Timestamp cuando empezó el descanso
    float lightQualityAtChange;            // Calidad de luz en el último cambio de estado
    
    // HUMIDIFICADOR: Control simplificado con secuencia inmediata
    bool humidifierMasterActive;         // Relé maestro (PIN 14) activo
    unsigned long humidifierStartTime;   // Tiempo de inicio del ciclo
    
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
    
    // ========================================
    // RUTINAS PERIÓDICAS INDEPENDIENTES
    // ========================================
    void runIndependentRoutines();        // Nueva función principal de rutinas
    
    // Cronograma de recirculación (cada 4h, independiente)
    void initializeRecirculationSchedule();
    void handleRecirculationRoutine();
    bool isRecirculationScheduleTime();
    
    // Cronograma de aireación autónoma (cada 30min, independiente)
    void initializeAutonomousAirSchedule();
    void handleAutonomousAirRoutine();
    bool isAutonomousAirScheduleTime();
    
    // Exclusión mutua y control de prioridades
    void updateAirStoneControl();         // Controlador central de aireación
    void setAirStoneMode(AirStoneMode newMode, const char* reason);
    
    // Funciones auxiliares
    unsigned long getCurrentMinutes();
    void handleThermalEmergency();        // Nueva emergencia térmica
    void handleLightStateMachine();       // Nueva máquina de estados de luz
    void logRoutineAction(const char* routine, const char* action);
    void logAirStoneAction(const char* action, const char* mode, const char* reason);
    
    // FUNCIONES DE CONTROL SIMPLIFICADAS CON HISTÉRESIS Y REPOSO
    void initializeVariableConfigs();     // Inicializar configuraciones
    bool canActivateAfterRest(const ActuatorState& actuator, unsigned long restTime);
    bool controlGenericVariable(float sensorValue, const VariableConfig& config, ActuatorState& actuator, const char* name);
    void logVariableStatus(const char* name, float value, const VariableConfig& config, bool inRange);
    
    // Utilidades mejoradas
    bool isLightScheduleActive();
    bool hasMinimumTimePassed(const ActuatorState& state, unsigned long minTime);
    void setActuatorState(ActuatorState& state, bool newState, const char* name);
    void setActuatorStateImmediate(ActuatorState& state, bool newState, const char* name);
    void applyActuatorStates();
    void logControlDecision(const char* sensor, float value, const char* action);
    void logSafetyAction(const char* reason);
    
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