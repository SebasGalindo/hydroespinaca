# Justificación del uso de httpx vs requests

## ¿Por qué httpx en lugar de requests?

En el `fuzzy-service` utilizamos `httpx` como cliente HTTP en lugar de la tradicional librería `requests` por las siguientes razones técnicas:

### 1. Soporte nativo para operaciones asíncronas

- **httpx**: Soporta tanto operaciones síncronas como asíncronas de forma nativa
- **requests**: Solo soporta operaciones síncronas, requiere wrappers como `asyncio.run_in_executor()` para uso asíncrono

```python
# httpx - Nativo async
async with httpx.AsyncClient() as client:
    response = await client.post(url, json=data)

# requests - Requiere wrapper
import asyncio
loop = asyncio.get_event_loop()
response = await loop.run_in_executor(None, requests.post, url)
```

### 2. Soporte para HTTP/2

- **httpx**: Soporte nativo para HTTP/2, mejorando el rendimiento en conexiones múltiples
- **requests**: No soporta HTTP/2 de forma nativa

### 3. Mejor rendimiento en alta concurrencia

Según benchmarks de la comunidad, `httpx` maneja mejor las cargas de alta concurrencia que `requests` cuando se usa de forma asíncrona, especialmente importante para:

- Envío de múltiples comandos a actuadores simultáneamente
- Health checks periódicos al actuator-service
- Procesamiento de lotes grandes de lecturas de sensores

### 4. API compatible con requests

`httpx` mantiene una API muy similar a `requests`, facilitando la migración y el aprendizaje:

```python
# Sintaxis muy similar
response = httpx.get('https://api.example.com')
response = requests.get('https://api.example.com')
```

### 5. Mejor integración con FastAPI

FastAPI recomienda oficialmente el uso de `httpx` para clientes HTTP asincrónicos, asegurando mejor compatibilidad y soporte.

### 6. Timeouts más granulares

`httpx` ofrece control más granular sobre timeouts:

```python
timeout = httpx.Timeout(10.0, connect=5.0)
async with httpx.AsyncClient(timeout=timeout) as client:
    response = await client.post(url, json=data)
```

## Consideraciones

### Posibles desventajas

- **Madurez**: `requests` tiene más años en el mercado y una base de usuarios más amplia
- **Ecosistema**: Algunas librerías de terceros pueden estar más optimizadas para `requests`
- **Tamaño**: `httpx` puede tener un footprint ligeramente mayor

### Alternativas consideradas

- **aiohttp**: Excelente para alta concurrencia, pero con API diferente a requests
- **requests + asyncio**: Funcional pero menos eficiente para operaciones asíncronas

## Conclusión

Para el `fuzzy-service`, que requiere:
- Comunicación asíncrona con el actuator-service
- Procesamiento concurrente de múltiples comandos
- Integración nativa con FastAPI
- Rendimiento optimizado

`httpx` es la elección más adecuada, proporcionando las capacidades asíncronas necesarias sin sacrificar la familiaridad de la API de `requests`.