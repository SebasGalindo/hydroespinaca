#include "sensors.h"
#include "config.h"

SensorManager::SensorManager() : dht(DHT_PIN, DHT_TYPE), bufferIndex(0) {
    // Initialize buffers
    for(int i = 0; i < BUFFER_SIZE; i++) {
        phBuffer[i] = 7.0; // Neutral pH
        ecBuffer[i] = 0.0;
    }
}

void SensorManager::begin() {
    dht.begin();
    
    // Initialize ADC
    analogReadResolution(12); // 12-bit ADC
    analogSetAttenuation(ADC_11db); // 0-3.3V range
    
    Serial.println("✅ Sensores inicializados");
}

float SensorManager::readTemperature() {
    float temp = dht.readTemperature();
    if (isnan(temp)) {
        Serial.println("❌ Error leyendo temperatura");
        return -999.0;
    }
    return temp;
}

float SensorManager::readHumidity() {
    float humidity = dht.readHumidity();
    if (isnan(humidity)) {
        Serial.println("❌ Error leyendo humedad");
        return -999.0;
    }
    return humidity;
}

float SensorManager::readPH() {
    int rawValue = analogRead(PH_SENSOR_PIN);
    float voltage = rawValue * (3.3 / 4095.0);
    
    // Conversión básica pH (ajustar según tu sensor)
    float ph = 7.0 - ((voltage - 1.65) / 0.18) + phCalibration;
    
    // Aplicar filtro de media móvil
    ph = getMovingAverage(phBuffer, ph);
    
    return constrain(ph, 0.0, 14.0);
}

float SensorManager::readEC() {
    int rawValue = analogRead(EC_SENSOR_PIN);
    float voltage = rawValue * (3.3f / 4095.0f);
    
    float ec = (voltage * 2.0f) * ecCalibration;
    ec = getMovingAverage(ecBuffer, ec);

    // Evita el error usando float en ambos lados
    return max(ec, 0.0f);
}


// float SensorManager::readWaterLevel() {
//     int rawValue = analogRead(WATER_LEVEL_PIN);
//     float percentage = (rawValue / 4095.0f) * 100.0f;
//     return constrain(percentage, 0.0f, 100.0f);
// }

float SensorManager::readLightLevel() {
    int rawValue = analogRead(LDR_PIN);
    float lux = map(rawValue, 0, 4095, 0, 1000); // Aproximación
    return lux;
}

float SensorManager::getMovingAverage(float* buffer, float newValue) {
    buffer[bufferIndex] = newValue;
    bufferIndex = (bufferIndex + 1) % BUFFER_SIZE;
    
    float sum = 0;
    for(int i = 0; i < BUFFER_SIZE; i++) {
        sum += buffer[i];
    }
    return sum / BUFFER_SIZE;
}

void SensorManager::createReadingsBatch(DynamicJsonDocument& doc) {
    // Clear document
    doc.clear();
    
    // Backend format - esp32Id and ISO timestamp
    doc["esp32Id"] = ESP32_ID;
    
    // ISO 8601 timestamp (usar NTP en producción)
    char timestamp[32];
    unsigned long currentTime = millis() / 1000; // Segundos desde boot
    sprintf(timestamp, "2025-07-29T%02d:%02d:%02dZ", 
            (int)((currentTime / 3600) % 24),
            (int)((currentTime / 60) % 60), 
            (int)(currentTime % 60));
    doc["timestamp"] = timestamp;
    
    // Readings array con physicalId y variableId
    JsonArray readings = doc.createNestedArray("readings");
    
    // Temperature
    float tempValue = readTemperature();
    if (tempValue != -999.0) {
        JsonObject tempReading = readings.createNestedObject();
        tempReading["physicalId"] = "temp-01";
        tempReading["variableId"] = "temperature";
        tempReading["value"] = tempValue;
    }
    
    // Humidity
    float humidityValue = readHumidity();
    if (humidityValue != -999.0) {
        JsonObject humidityReading = readings.createNestedObject();
        humidityReading["physicalId"] = "humidity-01";
        humidityReading["variableId"] = "humidity";
        humidityReading["value"] = humidityValue;
    }
    
    // pH
    float phValue = readPH();
    JsonObject phReading = readings.createNestedObject();
    phReading["physicalId"] = "ph-01";
    phReading["variableId"] = "ph";
    phReading["value"] = phValue;
    
    // EC (Conductividad Eléctrica)
    float ecValue = readEC();
    JsonObject ecReading = readings.createNestedObject();
    ecReading["physicalId"] = "ec-01";
    ecReading["variableId"] = "ec";
    ecReading["value"] = ecValue;
    
    // Water Level
    // float waterValue = readWaterLevel();
    // JsonObject waterReading = readings.createNestedObject();
    // waterReading["physicalId"] = "water-level-01";
    // waterReading["variableId"] = "water_level";
    // waterReading["value"] = waterValue;
    
    // Light Level
    float lightValue = readLightLevel();
    JsonObject lightReading = readings.createNestedObject();
    lightReading["physicalId"] = "light-01";
    lightReading["variableId"] = "light_intensity";
    lightReading["value"] = lightValue;
}

void SensorManager::calibratePH(float referenceValue) {
    float currentReading = readPH();
    phCalibration = referenceValue - currentReading;
    Serial.printf("✅ pH calibrado. Offset: %.2f\n", phCalibration);
}

void SensorManager::calibrateEC(float referenceValue) {
    float currentReading = readEC();
    if (currentReading > 0) {
        ecCalibration = referenceValue / currentReading;
        Serial.printf("✅ EC calibrado. Factor: %.2f\n", ecCalibration);
    }
}