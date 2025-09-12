# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ESP32 firmware v2.0 for the HydroEspinaca hydroponic system. The firmware manages sensors and executes parallel job routines for automated plant cultivation, communicating with a backend system via MQTT with robust scheduling and notification capabilities.

## Architecture

The codebase follows a modular C++ design with FreeRTOS multi-tasking:

- **SensorManager** (`sensors.h/cpp`): Handles DHT22 temperature/humidity and BH1750 light sensor with I2C. Returns NaN for failed sensors.
- **JobScheduler** (`job_scheduler.h/cpp`): Core scheduling system with 3 parallel channels, job consolidation, queuing, and control routines
- **MQTTHandler** (`mqtt_handler.h/cpp`): Manages WiFi/MQTT connectivity with exponential backoff, QoS handling, telemetry buffering, and LWT

Key workflow: 
- Telemetry published every 60s (only working sensors included)
- Job schedules received via MQTT trigger parallel execution
- Consolidation/queuing decisions published as notifications  
- Completed jobs published with execution logs
- Emergency stop and watchdog protection

## Development Commands

This project uses PlatformIO for ESP32 development:

```bash
# Build firmware
pio run

# Upload to ESP32
pio run --target upload

# Monitor serial output
pio device monitor

# Build and upload in one command
pio run --target upload && pio device monitor

# Clean build
pio run --target clean
```

## Configuration

- Pin assignments and scheduler constants in `include/config.h`
- WiFi/MQTT credentials in `include/secrets.h` (copy from `secrets.example.h`)
- ESP32 ID: `6883fff7b079309f3ba4f238`, Client ID: `esp32-001`

## MQTT Topics & Payloads

- **Telemetry** (QoS 0): `sensor/esp32-001/readings`
- **Job Schedule** (QoS 1): `actuator/job/schedule` 
- **Status/LWT** (retained): `sensor/esp32-001/status`
- **Completions** (QoS 1): `actuator/routine/completions`
- **Notifications** (QoS 1): `actuator/routine/notifications`

## Job Scheduler Rules

- 3 parallel channels (FreeRTOS tasks)
- **Control routine**: power:OFF or dutyCycle:0 cancels jobs on same pin
- **Consolidation**: same baseId + step 0 → merge parameters, log changes
- **Queuing**: step > 0 → enqueue with notification
- Step states: pending → in_progress → (ok|cancelled|error)
- 500ms timeout tolerance per step

## Hardware Interface

**Active**: DHT22 (pin 4), BH1750 I2C (SDA:21, SCL:22), Fan PWM (pin 16)
**Stubbed**: pH, EC, water temp, water level, heater, broad spectrum light, air stone, water pump