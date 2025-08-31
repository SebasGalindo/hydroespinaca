#include "sensors.h"
#include "config.h"

SensorManager::SensorManager() : dht(DHT_PIN, DHT_TYPE), dhtInitialized(false), bh1750Initialized(false) {
}

void SensorManager::begin() {
    // Initialize I2C
    Wire.begin(I2C_SDA_PIN, I2C_SCL_PIN);
    
    // Initialize DHT22
    dht.begin();
    dhtInitialized = true;
    Serial.println("✅ DHT22 inicializado");
    
    // Initialize BH1750
    if (lightMeter.begin(BH1750::CONTINUOUS_HIGH_RES_MODE)) {
        bh1750Initialized = true;
        Serial.println("✅ BH1750 inicializado");
    } else {
        Serial.println("❌ Error inicializando BH1750");
        bh1750Initialized = false;
    }
    
    Serial.println("✅ Sensores inicializados");
}

float SensorManager::readTemperature() {
    if (!dhtInitialized) return NAN;
    
    float temp = dht.readTemperature();
    if (isnan(temp)) {
        Serial.println("❌ Error leyendo temperatura");
        return NAN;
    }
    return temp;
}

float SensorManager::readHumidity() {
    if (!dhtInitialized) return NAN;
    
    float humidity = dht.readHumidity();
    if (isnan(humidity)) {
        Serial.println("❌ Error leyendo humedad");
        return NAN;
    }
    return humidity;
}

float SensorManager::readLightLevel() {
    if (!bh1750Initialized) return NAN;
    
    float lux = lightMeter.readLightLevel();
    if (lux < 0) {
        Serial.println("❌ Error leyendo sensor de luz");
        return NAN;
    }
    return lux;
}

String SensorManager::getCurrentTimestamp() {
    // Simple timestamp - in production use NTP
    unsigned long currentTime = millis() / 1000;
    char timestamp[32];
    sprintf(timestamp, "2025-08-31T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    return String(timestamp);
}

void SensorManager::createReadingsBatch(DynamicJsonDocument& doc) {
    doc.clear();
    
    // Set ESP32 ID and timestamp
    doc["esp32Id"] = ESP32_ID;
    doc["timestamp"] = getCurrentTimestamp();
    
    // Create readings array
    JsonArray readings = doc.createNestedArray("readings");
    
    // Temperature - only add if sensor is working
    float tempValue = readTemperature();
    if (!isnan(tempValue)) {
        JsonObject tempReading = readings.createNestedObject();
        tempReading["physicalId"] = "temp-01";
        tempReading["variableId"] = "temperature";
        tempReading["value"] = tempValue;
    }
    
    // Humidity - only add if sensor is working
    float humidityValue = readHumidity();
    if (!isnan(humidityValue)) {
        JsonObject humidityReading = readings.createNestedObject();
        humidityReading["physicalId"] = "humidity-01";
        humidityReading["variableId"] = "humidity";
        humidityReading["value"] = humidityValue;
    }
    
    // Light Level - only add if sensor is working
    float lightValue = readLightLevel();
    if (!isnan(lightValue)) {
        JsonObject lightReading = readings.createNestedObject();
        lightReading["physicalId"] = "light-01";
        lightReading["variableId"] = "light_intensity";
        lightReading["value"] = lightValue;
    }
    
    // Note: Stubbed sensors (pH, EC, water temp, water level) are not included
    // The backend will detect missing sensors and notify admin
}