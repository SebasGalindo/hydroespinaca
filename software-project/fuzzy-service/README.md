# FuzzyService

Servicio de lógica difusa para el sistema HydroEspinaca que gestiona sistemas fuzzy, variables, reglas y rutinas para el control automatizado de cultivos hidropónicos.

## Características

- **Gestión de Sistemas Fuzzy**: CRUD completo para sistemas de lógica difusa
- **Variables y Términos Fuzzy**: Definición y gestión de variables lingüísticas
- **Reglas Fuzzy**: Creación y evaluación de reglas de inferencia
- **Rutinas Automatizadas**: Secuencias de evaluaciones programadas
- **Integración MQTT**: Escucha de sensores en tiempo real
- **Control de Actuadores**: Comunicación con servicio de actuadores
- **API REST**: Interfaz completa para gestión y consultas

## Arquitectura

El proyecto sigue los principios de Clean Architecture con las siguientes capas:

- **FuzzyService.Api**: Controladores REST y configuración de API
- **FuzzyService.Application**: Lógica de aplicación, comandos y consultas
- **FuzzyService.Domain**: Entidades de dominio e interfaces
- **FuzzyService.Infrastructure**: Persistencia y servicios externos

## Tecnologías

- **FastAPI**: Framework web moderno y rápido
- **MongoDB**: Base de datos NoSQL para persistencia
- **MQTT**: Comunicación con sensores IoT
- **scikit-fuzzy**: Biblioteca de lógica difusa
- **Pydantic**: Validación y serialización de datos
- **MediatR**: Patrón mediador para CQRS

## Instalación

1. Instalar dependencias:
```bash
pip install -r requirements.txt
```

2. Configurar variables de entorno:
```bash
cp .env.example .env
# Editar .env con la configuración apropiada
```

3. Ejecutar el servicio:
```bash
uvicorn FuzzyService.Api.main:app --reload
```

## Configuración

### Variables de Entorno

- `MONGODB_CONNECTION_STRING`: Cadena de conexión a MongoDB
- `MQTT_BROKER_HOST`: Host del broker MQTT
- `MQTT_BROKER_PORT`: Puerto del broker MQTT
- `ACTUATOR_SERVICE_URL`: URL del servicio de actuadores
- `LOG_LEVEL`: Nivel de logging (DEBUG, INFO, WARNING, ERROR)

## API Endpoints

### Sistemas Fuzzy
- `GET /api/fuzzy-systems` - Listar sistemas
- `GET /api/fuzzy-systems/{id}` - Obtener sistema por ID
- `POST /api/fuzzy-systems` - Crear sistema
- `PUT /api/fuzzy-systems/{id}` - Actualizar sistema
- `DELETE /api/fuzzy-systems/{id}` - Eliminar sistema

### Variables Fuzzy
- `GET /api/fuzzy-variables` - Listar variables
- `POST /api/fuzzy-variables` - Crear variable
- `PUT /api/fuzzy-variables/{id}` - Actualizar variable

### Reglas Fuzzy
- `GET /api/fuzzy-rules` - Listar reglas
- `POST /api/fuzzy-rules` - Crear regla
- `PUT /api/fuzzy-rules/{id}` - Actualizar regla

### Evaluaciones
- `GET /api/fuzzy-evaluations` - Listar evaluaciones
- `GET /api/fuzzy-evaluations/{id}` - Obtener evaluación

## Desarrollo

### Estructura del Proyecto

```
FuzzyService/
├── FuzzyService.Api/          # Capa de presentación
│   ├── Controllers/           # Controladores REST
│   ├── Middleware/           # Middleware personalizado
│   └── Configuration/        # Configuración de API
├── FuzzyService.Application/  # Capa de aplicación
│   ├── Features/             # Comandos y consultas por entidad
│   ├── Services/             # Servicios de aplicación
│   └── Common/               # Utilidades comunes
├── FuzzyService.Domain/       # Capa de dominio
│   ├── Entities/             # Entidades de dominio
│   ├── Interfaces/           # Contratos de repositorios
│   └── ValueObjects/         # Objetos de valor
└── FuzzyService.Infrastructure/ # Capa de infraestructura
    ├── Persistence/          # Repositorios y contexto de datos
    ├── ExternalServices/     # Servicios externos (MQTT, HTTP)
    └── Configuration/        # Configuración de infraestructura
```

### Ejecutar Tests

```bash
pytest
```

### Linting y Formato

```bash
black .
flake8 .
```

## Licencia

Este proyecto está bajo la Licencia MIT.