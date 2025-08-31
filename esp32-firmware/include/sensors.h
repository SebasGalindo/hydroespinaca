#ifndef SENSORS_H
#define SENSORS_H

#include <Arduino.h>
#include <DHT.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <BH1750.h>

class SensorManager {
private:
    DHT dht;
    BH1750 lightMeter;
    
    // Sensor validity flags
    bool dhtInitialized;
    bool bh1750Initialized;
    
    // Time management
    String getCurrentTimestamp();
    
public:
    SensorManager();
    void begin();
    
    // Individual sensor readings (return NaN if sensor fails)
    float readTemperature();
    float readHumidity();
    float readLightLevel();
    
    // Batch reading - creates JSON with null values for failed sensors
    void createReadingsBatch(DynamicJsonDocument& doc);
    
    // Sensor status
    bool isDHTAvailable() const { return dhtInitialized; }
    bool isBH1750Available() const { return bh1750Initialized; }
};

#endif