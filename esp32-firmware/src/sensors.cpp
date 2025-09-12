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

void SensorManager::createReadingsBatch(DynamicJsonDocument& doc, String (*timestampFunction)()) {
    Serial.println("🔍 GENERANDO DATOS DE SENSORES");
    doc.clear();
    
    // Get timestamp from external function or fallback to internal
    String timestamp = (timestampFunction != nullptr) ? timestampFunction() : getCurrentTimestamp();
    
    // Set ESP32 ID and timestamp
    doc["esp32Id"] = ESP32_ID;
    doc["timestamp"] = timestamp;
    Serial.printf("🆔 ESP32 ID: %s\n", ESP32_ID);
    Serial.printf("⏰ Timestamp: %s\n", timestamp.c_str());
    
    // Create readings array
    JsonArray readings = doc.createNestedArray("readings");
    int validReadings = 0;
    
    // Temperature - only add if sensor is working
    Serial.print("🌡️  Temperatura: ");
    float tempValue = readTemperature();
    if (!isnan(tempValue)) {
        JsonObject tempReading = readings.createNestedObject();
        tempReading["physicalId"] = "DHT22-A1"; // DHT22 sensor physical ID
        tempReading["variableId"] = "688970ab7f02137645d58398"; // Temperature MongoDB ObjectId
        tempReading["value"] = tempValue;
        validReadings++;
        Serial.printf("%.1f°C ✅\n", tempValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // Humidity - only add if sensor is working
    Serial.print("💧 Humedad: ");
    float humidityValue = readHumidity();
    if (!isnan(humidityValue)) {
        JsonObject humidityReading = readings.createNestedObject();
        humidityReading["physicalId"] = "DHT22-A1"; // DHT22 sensor physical ID
        humidityReading["variableId"] = "688970af7f02137645d58399"; // Humidity MongoDB ObjectId
        humidityReading["value"] = humidityValue;
        validReadings++;
        Serial.printf("%.1f%% ✅\n", humidityValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // Light Level - only add if sensor is working
    Serial.print("💡 Luz: ");
    float lightValue = readLightLevel();
    if (!isnan(lightValue)) {
        JsonObject lightReading = readings.createNestedObject();
        lightReading["physicalId"] = "BH1750-A1"; // BH1750 sensor physical ID
        lightReading["variableId"] = "688970837f02137645d58395"; // Luminosity MongoDB ObjectId
        lightReading["value"] = lightValue;
        validReadings++;
        Serial.printf("%.1f lux ✅\n", lightValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    Serial.printf("📊 Total de lecturas válidas: %d/3\n", validReadings);
    
    // PhysicalId Mappings:
    // | Code       | physicalId | Variable              | MongoDB ObjectId         | Status |
    // |------------|------------|-----------------------|--------------------------|--------|
    // | bh1750-001 | BH1750-A1  | Luminosity            | 688970837f02137645d58395 | Active |
    // | dht22-001  | DHT22-A1   | Temperature/Humidity  | 688970ab7f02137645d58398 | Active |
    // | dht22-001  | DHT22-A1   | Temperature/Humidity  | 688970af7f02137645d58399 | Active |
    // | ph-001     | SEN0161-A1 | pH Level              | 688970a27f02137645d58396 | Stubbed|
    // | tds-001    | TDS-A1     | Electrical Conduct.   | 688970a77f02137645d58397 | Stubbed|
    //
    // Note: Stubbed sensors (pH, EC, water temp, water level) are not included
    // The backend will detect missing sensors and notify admin
}