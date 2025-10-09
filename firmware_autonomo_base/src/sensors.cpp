#include "sensors.h"
#include "config.h"
#include "pins.h"

SensorManager::SensorManager(NTPClient* ntpClient) : dht(PIN_DHT22, DHT_TYPE), tcs(TCS34725_INTEGRATIONTIME_614MS, TCS34725_GAIN_1X), 
                                 timeClient(ntpClient), dhtInitialized(false), tcsInitialized(false) {
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
    
    // NUEVA VALIDACIÓN: Solo calcular lightIndex si hay suficiente luz (C >= 3000)
    if (c < 3000) {
        Serial.printf("[SENSOR] TCS34725: C=%d < 3000 (oscuridad) → LightIndex no calculado\n", c);
        return NAN;  // No calcular ni enviar Index en oscuridad
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
    // Umbral control: <70% enciende LED, >75% por 5min apaga LED
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
    
    // LECTURAS DE LUZ CON RESTRICCIÓN HORARIA Y VALIDACIÓN OPTIMIZADA
    bool lightTelemetryActive = isLightTelemetryActive();
    
    if (lightTelemetryActive) {
        // DENTRO DEL HORARIO (6:00-18:00): Procesar lecturas de luz
        
        // Clear Channel - SIEMPRE enviar si estamos en horario válido
        Serial.print("💡 Clear Channel: ");
        uint16_t clearChannel = readLightClearChannel();
        if (clearChannel > 0) {  // 0 indica sensor no disponible
            JsonObject clearReading = readings.createNestedObject();
            clearReading["physicalId"] = "TCS34725-A1"; // TCS34725 Clear channel physical ID  
            clearReading["variableId"] = "68d6dde25b8956ed967d6a8d"; // Clear Channel MongoDB ObjectId
            clearReading["value"] = clearChannel;
            validReadings++;
            Serial.printf("%d ✅\n", clearChannel);
            
            // Light Index - SOLO calcular y enviar si C >= 3000
            Serial.print("🌈 LightIndex: ");
            if (clearChannel >= 3000) {
                float lightIndex = readLightIndex();
                if (!isnan(lightIndex)) {
                    JsonObject lightReading = readings.createNestedObject();
                    lightReading["physicalId"] = "TCS34725-A1"; // TCS34725 sensor physical ID
                    lightReading["variableId"] = "688970837f02137645d58395"; // Light Index MongoDB ObjectId
                    lightReading["value"] = lightIndex;
                    validReadings++;
                    Serial.printf("%.1f%% ✅ (C=%d >= 3000)\n", lightIndex, clearChannel);
                } else {
                    Serial.printf("N/A (fallo en cálculo) ❌ (C=%d)\n", clearChannel);
                }
            } else {
                Serial.printf("OMITIDO (C=%d < 3000, oscuridad) ⚫\n", clearChannel);
            }
        } else {
            Serial.println("N/A (sensor TCS34725 falló) ❌");
        }
    } else {
        // FUERA DEL HORARIO (18:00-6:00): No enviar lecturas de luz
        Serial.println("🌙 Fuera de horario de luz (18:00-6:00) - omitiendo lecturas Clear e Index");
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
    
    // EC (Electrical Conductivity) - only add if sensor is working
    Serial.print("⚡ EC: ");
    float ecValue = readTDS();  // Function now returns EC in mS/cm
    if (!isnan(ecValue)) {
        JsonObject ecReading = readings.createNestedObject();
        ecReading["physicalId"] = "TDS-A1"; // TDS sensor physical ID (hardware ID remains same)
        ecReading["variableId"] = "688970a77f02137645d58397"; // EC MongoDB ObjectId 
        ecReading["value"] = ecValue;
        validReadings++;
        Serial.printf("%.2f mS/cm ✅\n", ecValue);
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

    // Water Level (HC-SR04) - only add if sensor is working
    Serial.print("💧 Water Level: ");
    float waterLevelValue = readWaterLevel();
    if (!isnan(waterLevelValue)) {
        JsonObject waterLevelReading = readings.createNestedObject();
        waterLevelReading["physicalId"] = "HC-SR04-A1"; // Ultrasonido sensor physical ID
        waterLevelReading["variableId"] = "68d1d07307c249cda4c369b0"; // Water Level MongoDB ObjectId
        waterLevelReading["value"] = waterLevelValue;
        validReadings++;
        Serial.printf("%.2f cm ✅\n", waterLevelValue);
    } else {
        Serial.println("N/A (sensor falló) ❌");
    }
    
    Serial.printf("📊 Total de lecturas válidas: %d/8\n", validReadings);
    
    // PhysicalId Mappings:
    // | Code        | physicalId       | Variable              | MongoDB ObjectId         | Status |
    // |-------------|------------------|-----------------------|--------------------------|--------|
    // | tcs34725-01 | TCS34725-A1      | Light Index           | 688970837f02137645d58395 | Active |
    // | tcs34725-02 | TCS34725-A1-CLEAR| Clear Channel         | 68d6dde25b8956ed967d6a8d | Active |
    // | dht22-001   | DHT22-A1         | Temperature           | 688970ab7f02137645d58398 | Active |
    // | dht22-001   | DHT22-A1         | Humidity              | 688970af7f02137645d58399 | Active |
    // | ph-001      | SEN0161-A1       | pH Level              | 688970a27f02137645d58396 | Active |
    // | tds-001     | TDS-A1           | Electrical Conduct.   | 688970a77f02137645d58397 | Active |
    // | ntc-001     | NTC-A1           | Water Temperature     | 68bb4d8cbdcb66fc5738f9af | Active |
    // | ultrasonido-01 | HC-SR04-A1       | Water Level           | 68d1d07307c249cda4c369b0 | Active |
    //
    // Note: All sensors now active and sending telemetry data
    // NTC Roots implementation removed as requested
}

// ADC Helper Functions
float SensorManager::readADCVoltage(int pin) {
    return readADCVoltageAveraged(pin, 10);  // Default 10 samples
}

float SensorManager::readADCVoltageAveraged(int pin, int samples) {
    long sum = 0;
    for (int i = 0; i < samples; i++) {
        sum += analogRead(pin);
        delay(5);  // Small delay between readings to reduce noise
    }
    
    int rawValue = sum / samples;
    float voltage = (rawValue / ADC_RESOLUTION) * ADC_VREF;
    
    // Protection against invalid voltages
    if (voltage < 0.01) voltage = 0.01;  // Prevent division by zero
    if (voltage > 3.2) voltage = 3.2;   // Clamp to reasonable max
    
    Serial.printf("[SENSOR] ADC Pin %d: raw=%d (avg %d samples), voltage=%.3fV\n", 
                  pin, rawValue, samples, voltage);
    return voltage;
}

// Calculate median from array of float values
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

// pH Sensor Reading (SEN0161 - Calibrated with median filtering)
float SensorManager::readPH() {
    const int NUM_SAMPLES = 15;  // Reducido para el sistema autónomo (vs 60 del test)
    const int SAMPLE_DELAY = 100;  // Reducido para el sistema autónomo (vs 500ms)
    
    // Constantes calibradas usando mediana de mediciones
    const float PH_SLOPE = 21.96f;
    const float PH_INTERCEPT = -16.08f;
    
    float voltages[NUM_SAMPLES];
    
    // Tomar muestras con delay
    for (int i = 0; i < NUM_SAMPLES; i++) {
        int adcRaw = analogRead(PIN_PH_ADC);
        float voltage = adcRaw * (ADC_VREF / ADC_RESOLUTION);
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
    
    Serial.printf("[SENSOR] pH: mediana=%.4fV pH=%.2f (calibrado)\n", 
                  medianVoltage, phValue);
    
    // Validar rango físico del pH (0-14)
    if (phValue < 0.0 || phValue > 14.0 || isnan(phValue)) {
        Serial.printf("[SENSOR] pH value fuera de rango físico: %.2f\n", phValue);
        return NAN;
    }
    
    return phValue;
}

// EC Sensor Reading (TDS Meter V1.0) - Returns EC in mS/cm for API/MQTT
float SensorManager::readTDS() {
    static int analogBuffer[TDS_SAMPLE_COUNT];
    static int analogBufferIndex = 0;
    static float calibrationFactor = 1.0;  // Adjustable calibration factor
    
    // Read ADC value
    int adcValue = analogRead(PIN_TDS_ADC);
    
    // Store in circular buffer
    analogBuffer[analogBufferIndex++] = adcValue;
    if (analogBufferIndex >= TDS_SAMPLE_COUNT) analogBufferIndex = 0;
    
    // Calculate average from buffer
    long avgValue = 0;
    for (int i = 0; i < TDS_SAMPLE_COUNT; i++) {
        avgValue += analogBuffer[i];
    }
    avgValue /= TDS_SAMPLE_COUNT;
    
    // Convert to voltage
    float voltage = (avgValue / 4095.0) * ADC_VREF;
    
    // TDS calculation using polynomial formula (TDS Meter V1.0)
    float tdsValue = (133.42 * pow(voltage, 3) 
                    - 255.86 * pow(voltage, 2) 
                    + 857.39 * voltage) * calibrationFactor;
    
    // Ensure valid TDS value for calculation
    if (tdsValue < 0) tdsValue = 0;
    if (isnan(tdsValue)) tdsValue = 0;
    
    // Convert TDS to EC: EC = TDS / 500 (standard conversion factor)
    float ecValue = tdsValue / 500.0f;
    
    // Validate EC range (0-10 mS/cm is physically reasonable)
    if (ecValue < 0.0f || ecValue > 10.0f) {
        Serial.printf("⚠️ [SENSOR] EC fuera de rango físico: %.3f mS/cm - descartando lectura\n", ecValue);
        return NAN;
    }
    
    // Log with both EC (for API) and TDS (for debugging/traceability)
    Serial.printf("⚡ [SENSOR] EC: ADC=%d avg=%ld V=%.3f EC=%.2f mS/cm | TDS estimado: %.0f ppm\n", 
                  adcValue, avgValue, voltage, ecValue, tdsValue);
    
    // CHANGE: Return EC in mS/cm for API/MQTT payload, not TDS
    return ecValue;
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
    
    // Only validate against realistic sensor limits (not optimal ranges)
    // Optimal ranges are handled by control system, not sensor readings
    if (isnan(temperature)) {
        Serial.printf("[SENSOR] %s temperature calculation failed\n", 
                     isTank ? "Tank" : "Roots");
        return NAN;
    }
    
    return temperature;
}

// Tank Temperature (NTC sensor)
float SensorManager::readTankTemperature() {
    // Use averaged readings for better stability
    float voltage = readADCVoltageAveraged(PIN_NTC_TANK, 15);  // More samples for NTC
    
    if (voltage < 0.05 || voltage > 3.3) {
        Serial.printf("[SENSOR] Tank NTC voltage fuera de rango funcional: %.3fV\n", voltage);
        return NAN;
    }
    
    // Improved resistance calculation with protection
    // Voltage divider: NTC connected between VCC and ADC pin, 10k resistor to ground
    // R_ntc = R_series * (Vcc / V_adc - 1)
    float resistance = 10000.0 * (ADC_VREF / voltage - 1.0);
    
    // Validate resistance range for sensor functionality (not optimal ranges)  
    if (resistance < 100.0 || resistance > 500000.0) {
        Serial.printf("[SENSOR] Tank NTC resistance fuera de rango funcional: %.1f ohms\n", resistance);
        return NAN;
    }
    
    float temperature = convertToTemperature(resistance, true);
    
    Serial.printf("[SENSOR] Tank temp: V=%.3f R=%.1fΩ T=%.2f°C\n", 
                  voltage, resistance, temperature);
    
    return temperature;
}

// Roots Temperature (NTC sensor) - REMOVED in autonomous version

// Función auxiliar para medición ultrasónica individual (basada en código de referencia)
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
    float distance = duration * 0.034 / 2.0;
    
    // Validate distance is within HC-SR04 functional range (según código de referencia)
    if (distance < 2 || distance > 400) return -1;
    
    return distance;
}

// Ultrasonic Water Level Sensor (HC-SR04) - Returns distance in cm for API/MQTT
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
    
    // Validate distance is within useful range (not too close to avoid false readings)
    if (distance < 2.0f) {
        Serial.printf("💧 Water Level: [SENSOR] Distancia muy pequeña (%.2fcm < 2cm) - posible ruido ❌\n", distance);
        return NAN;
    }
    
    // Calculate actual water level in cm (sensor mounted at top)
    // level = tank_height - distance_to_water_surface
    float waterLevel = TANK_HEIGHT_CM - distance;

    // Ensure valid range for water level (0 to tank height)
    if (waterLevel < 0.0f) waterLevel = 0.0f;
    if (waterLevel > TANK_HEIGHT_CM) waterLevel = TANK_HEIGHT_CM;

    // Calculate percentage for logging purposes only
    float waterLevelPercent = 100.0f * waterLevel / TANK_HEIGHT_CM;

    // Success log with measurement statistics
    Serial.printf("💧 Water Level: [SENSOR] Ultrasónico: mediciones válidas=%d/%d, distancia=%.2fcm, nivel=%.2fcm (%.1f%%) ✅\n",
                  validas, NUM_SAMPLES, distance, waterLevel, waterLevelPercent);

    // IMPORTANTE: Retornar DISTANCIA medida (no nivel de agua)
    // El backend necesita la distancia raw para la variable 68d1d07307c249cda4c369b0
    return distance;
}

// Función para verificar si debe enviar telemetría de luz (6:00-18:00)
bool SensorManager::isLightTelemetryActive() {
    if (!timeClient || !timeClient->isTimeSet()) {
        // Sin NTP disponible - permitir envío para compatibilidad
        static unsigned long lastWarning = 0;
        if (millis() - lastWarning > 300000) {  // Advertir cada 5 minutos
            Serial.println("⚠️ [SENSOR] NTP no disponible - enviando lecturas de luz sin restricción horaria");
            lastWarning = millis();
        }
        return true;
    }
    
    // Obtener hora actual en Colombia (UTC-5)
    int currentHour = timeClient->getHours();
    currentHour = (currentHour - 5 + 24) % 24;  // Convertir a UTC-5
    
    // Verificar si estamos en horario de luz (6:00-18:00)
    bool inSchedule = (currentHour >= 6 && currentHour < 18);
    
    Serial.printf("🕐 [SENSOR] Hora local: %02d:%02d - Telemetría luz: %s\n", 
                  currentHour, timeClient->getMinutes(), 
                  inSchedule ? "ACTIVA" : "INACTIVA");
    
    return inSchedule;
}