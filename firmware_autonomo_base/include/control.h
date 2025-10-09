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

// Temperatura aire (°C) - Configuración de seguridad para calefactor
#define TEMP_MIN 18.0f
#define TEMP_MAX 24.0f
#define TEMP_HYSTERESIS 1.0f
#define TEMP_REST_TIME_MS (5 * 60 * 1000)  // 5 min reposo

// ========================================
// PARCHE DE SEGURIDAD DEL CALEFACTOR
// ========================================
// Umbrales específicos para monitoreo rápido del calefactor
#define HEATER_ON_TEMP       17.0f          // Enciende ≤ 17.0°C
#define HEATER_OFF_TEMP      22.5f          // Apaga ≥ 22.5°C (control normal)
#define HEATER_EMERGENCY_TEMP 25.0f         // Apaga inmediatamente ≥ 25.0°C
#define HEATER_MIN_ON_TIME   (3 * 60 * 1000)  // 3 minutos mínimo encendido
#define HEATER_MIN_OFF_TIME  (2 * 60 * 1000)  // 2 minutos mínimo apagado
#define HEATER_FAST_MONITOR_INTERVAL 3000   // 3 segundos de monitoreo rápido
#define HEATER_TEMP_BUFFER_SIZE 3            // Media móvil de 3 lecturas

// Humedad aire (%) - Actualizada según especificaciones
#define HUMIDITY_MIN 60.0f               // Cambiado de 50% a 60%
#define HUMIDITY_MAX 75.0f               // Cambiado de 70% a 75%
#define HUMIDITY_HYSTERESIS 3.0f         // Cambiado de 5% a 3%
#define HUMIDITY_REST_TIME_MS (3 * 60 * 1000)  // 3 min reposo (humidificador)

// ========================================
// VALIDACIÓN DE EFECTIVIDAD DEL HUMIDIFICADOR
// ========================================
// Protección contra falta de agua o módulo defectuoso
#define HUMID_WINDOW_MINUTES 5                          // Ventana de evaluación (5 min)
#define HUMID_MIN_DELTA 1.5f                            // Incremento mínimo de HR (1.5%)
#define HUMID_TEMP_RESET_THRESHOLD 2.0f                 // Si temp sube >2°C, reiniciar ventana
#define HUMID_LOCKOUT_DURATION_MS (2 * 60 * 60 * 1000) // Bloqueo de 2 horas
#define HUMID_MAX_ON_TIME_MS (9 * 60 * 1000)           // Máximo 9 min encendido continuo
#define HUMID_COOLDOWN_MS (2 * 60 * 1000)              // Cooldown 2 min después de max time

// Temperatura agua (°C) - Rango seguro para espinaca DWC
#define WATER_TEMP_MIN 18.0f             // Mínimo seguro para espinaca
#define WATER_TEMP_MAX 23.0f             // Máximo seguro para espinaca
#define WATER_TEMP_HYSTERESIS 1.0f
#define WATER_TEMP_REST_TIME_MS (5 * 60 * 1000)  // 5 min reposo

// ========================================
// RECIRCULACIÓN EXTRA POR CALEFACTOR DE AGUA
// ========================================
// Cuando el calefactor se apaga por alcanzar WATER_TEMP_MAX,
// se dispara una recirculación extra para homogeneizar temperatura
#define EXTRA_RECIRCULATION_DURATION_MS (5 * 60 * 1000)  // 5 minutos (actualizado de 3)
#define EXTRA_RECIRCULATION_COOLDOWN_MS (60 * 60 * 1000) // 1 hora (cooldown)
#define SAFE_WINDOW_MS (15 * 60 * 1000)                  // 15 minutos ventana seguridad

// pH - Solo monitoreo (sin control automático)
#define PH_MIN 6.0f                      // Cambiado de 5.5 a 6.0
#define PH_MAX 7.0f                      // Cambiado de 6.5 a 7.0
#define PH_HYSTERESIS 0.2f

// EC - Solo monitoreo (sin control automático)
#define EC_MIN 1.8f         
#define EC_MAX 2.3f         
#define EC_HYSTERESIS 0.2f

// Nivel de agua (cm) - Fail-safe para proteger bomba
// IMPORTANTE: Se usa DISTANCIA medida por sensor (no nivel de agua)
// Tanque altura = 40cm, sensor en la parte superior
#define WATER_DISTANCE_MAX 33.0f         // Apagar bomba si distancia > 33cm (tanque vacío - nivel < 7cm)
#define WATER_DISTANCE_RECOVERY 34.0f    // Permitir bomba si distancia < 34cm (tanque lleno - nivel > 6cm)
#define WATER_LEVEL_REST_TIME_MS (15 * 60 * 1000)  // 15 min reposo

// Luz - Control simplificado con BH1750
#define LUX_ON_THRESHOLD    10000.0f    // Si lux < 10000, encender luz artificial
#define LUX_OFF_THRESHOLD   12000.0f    // Si lux > 12000, apagar luz artificial
#define LIGHT_MIN_ON_MS     (30 * 60 * 1000UL) // Mínimo 30 min encendida (evita parpadeos)
#define REQUIRED_LIGHT_HOURS 14         // Fotoperiodo diario total (5 a.m. – 7 p.m.)

// Horario de luz (Colombia UTC-5) - 14 horas de fotoperiodo
#define LIGHT_START_HOUR 5   // 5:00 AM
#define LIGHT_END_HOUR 19    // 7:00 PM (19:00)

// Tiempos mínimos de actuadores (ms)
// NOTA: HEATER_MIN_ON_TIME ya definido en línea 34 como 3 minutos
#define WATER_HEATER_MIN_ON_TIME (5 * 60 * 1000)  // 5 minutos (agua)
#define FAN_MIN_ON_TIME (5 * 60 * 1000)        // 5 minutos
#define LIGHT_MIN_TIME (5 * 60 * 1000)         // 5 minutos
#define PUMP_MIN_ON_TIME (3 * 60 * 1000)       // 3 minutos mínimo bomba

// ========================================
// CONFIGURACIÓN DE RUTINAS PERIÓDICAS INDEPENDIENTES
// ========================================

// CRONOGRAMA DE RECIRCULACIÓN (cada 2h) - INDEPENDIENTE
#define PUMP_INTERVAL_S 7200                   // 2 horas = 7200 segundos
#define PUMP_DURATION_S 720                    // 12 minutos = 720 segundos
#define PUMP_INTERVAL_MINUTES 120              // 2 horas = 120 minutos

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

// CONTROL DE LUZ SIMPLIFICADO - Solo ON/OFF basado en lux
enum LightState {
    LIGHT_OFF,                     // Apagada (lux suficiente o fuera de horario)
    LIGHT_ON                       // Encendida (lux insuficiente dentro del horario)
};

struct SensorReadings {
    float temperature;
    float humidity;
    float lightLux;         // BH1750 lux reading
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
    
    // CONTROL DE LUZ SIMPLIFICADO
    LightState lightState;                 // Estado actual de la luz (ON/OFF)
    unsigned long lightOnStartTime;        // Timestamp cuando se encendió la luz
    
    // HUMIDIFICADOR: Control simplificado con secuencia inmediata
    bool humidifierMasterActive;         // Relé maestro (PIN 14) activo
    unsigned long humidifierStartTime;   // Tiempo de inicio del ciclo

    // ========================================
    // VALIDACIÓN DE EFECTIVIDAD DEL HUMIDIFICADOR
    // ========================================
    float humidityAtStart;               // Humedad cuando se encendió
    float temperatureAtStart;            // Temperatura cuando se encendió
    unsigned long humidifierWindowStart; // Inicio de ventana de evaluación
    bool humidifierLocked;               // Bloqueado por inefectividad
    unsigned long humidifierLockedUntil; // Timestamp de expiración del bloqueo
    bool windowValidationActive;         // Ventana de validación activa
    
    // ========================================
    // PARCHE DE SEGURIDAD DEL CALEFACTOR (monitoreo rápido)
    // ========================================
    bool heaterFastMonitoringActive;     // Monitoreo rápido del calefactor activo
    unsigned long lastHeaterMonitorTime; // Última verificación del monitoreo rápido
    unsigned long heaterOnStartTime;     // Tiempo cuando se encendió el calefactor
    unsigned long heaterOffStartTime;    // Tiempo cuando se apagó el calefactor (para reposo)
    
    // Buffer de temperaturas para media móvil (filtro de ruido)
    float heaterTempBuffer[HEATER_TEMP_BUFFER_SIZE];
    int heaterTempBufferIndex;
    bool heaterTempBufferFull;
    
    // ========================================
    // RECIRCULACIÓN EXTRA POR CALEFACTOR DE AGUA
    // ========================================
    bool extraRecirculationActive;               // Recirculación extra activa
    unsigned long extraRecirculationStartTime;   // Inicio de recirculación extra
    unsigned long lastExtraRecirculationTime;    // Última vez que se activó recirculación extra
    bool lastWaterHeaterState;                   // Estado previo del calefactor (para detectar cambio)
    
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
    // PARCHE DE SEGURIDAD DEL CALEFACTOR - Funciones internas
    // ========================================
    void addTemperatureToHeaterBuffer(float temp);  // Añadir temperatura al buffer
    float getHeaterAverageTemperature();      // Obtener media móvil de temperaturas
    bool canTurnHeaterOn();                   // Verificar si puede encender (tiempos)
    bool canTurnHeaterOff();                  // Verificar si puede apagar (tiempos)
    void heaterSafetyTurnOff(const char* reason);   // Apagar por seguridad con logging
    
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
    
    // ========================================
    // RECIRCULACIÓN EXTRA POR CALEFACTOR DE AGUA
    // ========================================
    void onWaterHeaterStateChanged(bool isNowOn);      // Detecta cambio de estado del calefactor
    void tryTriggerExtraRecirculation();               // Intenta activar recirculación extra
    void handleExtraRecirculation();                   // Maneja ciclo de recirculación extra
    bool canTriggerExtraRecirculation();               // Valida si puede disparar recirculación extra
    
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

    // ========================================
    // MONITOREO RÁPIDO DEL CALEFACTOR (llamado desde main.cpp)
    // ========================================
    void heaterFastMonitoring();              // Monitoreo rápido cada 3 segundos

    // Estado del sistema
    bool isWaterLevelOk() const { return state.waterLevelOk; }
    bool isSystemEnabled() const { return state.systemEnabled; }

    // Diagnóstico
    void printSystemStatus();
    void printSensorAverages();
    void printActuatorStates();
};