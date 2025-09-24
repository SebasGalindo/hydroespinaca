#include "sensors.h"
#include "config.h"
#include "pins.h"

SensorManager::SensorManager() : dht(PIN_DHT22, DHT_TYPE), tcs(TCS34725_INTEGRATIONTIME_614MS, TCS34725_GAIN_1X), 
                                 dhtInitialized(false), tcsInitialized(false) {
    // Initialize ADC for analog sensors
    analogSetAttenuation(ADC_11db);  // For 3.3V input range
}

void SensorManager::begin() {
    // Initialize I2C
    Wire.begin(PIN_I2C_SDA, PIN_I2C_SCL);
    
    // Configure ADC pins
    pinMode(PIN_PH_ADC, INPUT);
    pinMode(PIN_TDS_ADC, INPUT);
    pinMode(PIN_NTC_TANK, INPUT);
    
    // Configure ultrasonic pins
    pinMode(PIN_ULTRA_TRIG, OUTPUT);
    pinMode(PIN_ULTRA_ECHO, INPUT);
    
    // Initialize DHT22
    dht.begin();
    dhtInitialized = true;
    Serial.println("✅ DHT22 inicializado");
    
    // BH1750 removed - using TCS34725 for light sensing
    
    // Initialize TCS34725
    if (tcs.begin()) {
        tcsInitialized = true;
        Serial.println("✅ TCS34725 inicializado");
    } else {
        Serial.println("❌ Error inicializando TCS34725");
        tcsInitialized = false;
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

// BH1750 readLightLevel() removed - use readLightIndex() instead

float SensorManager::readLightIndex() {
    if (!tcsInitialized) return NAN;
    
    uint16_t r, g, b, c;
    
    // Read raw RGBC values
    tcs.getRawData(&r, &g, &b, &c);
    
    // Validate readings
    if (c == 0) {
        Serial.println("[SENSOR] TCS34725 - Clear channel is zero, sensor may be covered");
        return NAN;
    }
    
    // Calculate total for normalization
    float total = r + g + b + c;
    if (total == 0) {
        Serial.println("[SENSOR] TCS34725 - All channels zero");
        return NAN;
    }
    
    // Apply the proposed formula: LightIndex = (0.5 * (R / (R+B+G+C))) + (0.5 * (B / (R+B+G+C)))
    float rNormalized = (float)r / total;
    float bNormalized = (float)b / total;
    float lightIndex = (0.5 * rNormalized) + (0.5 * bNormalized);
    
    // Convert to 0-100 scale
    lightIndex *= 100.0;
    
    Serial.printf("[SENSOR] TCS34725: R=%d G=%d B=%d C=%d LightIndex=%.2f\n", 
                  r, g, b, c, lightIndex);
    
    return lightIndex;
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
    
    // Light Index - TCS34725 color sensor (replaces BH1750)
    Serial.print("🌈 LightIndex: ");
    float lightIndex = readLightIndex();
    if (!isnan(lightIndex)) {
        JsonObject lightReading = readings.createNestedObject();
        lightReading["physicalId"] = "TCS34725-A1"; // TCS34725 sensor physical ID
        lightReading["variableId"] = "688970837f02137645d58395"; // Light Index MongoDB ObjectId
        lightReading["value"] = lightIndex;
        validReadings++;
        Serial.printf("%.1f%% ✅\n", lightIndex);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // pH Level - only add if sensor is working
    Serial.print("🧪 pH Level: ");
    float phValue = readPH();
    if (!isnan(phValue)) {
        JsonObject phReading = readings.createNestedObject();
        phReading["physicalId"] = "SEN0161-A1"; // pH sensor physical ID
        phReading["variableId"] = "6872d26e4e4c4db795189cf8"; // pH MongoDB ObjectId
        phReading["value"] = phValue;
        validReadings++;
        Serial.printf("%.2f pH ✅\n", phValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // TDS (Electrical Conductivity) - only add if sensor is working
    Serial.print("⚡ TDS/EC: ");
    float tdsValue = readTDS();
    if (!isnan(tdsValue)) {
        JsonObject tdsReading = readings.createNestedObject();
        tdsReading["physicalId"] = "TDS-A1"; // TDS sensor physical ID
        tdsReading["variableId"] = "6872d2794e4c4db795189cf9"; // TDS MongoDB ObjectId
        tdsReading["value"] = tdsValue;
        validReadings++;
        Serial.printf("%.1f ppm ✅\n", tdsValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // Water Temperature (NTC) - only add if sensor is working
    Serial.print("🌡️  Water Temp: ");
    float waterTempValue = readTankTemperature();
    if (!isnan(waterTempValue)) {
        JsonObject waterTempReading = readings.createNestedObject();
        waterTempReading["physicalId"] = "NTC-A1"; // NTC water sensor physical ID
        waterTempReading["variableId"] = "68d1cf1407c249cda4c369ae"; // NTC MongoDB ObjectId
        waterTempReading["value"] = waterTempValue;
        validReadings++;
        Serial.printf("%.1f°C ✅\n", waterTempValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // Water Level (HC-SR04 Ultrasonic) - only add if sensor is working
    Serial.print("💧 Water Level: ");
    float waterLevelValue = readWaterLevel();
    if (!isnan(waterLevelValue)) {
        JsonObject waterLevelReading = readings.createNestedObject();
        waterLevelReading["physicalId"] = "HC_SR04-A1"; // HC-SR04 sensor physical ID
        waterLevelReading["variableId"] = "68d1d11c07c249cda4c369b2"; // Water Level MongoDB ObjectId
        waterLevelReading["value"] = waterLevelValue;
        validReadings++;
        Serial.printf("%.1f%% ✅\n", waterLevelValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    Serial.printf("📊 Total de lecturas válidas: %d/7\n", validReadings);
    
    // PhysicalId Mappings:
    // | Code        | physicalId    | Variable              | MongoDB ObjectId         | Status |
    // |-------------|---------------|-----------------------|--------------------------|--------|
    // | tcs34725-01 | TCS34725-A1   | Light Index           | 688970837f02137645d58395 | Active |
    // | dht22-001   | DHT22-A1      | Temperature/Humidity  | 688970ab7f02137645d58398 | Active |
    // | dht22-001   | DHT22-A1      | Temperature/Humidity  | 688970af7f02137645d58399 | Active |
    // | ph-001      | SEN0161-A1    | pH Level              | 6872d26e4e4c4db795189cf8 | Active |
    // | tds-001     | TDS-A1        | Electrical Conduct.   | 6872d2794e4c4db795189cf9 | Active |
    // | ntc-001     | NTC-A1        | Water Temperature     | 68d1cf1407c249cda4c369ae | Active |
    // | hc_sr04-001 | HC_SR04-A1    | Water Level           | 68d1d11c07c249cda4c369b2 | Active |
    //
    // Note: All sensors now active and sending telemetry data
    // NTC Roots implementation removed as requested
}

// ADC Helper Functions
float SensorManager::readADCVoltage(int pin) {
    int rawValue = analogRead(pin);
    float voltage = (rawValue / ADC_RESOLUTION) * ADC_VREF;
    Serial.printf("[SENSOR] ADC Pin %d: raw=%d, voltage=%.3fV\n", pin, rawValue, voltage);
    return voltage;
}

// pH Sensor Reading
float SensorManager::readPH() {
    float voltage = readADCVoltage(PIN_PH_ADC);
    if (voltage < 0.1 || voltage > 3.2) {
        Serial.println("[SENSOR] pH voltage out of range");
        return NAN;
    }
    
    // Convert voltage to pH using calibration
    // pH = a * voltage + b (adjust calibration values as needed)
    float phValue = phCalibration.a * voltage + phCalibration.b;
    Serial.printf("[SENSOR] pH raw V=%.3f pH=%.2f\n", voltage, phValue);
    
    // Validate pH range
    if (phValue < 0.0 || phValue > 14.0) {
        Serial.println("[SENSOR] pH value out of valid range");
        return NAN;
    }
    
    return phValue;
}

// TDS Sensor Reading
float SensorManager::readTDS() {
    float voltage = readADCVoltage(PIN_TDS_ADC);
    if (voltage < 0.1 || voltage > 3.2) {
        Serial.println("[SENSOR] TDS voltage out of range");
        return NAN;
    }
    
    // Convert voltage to TDS (Total Dissolved Solids)
    float tdsValue = voltage * tdsCalibration.factor * 1000; // Convert to ppm
    Serial.printf("[SENSOR] TDS raw V=%.3f TDS=%.1f ppm\n", voltage, tdsValue);
    
    return tdsValue;
}

// Steinhart-Hart equation for NTC temperature conversion
float SensorManager::steinhart(float resistance) {
    // Steinhart-Hart coefficients for 10k NTC
    const float A = 0.001129148;
    const float B = 0.000234125;
    const float C = 0.0000000876741;
    
    float logR = log(resistance);
    float tempK = 1.0 / (A + B * logR + C * logR * logR * logR);
    float tempC = tempK - 273.15;
    
    return tempC;
}

float SensorManager::convertToTemperature(float resistance, bool isTank) {
    if (resistance <= 0) return NAN;
    
    float temperature = steinhart(resistance);
    
    // Validate temperature range
    if (temperature < -10.0 || temperature > 60.0) {
        Serial.printf("[SENSOR] %s temperature out of range: %.2f°C\n", 
                     isTank ? "Tank" : "Roots", temperature);
        return NAN;
    }
    
    return temperature;
}

// Tank Temperature (NTC sensor)
float SensorManager::readTankTemperature() {
    float voltage = readADCVoltage(PIN_NTC_TANK);
    if (voltage < 0.1 || voltage > 3.2) {
        Serial.println("[SENSOR] Tank NTC voltage out of range");
        return NAN;
    }
    
    // Convert voltage to resistance (assuming voltage divider with 10k resistor)
    float resistance = 10000.0 * voltage / (ADC_VREF - voltage);
    float temperature = convertToTemperature(resistance, true);
    
    Serial.printf("[SENSOR] Tank temp: V=%.3f R=%.1fΩ T=%.2f°C\n", 
                  voltage, resistance, temperature);
    
    return temperature;
}

// Roots Temperature (NTC sensor) - REMOVED

// Ultrasonic Water Level Sensor
float SensorManager::readWaterLevel() {
    // Send trigger pulse
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    delayMicroseconds(2);
    digitalWrite(PIN_ULTRA_TRIG, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    
    // Read echo pulse with timeout (30ms = ~5m max distance)
    unsigned long duration = pulseIn(PIN_ULTRA_ECHO, HIGH, 30000);
    
    if (duration == 0) {
        Serial.println("[SENSOR] Ultrasonic timeout - no echo received");
        return NAN;
    }
    
    // Convert duration to distance in cm
    // Speed of sound = 34300 cm/s, divide by 2 for round trip
    float distance = (duration * 0.0343) / 2.0;
    
    // Validate distance is within tank limits
    if (distance > TANK_MAX_DISTANCE_CM || distance < 0.5) {
        Serial.printf("[SENSOR] Ultrasonic distance out of range: %.2f cm\n", distance);
        return NAN;
    }
    
    // Convert distance to water level percentage
    // (tank full = min distance, tank empty = max distance)
    float waterLevel = ((TANK_MAX_DISTANCE_CM - distance) / TANK_MAX_DISTANCE_CM) * 100.0;
    if (waterLevel < 0) waterLevel = 0;
    if (waterLevel > 100) waterLevel = 100;
    
    Serial.printf("[SENSOR] Ultrasonic: duration=%luμs distance=%.2fcm level=%.1f%%\n", 
                  duration, distance, waterLevel);
    
    return waterLevel;
}