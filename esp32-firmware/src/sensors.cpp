#include "sensors.h"
#include "config.h"
#include "pins.h"

SensorManager::SensorManager() : dht(PIN_DHT22, DHT_TYPE), tcs(TCS34725_INTEGRATIONTIME_614MS, TCS34725_GAIN_1X), 
                                 dhtInitialized(false), tcsInitialized(false) {
    // Initialize ADC for analog sensors with improved precision
    analogReadResolution(12);        // 0-4095 (12-bit resolution)
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
    
    // FÓRMULA C ESPECTRAL PARA FOTOSÍNTESIS (VALIDADA EMPÍRICAMENTE)
    // Fórmula C: (R+B)/RGB sin Clear - La mejor según análisis de test
    // Luz natural promedio: 67.53% | Luz artificial promedio: 78.11%
    // Umbral control: <67% enciende LED, >70% apaga LED
    float totalRGB = r + g + b;  // Sin canal Clear
    float lightIndex = 0.0;
    if (totalRGB > 0) {
        lightIndex = ((float)(r + b) / totalRGB) * 100.0;
    }
    
    Serial.printf("[SENSOR] TCS34725: R=%d G=%d B=%d C=%d LightIndex=%.2f%% (Fórmula C)\n", 
                  r, g, b, c, lightIndex);
    
    return lightIndex;
}

uint16_t SensorManager::readLightClearChannel() {
    if (!tcsInitialized) return 0;
    
    uint16_t r, g, b, c;
    
    // Read raw RGBC values
    tcs.getRawData(&r, &g, &b, &c);
    
    Serial.printf("[SENSOR] TCS34725 Clear Channel: C=%d\n", c);
    
    return c;  // Return raw Clear channel value
}

float SensorManager::calculateMedian(float values[], int size) {
    if (size == 0) return NAN;
    if (size == 1) return values[0];
    
    // Create a copy of the array for sorting (to avoid modifying original)
    float sortedValues[size];
    for (int i = 0; i < size; i++) {
        sortedValues[i] = values[i];
    }
    
    // Simple bubble sort for small arrays
    for (int i = 0; i < size - 1; i++) {
        for (int j = 0; j < size - i - 1; j++) {
            if (sortedValues[j] > sortedValues[j + 1]) {
                float temp = sortedValues[j];
                sortedValues[j] = sortedValues[j + 1];
                sortedValues[j + 1] = temp;
            }
        }
    }
    
    // Return median
    if (size % 2 == 0) {
        // Even number of elements: average of two middle elements
        return (sortedValues[size/2 - 1] + sortedValues[size/2]) / 2.0;
    } else {
        // Odd number of elements: middle element
        return sortedValues[size/2];
    }
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
    
    // Clear Channel - TCS34725 for darkness detection
    Serial.print("💡 Clear Channel: ");
    uint16_t clearChannel = readLightClearChannel();
    if (clearChannel > 0) {  // 0 indica sensor no disponible
        JsonObject clearReading = readings.createNestedObject();
        clearReading["physicalId"] = "TCS34725-A1"; // TCS34725 Clear channel physical ID  
        clearReading["variableId"] = "68d6dde25b8956ed967d6a8d"; // Clear Channel MongoDB ObjectId (temporal)
        clearReading["value"] = clearChannel;
        validReadings++;
        Serial.printf("%d ✅\n", clearChannel);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    // pH Level - only add if sensor is working
    Serial.print("🧪 pH Level: ");
    float phValue = readPH();
    if (!isnan(phValue)) {
        JsonObject phReading = readings.createNestedObject();
        phReading["physicalId"] = "SEN0161-A1"; // pH sensor physical ID
        phReading["variableId"] = "688970a27f02137645d58396"; // pH MongoDB ObjectId
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
        tdsReading["variableId"] = "688970a77f02137645d58397"; // TDS MongoDB ObjectId
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
        waterTempReading["variableId"] = "68bb4d8cbdcb66fc5738f9af"; // NTC MongoDB ObjectId
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
        waterLevelReading["physicalId"] = "WaterLevelModule-A1"; // Water Level Module sensor physical ID
        waterLevelReading["variableId"] = "68d1d07307c249cda4c369b0"; // Water Level MongoDB ObjectId
        waterLevelReading["value"] = waterLevelValue;
        validReadings++;
        Serial.printf("%.2f cm ✅\n", waterLevelValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    Serial.printf("📊 Total de lecturas válidas: %d/8\n", validReadings);
    
    // PhysicalId Mappings:
    // | Code        | physicalId    | Variable              | MongoDB ObjectId         | Status |
    // |-------------|---------------|-----------------------|--------------------------|--------|
    // | tcs34725-01 | TCS34725-A1   | Light Index           | 68d40894da0854c0bc32cb2c | Active |
    // | dht22-001   | DHT22-A1      | Temperature/Humidity  | 68d408e3da0854c0bc32cb2d | Active |
    // | ph-001      | SEN0161-A1    | pH Level              | 68d40911da0854c0bc32cb2f | Active |
    // | tds-001     | TDS-A1        | Electrical Conduct.   | 68d408feda0854c0bc32cb2e | Active |
    // | ntc-001     | NTC-A1        | Water Temperature     | 68d4091eda0854c0bc32cb30 | Active |
    // | waterlevel-module-01 | WaterLevelModule-A1 | Water Level           | 68d40931da0854c0bc32cb31 | Active |
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

// pH Sensor Reading (SEN0161 - Calibrated with median filtering)
float SensorManager::readPH() {
    const int NUM_SAMPLES = 15;  // Reducido para el sistema inteligente (vs 60 del test)
    const int SAMPLE_DELAY = 100;  // Reducido para el sistema inteligente (vs 500ms)
    
    // Constantes calibradas usando mediana de mediciones
    const float PH_SLOPE = 21.96f;
    const float PH_INTERCEPT = -16.08f;
    // Usar constantes definidas en pins.h: ADC_VREF y ADC_RESOLUTION
    
    float voltages[NUM_SAMPLES];
    
    // Tomar muestras con delay
    for (int i = 0; i < NUM_SAMPLES; i++) {
        int adcRaw = analogRead(PIN_PH_ADC);
        float voltage = adcRaw * (ADC_VREF / 4095.0f);  // 12-bit ADC = 0-4095
        voltages[i] = voltage;
        delay(SAMPLE_DELAY);
    }
    
    // Calcular mediana para filtrar ruido
    float medianVoltage = calculateMedian(voltages, NUM_SAMPLES);
    
    // Validar voltaje funcional
    if (medianVoltage < 0.05 || medianVoltage > 3.3) {
        Serial.printf("[SENSOR] pH voltage fuera de rango funcional: %.4fV\n", medianVoltage);
        return NAN;
    }
    
    // Calcular pH usando ecuación calibrada
    float phValue = PH_SLOPE * medianVoltage + PH_INTERCEPT;
    
    Serial.printf("[SENSOR] pH: %d muestras, mediana=%.4fV, pH=%.2f\n", 
                  NUM_SAMPLES, medianVoltage, phValue);
    
    // Validar rango pH funcional
    if (phValue < 0.0 || phValue > 14.0) {
        Serial.printf("[SENSOR] pH fuera de rango válido: %.2f\n", phValue);
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

float SensorManager::measureUltrasonicDistance() {
    // Send trigger pulse (standard HC-SR04 sequence)
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    delayMicroseconds(2);
    digitalWrite(PIN_ULTRA_TRIG, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    
    // Timeout 30ms ≈ 5m (según código de referencia)
    unsigned long duration = pulseIn(PIN_ULTRA_ECHO, HIGH, 30000);
    
    // Check for timeout
    if (duration == 0) return -1;
    
    // Convert duration to distance in cm
    // Speed of sound = 0.034 cm/μs (según código de referencia), divide by 2 for round trip
    float distance = duration * 0.017;  // (0.034 / 2)
    
    return distance;
}

// Ultrasonic Water Level Sensor
float SensorManager::readWaterLevel() {
    const float TANK_HEIGHT_CM = 40.0f;           // Altura total del tanque
    const int NUM_SAMPLES = 5;                    // Número de mediciones para promedio
    
    float suma = 0;
    int validas = 0;
    
    // Tomar múltiples mediciones y promediar solo las válidas (como en código de referencia)
    for (int i = 0; i < NUM_SAMPLES; i++) {
        float distance = measureUltrasonicDistance();
        if (distance > 0) {
            suma += distance;
            validas++;
        }
        delay(50); // Delay entre mediciones (según código de referencia)
    }
    
    // Si no hay mediciones válidas
    if (validas == 0) {
        Serial.println("💧 Water Level: [SENSOR] Sin lecturas válidas del ultrasónico ❌");
        return NAN;
    }
    
    // Calcular distancia promediada
    float distance = suma / validas;
    
    // Additional validation: distance should be reasonable for tank
    if (distance > TANK_HEIGHT_CM + 10.0f) { // 10cm margin for sensor mounting
        Serial.printf("💧 Water Level: [SENSOR] Distancia promedio inválida (%.2fcm > altura tanque %.0fcm) ❌\n", 
                     distance, TANK_HEIGHT_CM);
        return NAN;
    }
    
    // Calculate water level percentage
    // If sensor is mounted at top: level = 100 * (tank_height - distance) / tank_height
    float waterLevel = 100.0f * (TANK_HEIGHT_CM - distance) / TANK_HEIGHT_CM;
    
    // Ensure valid range (0-100%)
    if (waterLevel < 0.0f) waterLevel = 0.0f;
    if (waterLevel > 100.0f) waterLevel = 100.0f;
    
    Serial.printf("[SENSOR] Ultrasonic: %d muestras válidas, distancia promedio=%.2fcm, nivel=%.1f%%\n", 
                  validas, distance, waterLevel);
    
    return waterLevel;
}