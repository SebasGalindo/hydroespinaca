#ifndef SENSORS_H
#define SENSORS_H

#include <Arduino.h>
#include <DHT.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <Adafruit_TCS34725.h>
#include "pins.h"

class SensorManager {
private:
    DHT dht;
    Adafruit_TCS34725 tcs;
    
    // Sensor validity flags
    bool dhtInitialized;
    bool tcsInitialized;
    
    // ADC calibration parameters
    struct {
        float a = 3.5;  // Slope for pH conversion
        float b = 7.0;  // Offset for pH conversion
    } phCalibration;
    
    struct {
        float factor = 0.5;  // TDS conversion factor
    } tdsCalibration;
    
    // Time management
    String getCurrentTimestamp();
    
    // ADC helper functions
    float readADCVoltage(int pin);
    float readADCVoltageAveraged(int pin, int samples);
    float convertToTemperature(float resistance, bool isTank = true);
    float steinhart(float resistance);
    
    // Noise filtering for ADC sensors
    float calculateMedian(float values[], int size);
    
    // Ultrasonic sensor helper
    float measureUltrasonicDistance();
    
public:
    SensorManager();
    void begin();
    
    // Individual sensor readings (return NaN if sensor fails)
    float readTemperature();
    float readHumidity();
    float readLightIndex();  // TCS34725 color sensor (replaces BH1750)
    uint16_t readLightClearChannel();  // TCS34725 Clear channel for darkness detection
    
    // New ADC sensors
    float readPH();
    float readTDS();
    float readTankTemperature();
    float readWaterLevel();  // Ultrasonic sensor
    
    // Batch reading - creates JSON with null values for failed sensors
    void createReadingsBatch(DynamicJsonDocument& doc, String (*timestampFunction)() = nullptr);
    
    // Sensor status
    bool isDHTAvailable() const { return dhtInitialized; }
    bool isTCSAvailable() const { return tcsInitialized; }
};

#endif