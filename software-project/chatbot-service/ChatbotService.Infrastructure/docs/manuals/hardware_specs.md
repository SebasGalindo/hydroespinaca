# Especificaciones de Hardware

## Microcontrolador: ESP32-DevKitC v4

El sistema HydroEspinaca utiliza un módulo ESP32-DevKitC v4 como controlador principal del invernadero.

### Características principales
- **Procesador**: Xtensa dual-core LX6 a 240 MHz
- **RAM**: 520 KB SRAM
- **Flash**: 4 MB
- **Wi-Fi**: 802.11 b/g/n 2.4 GHz
- **Bluetooth**: BLE 4.2
- **GPIO**: 34 pines programables
- **ADC**: 18 canales (12-bit)
- **DAC**: 2 canales (8-bit)
- **PWM**: Hasta 16 canales

### Alimentación
- **USB**: 5V micro-USB para programación y alimentación
- **VIN**: 5V – 12V regulados internamente a 3.3V
- **Consumo típico**: 80 – 240 mA (según Wi-Fi activo)

## Sensores conectados

### DHT22 — Temperatura y Humedad Ambiente
- **Pines**: datos en GPIO 4
- **Rango temperatura**: -40 °C a 80 °C (±0.5 °C)
- **Rango humedad**: 0 a 100 % RH (±2 %)
- **Frecuencia de lectura**: cada 2 segundos mínimo
- **Protocolo**: one-wire digital

### EC Sensor — Conductividad Eléctrica
- **Modelo**: DFRobot Gravity Analog EC Sensor v2.0
- **Pin**: ADC GPIO 36
- **Rango**: 0 a 20 mS/cm (calibrable)
- **Resolución**: 0.01 mS/cm
- **Requiere**: sonda sumergida en la solución nutritiva

### pH Sensor — Acidez de la Solución
- **Modelo**: DFRobot Gravity Analog pH Sensor v2.0
- **Pin**: ADC GPIO 39
- **Rango**: 0 a 14 pH
- **Resolución**: 0.01 pH
- **Calibración**: 2 puntos (pH 4.0 y pH 7.0) con soluciones buffer

### LDR — Sensor de Luminosidad
- **Pin**: ADC GPIO 34
- **Tipo**: fotorresistencia con divisor de voltaje
- **Uso**: detección de presencia de luz natural (no lux calibrado)

### Sensor de Nivel de Agua
- **Pin**: GPIO 26 (digital)
- **Tipo**: interruptor de flotador
- **Función**: detectar nivel bajo del tanque de solución nutritiva

## Actuadores conectados

### Bomba de Agua (Riego NFT)
- **Pin de control**: GPIO 25 (relay)
- **Tipo**: relé 5V con flyback diode
- **Caudal**: 800 L/h
- **Modo**: encendido/apagado controlado por el sistema difuso

### Ventilador Principal (7)
- **Pin de control**: GPIO 27 (PWM)
- **Frecuencia PWM**: 25 kHz
- **Voltaje**: 12V DC, alimentado por fuente externa
- **Control**: intensidad variable 0 – 100 % vía duty cycle

### Extractor de Aire
- **Pin de control**: GPIO 14 (PWM)
- **Similar al ventilador**, pero ubicado en la salida del invernadero

### LED Grow Lights
- **Pin de control**: GPIO 12 (PWM)
- **Tipo**: tira LED Full Spectrum (rojo + azul)
- **Control**: intensidad variable 0 – 100 %
- **Consumo**: 30W máximo

## Comunicación MQTT

El ESP32 se conecta al broker Mosquitto vía MQTT con TLS.

### Topics principales
- `greenhouse/{device_id}/sensors` — publica lecturas de sensores (JSON)
- `greenhouse/{device_id}/actuators/command` — recibe comandos de actuadores
- `greenhouse/{device_id}/actuators/state` — publica estado actual de actuadores
- `greenhouse/{device_id}/status` — heartbeat cada 30 segundos

### Formato de mensaje de sensores
```json
{
  "temperature": 24.5,
  "humidity": 65.2,
  "ec": 2.1,
  "ph": 6.0,
  "light": 45000,
  "waterLevel": true,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Diagrama de conexión

| Componente        | Pin GPIO | Tipo   |
|-------------------|----------|--------|
| DHT22             | 4        | Digital|
| EC Sensor         | 36       | ADC    |
| pH Sensor         | 39       | ADC    |
| LDR               | 34       | ADC    |
| Nivel de agua     | 26       | Digital|
| Bomba de agua     | 25       | Relay  |
| Ventilador        | 27       | PWM    |
| Extractor         | 14       | PWM    |
| LED Grow          | 12       | PWM    |
