#include "sensors.h"
#include "config.h"
#include "pins.h"

SensorManager::SensorManager(NTPClient* ntpClient) : dht(PIN_DHT22, DHT_TYPE),
                                 timeClient(ntpClient), dhtInitialized(false), bh1750Initialized(false) {
    // Initialize ADC for analog sensors with improved precision
    analogReadResolution(12);        // 0-4095 (12-bit resolution)
    analogSetAttenuation(ADC_11db);  // For 3.3V input range
}

void SensorManager::begin() {
    Wire.begin(PIN_I2C_SDA, PIN_I2C_SCL);

    pinMode(PIN_PH_ADC, INPUT);
    pinMode(PIN_TDS_ADC, INPUT);
    pinMode(PIN_NTC_TANK, INPUT);

    pinMode(PIN_ULTRA_TRIG, OUTPUT);
    pinMode(PIN_ULTRA_ECHO, INPUT);

    dht.begin();
    dhtInitialized = true;

    if (lightMeter.begin(BH1750::CONTINUOUS_HIGH_RES_MODE)) {
        bh1750Initialized = true;
    } else {
        Serial.println("ERROR: BH1750 init failed");
        bh1750Initialized = false;
    }
}

float SensorManager::readTemperature() {
    if (!dhtInitialized) return NAN;
    return dht.readTemperature();
}

float SensorManager::readHumidity() {
    if (!dhtInitialized) return NAN;
    return dht.readHumidity();
}

float SensorManager::readLightLux() {
    if (!bh1750Initialized) return NAN;
    float lux = lightMeter.readLightLevel();
    if (lux < 0) return NAN;
    return lux;
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
    doc.clear();

    String timestamp = (timestampFunction != nullptr) ? timestampFunction() : getCurrentTimestamp();

    doc["esp32Id"] = ESP32_ID;
    doc["timestamp"] = timestamp;

    JsonArray readings = doc.createNestedArray("readings");

    // Temperature
    float tempValue = readTemperature();
    yield();
    if (!isnan(tempValue)) {
        JsonObject tempReading = readings.createNestedObject();
        tempReading["physicalId"] = "DHT22-A1";
        tempReading["variableCode"] = "T_AMB";
        tempReading["value"] = tempValue;
    }

    // Humidity
    float humidityValue = readHumidity();
    yield();
    if (!isnan(humidityValue)) {
        JsonObject humidityReading = readings.createNestedObject();
        humidityReading["physicalId"] = "DHT22-A1";
        humidityReading["variableCode"] = "HUM";
        humidityReading["value"] = humidityValue;
    }

    // Light (only during 5:00-19:00)
    bool lightTelemetryActive = isLightTelemetryActive();
    if (lightTelemetryActive) {
        float lux = readLightLux();
        yield();
        if (!isnan(lux)) {
            JsonObject luxReading = readings.createNestedObject();
            luxReading["physicalId"] = "BH1750-A1";
            luxReading["variableCode"] = "LUMINOSITY";
            luxReading["value"] = lux;
        }
    }

    // pH
    float phValue = readPH();
    yield();
    if (!isnan(phValue)) {
        JsonObject phReading = readings.createNestedObject();
        phReading["physicalId"] = "SEN0161-A1";
        phReading["variableCode"] = "PH";
        phReading["value"] = phValue;
    }

    // EC
    float ecValue = readTDS();
    yield();
    if (!isnan(ecValue)) {
        JsonObject ecReading = readings.createNestedObject();
        ecReading["physicalId"] = "TDS-A1";
        ecReading["variableCode"] = "EC";
        ecReading["value"] = ecValue;
    }

    // Water Temperature
    float waterTempValue = readTankTemperature();
    yield();
    if (!isnan(waterTempValue)) {
        JsonObject waterTempReading = readings.createNestedObject();
        waterTempReading["physicalId"] = "NTC-A1";
        waterTempReading["variableCode"] = "T_WAT";
        waterTempReading["value"] = waterTempValue;
    }

    // Water Level
    float waterLevelValue = readWaterLevel();
    yield();
    if (!isnan(waterLevelValue)) {
        JsonObject waterLevelReading = readings.createNestedObject();
        waterLevelReading["physicalId"] = "HC-SR04-A1";
        waterLevelReading["variableCode"] = "WL";
        waterLevelReading["value"] = waterLevelValue;
    }
}

// ADC Helper Functions
float SensorManager::readADCVoltage(int pin) {
    return readADCVoltageAveraged(pin, 10);  // Default 10 samples
}

float SensorManager::readADCVoltageAveraged(int pin, int samples) {
    long sum = 0;
    for (int i = 0; i < samples; i++) {
        sum += analogRead(pin);
        delay(5);
        if (i % 5 == 0) yield();
    }

    int rawValue = sum / samples;
    float voltage = (rawValue / ADC_RESOLUTION) * ADC_VREF;

    if (voltage < 0.01) voltage = 0.01;
    if (voltage > 3.2) voltage = 3.2;

    return voltage;
}

float SensorManager::readPH() {
    const int NUM_SAMPLES = 10;             // Reduced from 15 to 10 for faster reading
    const int SAMPLE_DELAY = 40;            // Reduced from 50ms to 40ms (still stable for pH sensor)
    const float PH_SLOPE = 1.529f;
    const float PH_INTERCEPT = 4.909f;

    float voltages[NUM_SAMPLES];

    // Total time: ~400ms (10 samples × 40ms) - balanced accuracy vs speed
    for (int i = 0; i < NUM_SAMPLES; i++) {
        int adcRaw = analogRead(PIN_PH_ADC);
        float voltage = (adcRaw * ADC_VREF) / ADC_RESOLUTION;
        voltages[i] = voltage;

        // Yield every 3 iterations for watchdog
        if (i % 3 == 0) yield();

        // Delay between samples (not after last sample)
        if (i < NUM_SAMPLES - 1) {
            delay(SAMPLE_DELAY);
        }
    }

    float medianVoltage = calculateMedian(voltages, NUM_SAMPLES);

    if (medianVoltage < 0.05 || medianVoltage > 3.3) {
        return NAN;
    }

    float phValue = PH_SLOPE * medianVoltage + PH_INTERCEPT;

    if (phValue < 0.0 || phValue > 14.0 || isnan(phValue)) {
        return NAN;
    }

    return phValue;
}

float SensorManager::readTDS() {
    static int analogBuffer[30];
    static int analogBufferIndex = 0;
    static float calibrationFactor = 1.0;

    int adcValue = analogRead(PIN_TDS_ADC);

    analogBuffer[analogBufferIndex++] = adcValue;
    if (analogBufferIndex >= 30) analogBufferIndex = 0;

    long avgValue = 0;
    for (int i = 0; i < 30; i++) {
        avgValue += analogBuffer[i];
    }
    avgValue /= 30;

    float voltage = (avgValue / 4095.0) * ADC_VREF;

    float tdsValue = (133.42 * pow(voltage, 3)
                    - 255.86 * pow(voltage, 2)
                    + 857.39 * voltage) * calibrationFactor;

    if (tdsValue < 0) tdsValue = 0;
    if (isnan(tdsValue)) tdsValue = 0;

    float ecValue = tdsValue / 500.0f;

    if (ecValue < 0.0f || ecValue > 10.0f) {
        return NAN;
    }

    return ecValue;
}

// Beta equation for NTC temperature conversion (simplified)
float SensorManager::steinhart(float resistance) {
    // Ecuación Beta simplificada usando constantes de config.h
    float steinhart;
    steinhart = resistance / NTC_NOMINAL_RESISTANCE;     // (R/Ro)
    steinhart = log(steinhart);                          // ln(R/Ro)
    steinhart /= NTC_BETA;                               // 1/B * ln(R/Ro)
    steinhart += 1.0 / (NTC_NOMINAL_TEMP + 273.15);     // + (1/To)
    steinhart = 1.0 / steinhart;                         // Invertir
    float temperatureC = steinhart - 273.15;             // Kelvin → Celsius

    return temperatureC;
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

float SensorManager::readTankTemperature() {
    const int NUM_SAMPLES = 10;

    long adcSum = 0;
    for (int i = 0; i < NUM_SAMPLES; i++) {
        adcSum += analogRead(PIN_NTC_TANK);
        delay(5);
    }
    int adcValue = adcSum / NUM_SAMPLES;

    float voltage = (adcValue / 4095.0) * ADC_VREF;

    if (voltage < 0.01 || voltage > 3.29) {
        return NAN;
    }

    float resistance;

    #ifdef NTC_CIRCUIT_A
        resistance = NTC_R_FIXED * ((ADC_VREF / voltage) - 1.0);
    #elif defined(NTC_CIRCUIT_B)
        resistance = NTC_R_FIXED * (voltage / (ADC_VREF - voltage));
    #else
        #error "Debe definir NTC_CIRCUIT_A o NTC_CIRCUIT_B en config.h"
    #endif

    if (resistance < 1000 || resistance > 100000) {
        return NAN;
    }

    float temperatureC = steinhart(resistance);

    if (isnan(temperatureC) || temperatureC < -10 || temperatureC > 60) {
        return NAN;
    }

    return temperatureC;
}

// Roots Temperature (NTC sensor) - REMOVED

float SensorManager::measureUltrasonicDistance() {
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    delayMicroseconds(2);
    digitalWrite(PIN_ULTRA_TRIG, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_ULTRA_TRIG, LOW);

    long duration = pulseIn(PIN_ULTRA_ECHO, HIGH);
    float distance = (duration * 0.0343) / 2;

    if (duration == 0 || distance < 2 || distance > 400) {
        return -1;
    }

    return distance;
}

float SensorManager::readWaterLevel() {
    const int NUM_READINGS = 7;                 // Balanced: 7 readings for good statistics without excessive time
    const int READING_DELAY_MS = 70;            // 70ms between readings (optimal for HC-SR04 stabilization)
    const float MAX_DEVIATION_PERCENT = 20.0;   // Strict outlier rejection (20% from median)
    const int MIN_VALID_READINGS = 4;           // At least 57% of readings must be valid
    const int MIN_FILTERED_READINGS = 3;        // Need at least 3 readings after outlier removal

    float readings[NUM_READINGS];
    int validCount = 0;

    // Take multiple readings with proper delay for sensor stabilization
    // Total time: ~490ms (7 readings × 70ms) - safe for watchdog
    for (int i = 0; i < NUM_READINGS; i++) {
        float distance = measureUltrasonicDistance();
        if (distance > 0) {
            readings[validCount++] = distance;
        }

        // Yield to watchdog every 2 iterations to prevent resets
        if (i % 2 == 0) yield();

        // Delay between readings for sensor stabilization
        if (i < NUM_READINGS - 1) {  // No delay after last reading
            delay(READING_DELAY_MS);
        }
    }

    // Need at least MIN_VALID_READINGS valid readings
    if (validCount < MIN_VALID_READINGS) {
        Serial.printf("[SENSOR] Water Level: Not enough valid readings (%d/%d)\n", validCount, NUM_READINGS);
        return NAN;
    }

    // Calculate median of valid readings
    float median = calculateMedian(readings, validCount);

    // Filter outliers: remove readings that deviate more than MAX_DEVIATION_PERCENT from median
    float filteredSum = 0;
    int filteredCount = 0;
    float maxDeviation = median * (MAX_DEVIATION_PERCENT / 100.0);

    for (int i = 0; i < validCount; i++) {
        float deviation = abs(readings[i] - median);
        if (deviation <= maxDeviation) {
            filteredSum += readings[i];
            filteredCount++;
        } else {
            Serial.printf("[SENSOR] Water Level: Outlier rejected: %.2f cm (median: %.2f cm, dev: %.2f%%)\n",
                         readings[i], median, (deviation / median) * 100.0);
        }
    }

    // Need at least MIN_FILTERED_READINGS readings after filtering
    if (filteredCount < MIN_FILTERED_READINGS) {
        Serial.printf("[SENSOR] Water Level: Too many outliers (%d/%d valid)\n", filteredCount, validCount);
        return NAN;
    }

    float finalValue = filteredSum / filteredCount;

    Serial.printf("[SENSOR] Water Level: %.2f cm (used %d/%d readings, rejected %d outliers)\n",
                 finalValue, filteredCount, validCount, validCount - filteredCount);

    return finalValue;
}

bool SensorManager::isLightTelemetryActive() {
    if (!timeClient || !timeClient->isTimeSet()) {
        return true;
    }

    int currentHour = timeClient->getHours();
    currentHour = (currentHour - 5 + 24) % 24;

    return (currentHour >= 5 && currentHour < 19);
}