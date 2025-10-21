# HydroEspinaca

Sistema distribuido de control inteligente para cultivos hidropónicos basado en microservicios, lógica difusa y comunicación IoT.

Este proyecto es un trabajo de grado desarrollado por estudiantes de la **Universidad de Cundinamarca**. Integra hardware IoT (ESP32), procesamiento de datos con lógica difusa Mamdani, y una arquitectura de microservicios moderna para el control autónomo de variables ambientales en cultivos hidropónicos.

## 👥 Equipo de Desarrollo

**Proyecto 1: Hardware**
- Juan David Moreno Beltrán
- Julian David Lara Beltrán

**Proyecto 2: Software**
- John Sebastián Galindo Hernández
- Miguel Ángel Moreno Beltrán

---

## 📘 Descripción General

HydroEspinaca es un ecosistema completo para el monitoreo y control automatizado de cultivos hidropónicos. El sistema:

- Recolecta datos de sensores (temperatura, humedad, pH, EC, nivel de agua, luminosidad) desde nodos ESP32
- Procesa las lecturas mediante un motor de lógica difusa que evalúa reglas Mamdani
- Genera comandos automáticos para actuadores (bombas, ventiladores, luces, humidificadores)
- Administra usuarios, roles y permisos con autenticación JWT centralizada
- Provee interfaces web y móvil para monitoreo en tiempo real
- Envía notificaciones por email para alertas críticas

**Tecnologías principales:** .NET 9, Python 3.12, FastAPI, MongoDB, MQTT, React Native, Docker

---

## 🧱 Estructura General

```
hydroespinaca/
├── hardware-project/           # Servicios de control de hardware IoT
│   ├── actuator-service/       # Control de actuadores (bombas, ventiladores, etc.)
│   └── sensor-service/         # Gestión de sensores y lecturas
│
├── software-project/           # Servicios de aplicación y lógica de negocio
│   ├── auth-service/           # Autenticación y autorización (JWT, roles, permisos)
│   ├── bff-service/            # Backend for Frontend (API Gateway para apps)
│   ├── fuzzy-service/          # Motor de lógica difusa (Python + scikit-fuzzy)
│   └── notification-service/   # Envío de emails y notificaciones
│
├── shared/                     # Librería compartida .NET
│   └── HydroEspinaca.Shared/   # DTOs, autenticación JWT, extensiones, MQTT, MongoDB
│
├── apps/                       # Aplicaciones frontend
│   ├── web/                    # Aplicación web (React)
│   └── mobile/                 # Aplicación móvil (React Native)
│
├── esp32-firmware/             # Firmware para nodos ESP32
├── firmware_autonomo_base/     # Firmware base para ESP32
├── deploy-artifacts/           # Scripts de despliegue y configuración
├── tools/                      # Herramientas de desarrollo
├── docker-compose.yml          # Orquestación de servicios
└── .env                        # Variables de entorno (no versionado)
```

---

## 🔗 Microservicios

### Hardware Project

#### **Sensor Service** (.NET 9)
Gestiona sensores IoT y procesa lecturas en tiempo real desde nodos ESP32 vía MQTT.

- Procesamiento de lecturas de sensores (PH, temperatura, humedad, EC, nivel de agua)
- Generación de alertas críticas basadas en umbrales físicos y óptimos
- Cálculo de agregaciones estadísticas (promedios, tendencias)
- Monitoreo de salud de nodos ESP32 (heartbeat, detección offline)

[📖 Ver documentación completa](./hardware-project/sensor-service/README.md)

#### **Actuator Service** (.NET 9)
Ejecuta comandos sobre actuadores físicos conectados a ESP32.

- Coordinación de comandos desde múltiples fuentes (fuzzy logic, rutinas internas, manual)
- Gestión de rutinas programadas por tiempo (time-based scheduling)
- Reglas de seguridad (pin locking, max runtime, cooldowns)
- Máquina de estados de actuadores (ON/OFF/PWM) en tiempo real

[📖 Ver documentación completa](./hardware-project/actuator-service/README.md)

---

### Software Project

#### **Auth Service** (.NET 9)
Centraliza autenticación y autorización para todo el ecosistema.

- Autenticación de usuarios (Resource Owner Password) con gestión de sesiones
- Autenticación M2M (Client Credentials) para comunicación entre microservicios
- JWT con claves RSA asimétricas (validación descentralizada vía JWKS)
- Sistema RBAC (roles y permisos granulares)
- Reset de contraseña vía email con códigos de verificación

[📖 Ver documentación completa](./software-project/auth-service/README.md)

#### **BFF Service** (.NET 9)
Backend for Frontend que orquesta llamadas a microservicios y expone API simplificada para apps.

- Gestión de sesiones web y móvil (cookies + headers)
- Refresco automático de tokens JWT
- Agregación de datos de múltiples servicios (composition pattern)
- Caché de consultas recurrentes (permisos, analíticas, clima)
- Integración con OpenWeather API

[📖 Ver documentación completa](./software-project/bff-service/README.md)

#### **Fuzzy Service** (Python 3.12 + FastAPI)
Motor de lógica difusa Mamdani para control autónomo de variables ambientales.

- Evaluación de reglas fuzzy con scikit-fuzzy
- Generación de comandos normalizados para actuadores
- APIs REST completas para sistemas, variables, términos y reglas
- Historial de evaluaciones con estadísticas agregadas
- Procesamiento de lecturas de sensores en tiempo real

[📖 Ver documentación completa](./software-project/fuzzy-service/README.md)

#### **Notification Service** (.NET 9)
Sistema de notificaciones por email con idempotencia y resiliencia.

- Envío de emails a destinatarios individuales o grupos predefinidos
- Cola asíncrona en memoria con BackgroundService
- Soporte multi-proveedor (Resend + SMTP fallback)
- Sanitización de HTML para prevenir XSS
- Auditoría completa de envíos en MongoDB

[📖 Ver documentación completa](./software-project/notification-service/README.md)

---

### Shared Library

#### **HydroEspinaca.Shared** (.NET 9)
Librería NuGet local con tipos, utilidades y configuración común para todos los microservicios .NET.

- DTOs compartidos entre microservicios
- Autenticación JWT y autorización basada en scopes
- Extensiones de configuración para MongoDB, MQTT, Swagger
- BaseMongoRepository genérico y servicios base
- Constantes del sistema (roles, topics MQTT, enums)

[📖 Ver documentación completa](./shared/HydroEspinaca.Shared/README.md)

---

## ⚙️ Flujo de Operación

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│   ESP32      │ ──MQTT──│    Sensor    │ ──HTTP──│    Fuzzy     │
│  (Hardware)  │         │   Service    │         │   Service    │
└──────────────┘         └──────────────┘         └──────────────┘
                                │                         │
                                │                         │
                                ▼                         ▼
                         ┌──────────────┐         ┌──────────────┐
                         │   MongoDB    │         │   Actuator   │
                         │ (Readings DB)│         │   Service    │
                         └──────────────┘         └──────────────┘
                                                          │
                                                          │ MQTT
                                                          ▼
                                                   ┌──────────────┐
                                                   │   ESP32      │
                                                   │ (Actuadores) │
                                                   └──────────────┘

                         ┌──────────────┐
                         │     Auth     │◄──────── JWT + M2M
                         │   Service    │         (Todos los servicios)
                         └──────────────┘
                                ▲
                                │
                         ┌──────────────┐
                         │     BFF      │◄──────── Frontend (Web/Móvil)
                         │   Service    │
                         └──────────────┘
```

**Flujo principal:**

1. **Sensores → MQTT → Sensor Service:** Los nodos ESP32 publican lecturas al broker MQTT
2. **Sensor Service → MongoDB:** Almacena lecturas, genera alertas según umbrales
3. **Sensor Service → Fuzzy Service:** Envía lecturas para evaluación de lógica difusa
4. **Fuzzy Service → Actuator Service:** Genera y envía comandos normalizados
5. **Actuator Service → MQTT → Actuadores:** Publica comandos a los nodos ESP32
6. **Frontend → BFF → Servicios:** Las apps consumen el BFF que agrega datos de los micros
7. **Auth Service → JWKS:** Todos los servicios validan tokens usando claves públicas

---

## 🚀 Inicio Rápido

### Requisitos

- Docker 20.10+ y Docker Compose 2.0+
- WSL2 (Windows) o Linux nativo
- .NET 9 SDK (solo para desarrollo local)
- Node.js 18+ y pnpm (solo para frontend)
- Python 3.12+ (solo para fuzzy-service local)

### Ejecución con Docker Compose

```bash
# 1. Clonar el repositorio
git clone https://github.com/youruser/hydroespinaca.git
cd hydroespinaca

# 2. Configurar variables de entorno
cp .env.test .env
# Editar .env con tus valores (MongoDB, MQTT, JWT secrets, etc.)

# 3. Generar claves RSA para JWT (si no existen)
mkdir -p software-project/auth-service/Keys
cd software-project/auth-service/Keys
openssl genrsa -out user-private.pem 2048
openssl rsa -in user-private.pem -pubout -out user-public.pem
openssl genrsa -out machinetomachine-private.pem 2048
openssl rsa -in machinetomachine-private.pem -pubout -out machinetomachine-public.pem
cd ../../..

# 4. Iniciar servicios
docker compose up --build

# 5. Verificar estado de los servicios
docker compose ps
```

### Endpoints principales

| Servicio | URL | Documentación |
|----------|-----|---------------|
| Auth Service | http://localhost:5018 | http://localhost:5018/swagger |
| Sensor Service | http://localhost:8080 | http://localhost:8080/swagger |
| Actuator Service | http://localhost:5063 | http://localhost:5063/swagger |
| Fuzzy Service | http://localhost:8000 | http://localhost:8000/docs |
| BFF Service | http://localhost:8081 | http://localhost:8081/swagger |
| Notification Service | http://localhost:5285 | http://localhost:5285/swagger |
| MongoDB | mongodb://localhost:27017 | - |
| MQTT Broker | mqtt://localhost:1883 | - |

### Desarrollo Local

Para desarrollo local de un microservicio específico, consulta el README individual de cada servicio.

**Librería compartida (.NET):**

```bash
# Build y empaquetado de HydroEspinaca.Shared
cd shared/HydroEspinaca.Shared
dotnet build
dotnet pack --configuration Release

# Actualizar en todos los microservicios
cd ../..
./build-shared-and-install.sh
```

---

## 🔐 Autenticación

Todos los microservicios usan **JWT Bearer tokens** emitidos por Auth Service:

- **User tokens:** Para usuarios web/móvil (RS256 con clave `user-{date}`)
- **M2M tokens:** Para comunicación entre servicios (RS256 con clave `m2m-{date}`)

Los servicios validan tokens **localmente** mediante JWKS (JSON Web Key Set) sin necesidad de consultar auth-service en cada request.

**Scopes principales:**

- `system:admin` - Acceso total (bypass de todas las políticas)
- `user:*`, `role:*`, `permission:*` - Gestión de identidad
- `sensor:*`, `actuator:*` - Control de hardware
- `fuzzy:*` - Gestión de lógica difusa
- `notification:*` - Envío de notificaciones

---

## 📝 Notas de Producción

### Seguridad

- **JWT Keys:** Rotar claves RSA cada 90 días. Almacenar en Kubernetes Secrets, nunca en código.
- **Client Secrets M2M:** Los secrets del seeding son SOLO para desarrollo. Cambiar antes de producción.
- **MongoDB:** Habilitar autenticación (`MONGO_INITDB_ROOT_USERNAME`, `MONGO_INITDB_ROOT_PASSWORD`).
- **MQTT:** Habilitar TLS y autenticación en el broker Mosquitto.
- **Rate Limiting:** Configurar límites en nginx o API Gateway antes de producción.

### Escalabilidad

- **BFF Service:** La cola de emails actual es in-memory. Migrar a Redis/RabbitMQ para múltiples réplicas.
- **Sensor Service:** Configurar índices MongoDB para lecturas: `(sensorId, variableCode, timestamp)`.
- **Caché:** Considerar Redis para caché distribuido en lugar de MemoryCache.

### Monitoreo

- Configurar health checks en todos los servicios (`/health`)
- Exportar métricas con Prometheus (login attempts, token generation, command execution)
- Centralizar logs con Serilog + ELK Stack
- Alertas de MongoDB (disk usage, replication lag)
- Alertas de MQTT (broker offline, mensajes perdidos)

---

## 📖 Documentación Adicional

- [HydroEspinaca.Shared](./shared/HydroEspinaca.Shared/README.md) - Librería compartida
- [Actuator Service](./hardware-project/actuator-service/README.md) - Control de actuadores
- [Sensor Service](./hardware-project/sensor-service/README.md) - Gestión de sensores
- [Auth Service](./software-project/auth-service/README.md) - Autenticación y autorización
- [BFF Service](./software-project/bff-service/README.md) - Backend for Frontend
- [Fuzzy Service](./software-project/fuzzy-service/README.md) - Motor de lógica difusa
- [Notification Service](./software-project/notification-service/README.md) - Sistema de notificaciones

---

## 📜 Licencia

MIT - Libre para investigación, modificación y uso educativo.

**Stack Principal:** .NET 9, Python 3.12, FastAPI, MongoDB, MQTT, React, React Native, Docker
**Última actualización:** 2025-10-21
