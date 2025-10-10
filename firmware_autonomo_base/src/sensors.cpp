#include "sensors.h"
#include "config.h"
#include "pins.h"

SensorManager::SensorManager(NTPClient* ntpClient) : dht(PIN_DHT22, DHT_TYPE),
                                 tcs(TCS34725_INTEGRATIONTIME_614MS, TCS34725_GAIN_1X),
                                 timeClient(ntpClient), dhtInitialized(false), bh1750Initialized(false), tcsInitialized(false) {
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

#if USE_TCS34725_LUX_FALLBACK
    // TEMPORAL: Inicializar TCS34725 para estimar lux
    if (tcs.begin()) {
        tcsInitialized = true;
        Serial.println("✅ TCS34725 inicializado (modo fallback lux)");
    } else {
        Serial.println("❌ Error inicializando TCS34725");
        tcsInitialized = false;
    }
#else
    // Initialize BH1750
    if (lightMeter.begin(BH1750::CONTINUOUS_HIGH_RES_MODE)) {
        bh1750Initialized = true;
        Serial.println("✅ BH1750 inicializado");
    } else {
        Serial.println("❌ Error inicializando BH1750");
        bh1750Initialized = false;
    }
#endif

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

// Light sensor - returns lux value
float SensorManager::readLightLux() {
#if USE_TCS34725_LUX_FALLBACK
    // TEMPORAL: Estimar lux desde TCS34725
    if (!tcsInitialized) return NAN;

    uint16_t r, g, b, c;

    // Timeout para evitar bloqueo en I2C
    unsigned long startTime = millis();
    const unsigned long timeout = 1000; // 1 segundo timeout

    // Intentar lectura con watchdog feed
    yield(); // Ceder antes de operación I2C
    tcs.getRawData(&r, &g, &b, &c);

    // Verificar si la operación tomó demasiado tiempo
    if (millis() - startTime > timeout) {
        Serial.println("❌ [SENSOR] TCS34725: Timeout en lectura I2C");
        return NAN;
    }

    // Fórmula empírica de Adafruit para estimar lux
    // Basada en la sensibilidad del sensor al espectro visible
    float lux = (-0.32466f * r) + (1.57837f * g) + (-0.73191f * b);

    if (lux < 0) lux = 0;
    lux = constrain(lux, 0, 65535);

    Serial.printf("[SENSOR] TCS34725→Lux (fallback): %.0f lux (R=%d G=%d B=%d C=%d)\n", lux, r, g, b, c);

    return lux;
#else
    // BH1750 nativo
    if (!bh1750Initialized) return NAN;

    float lux = lightMeter.readLightLevel();

    if (lux < 0) {
        Serial.println("❌ Error leyendo BH1750");
        return NAN;
    }

    Serial.printf("[SENSOR] BH1750: %.0f lux\n", lux);

    return lux;
#endif
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
    
    // LECTURAS DE LUZ CON RESTRICCIÓN HORARIA
    bool lightTelemetryActive = isLightTelemetryActive();

    if (lightTelemetryActive) {
        // DENTRO DEL HORARIO (5:00-19:00): Procesar lecturas de luz BH1750
        Serial.print("💡 Lux (BH1750): ");
        float lux = readLightLux();
        if (!isnan(lux)) {
            JsonObject luxReading = readings.createNestedObject();
            luxReading["physicalId"] = "BH1750-A1"; // BH1750 sensor physical ID
            luxReading["variableId"] = "688970837f02137645d58395"; // Light Lux MongoDB ObjectId
            luxReading["value"] = lux;
            validReadings++;
            Serial.printf("%.0f lux ✅\n", lux);
        } else {
            Serial.println("N/A (sensor BH1750 falló) ❌");
        }
    } else {
        // FUERA DEL HORARIO (19:00-5:00): No enviar lecturas de luz
        Serial.println("🌙 Fuera de horario de luz (19:00-5:00) - omitiendo lecturas de lux");
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
    // | BH1750-A1 | TCS34725-A1      | Lux                    | 688970837f02137645d58395 | Active |
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
        if (i % 5 == 0) yield(); // Ceder control cada 5 lecturas para evitar watchdog
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
    const int NUM_SAMPLES = 15;
    const int SAMPLE_DELAY = 50;  // Delay entre muestras

    // 📐 Calibración: 1.03 V -> pH 6.48 | 1.37 V -> pH 7.00
    const float PH_SLOPE = 1.529f;
    const float PH_INTERCEPT = 4.909f;

    float voltages[NUM_SAMPLES];

    // Tomar muestras con delay y yield para watchdog
    for (int i = 0; i < NUM_SAMPLES; i++) {
        int adcRaw = analogRead(PIN_PH_ADC);
        float voltage = (adcRaw * ADC_VREF) / ADC_RESOLUTION;
        voltages[i] = voltage;
        delay(SAMPLE_DELAY);
        if (i % 5 == 0) yield(); // Ceder control cada 5 muestras
    }

    // Calcular mediana para filtrar ruido (usa bubble sort interno)
    float medianVoltage = calculateMedian(voltages, NUM_SAMPLES);

    // Validar voltaje funcional
    if (medianVoltage < 0.05 || medianVoltage > 3.3) {
        Serial.printf("[SENSOR] pH voltage fuera de rango funcional: %.4fV\n", medianVoltage);
        return NAN;
    }
    
    // Calcular pH usando ecuación calibrada
    float phValue = PH_SLOPE * medianVoltage + PH_INTERCEPT;

    // Validar rango físico del pH (0-14)
    if (phValue < 0.0 || phValue > 14.0 || isnan(phValue)) {
        Serial.printf("⚠ [SENSOR] pH fuera de rango: %.2f | Voltaje: %.4f V\n", phValue, medianVoltage);
        return NAN;
    }

    Serial.printf("📊 [SENSOR] pH: Voltaje mediana=%.4fV | pH calibrado=%.2f\n",
                  medianVoltage, phValue);

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

// Función auxiliar para medición ultrasónica individual
float SensorManager::measureUltrasonicDistance() {
    // Asegurar que TRIG esté en LOW
    digitalWrite(PIN_ULTRA_TRIG, LOW);
    delayMicroseconds(2);

    // Enviar pulso de 10us al TRIG
    digitalWrite(PIN_ULTRA_TRIG, HIGH);
    delayMicroseconds(10);
    digitalWrite(PIN_ULTRA_TRIG, LOW);

    // Medir el tiempo que tarda en regresar el pulso
    // Timeout 30ms ≈ 5m máximo
    long duration = pulseIn(PIN_ULTRA_ECHO, HIGH, 30000);

    // Check for timeout
    if (duration == 0) return -1;

    // Calcular distancia en cm
    // Velocidad del sonido = 0.0343 cm/us, dividir por 2 por ida y vuelta
    float distance = (duration * 0.0343) / 2.0;

    // Validar rango funcional del HC-SR04 (2cm a 400cm)
    if (distance < 2 || distance > 400) return -1;

    return distance;
}

// Ultrasonic Sensor (HC-SR04) - Returns distance in cm
float SensorManager::readWaterLevel() {
    const int NUM_SAMPLES = 5;  // Número de mediciones para promedio

    float suma = 0;
    int validas = 0;

    // Tomar múltiples mediciones y promediar solo las válidas
    for (int i = 0; i < NUM_SAMPLES; i++) {
        float distance = measureUltrasonicDistance();
        if (distance > 0) {
            suma += distance;
            validas++;
        }
        delay(50); // Delay entre mediciones
        yield();   // Ceder control al watchdog entre mediciones
    }

    // Si no hay mediciones válidas
    if (validas == 0) {
        Serial.println("💧 [SENSOR] Ultrasónico: Sin lecturas válidas ❌");
        return NAN;
    }

    // Calcular distancia promediada
    float distance = suma / validas;

    // Validación básica de rango del sensor
    if (distance > 400.0f) {
        Serial.printf("💧 [SENSOR] Ultrasónico: Distancia fuera de rango (%.2fcm > 400cm) ❌\n", distance);
        return NAN;
    }

    if (distance < 2.0f) {
        Serial.printf("💧 [SENSOR] Ultrasónico: Distancia muy pequeña (%.2fcm < 2cm) - posible ruido ❌\n", distance);
        return NAN;
    }

    // Mostrar resultado
    Serial.printf("💧 [SENSOR] Ultrasónico: válidas=%d/%d, distancia=%.2fcm ✅\n",
                  validas, NUM_SAMPLES, distance);

    // RETORNAR DISTANCIA MEDIDA (en cm)
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

    // Verificar si estamos en horario de luz (5:00-19:00) - 14 horas
    bool inSchedule = (currentHour >= 5 && currentHour < 19);

    Serial.printf("🕐 [SENSOR] Hora local: %02d:%02d - Telemetría luz: %s\n",
                  currentHour, timeClient->getMinutes(),
                  inSchedule ? "ACTIVA" : "INACTIVA");

    return inSchedule;
}