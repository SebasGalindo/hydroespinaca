#ifndef SENSORS_H
#define SENSORS_H

#include <Arduino.h>
#include <DHT.h>
#include <ArduinoJson.h>

class SensorManager {
private:
    DHT dht;
    
    // Calibration values
    float phCalibration = 0.0;
    float ecCalibration = 1.0;
    
    // Moving average buffers
    static const int BUFFER_SIZE = 5;
    float phBuffer[BUFFER_SIZE];
    float ecBuffer[BUFFER_SIZE];
    int bufferIndex;
    
    float getMovingAverage(float* buffer, float newValue);
    
public:
    SensorManager();
    void begin();
    
    // Individual sensor readings
    float readTemperature();
    float readHumidity();
    float readPH();
    float readEC();
    // float readWaterLevel();
    float readLightLevel();
    
    // Batch reading
    void createReadingsBatch(DynamicJsonDocument& doc);
    
    // Calibration
    void calibratePH(float referenceValue);
    void calibrateEC(float referenceValue);
};

#endif