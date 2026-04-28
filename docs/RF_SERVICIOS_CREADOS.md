# Requerimientos Funcionales — Servicios Creados

Servicios: **Weather**, **Chatbot** y **BI**

---

## Weather Service

---

**Tabla RF-W01:** Requerimiento funcional RF-W01

| **Identificación del requerimiento** | RF-W01 |
|---|---|
| **Nombre del requerimiento** | Consulta de condiciones meteorológicas actuales |
| **Descripción del requerimiento** | El sistema consultará la API de OpenWeather para obtener las condiciones meteorológicas en tiempo real correspondientes a la ubicación del invernadero (Mosquera, Cundinamarca), incluyendo temperatura, humedad, velocidad del viento, índice UV y descripción del estado del cielo. |

---

**Tabla RF-W02:** Requerimiento funcional RF-W02

| **Identificación del requerimiento** | RF-W02 |
|---|---|
| **Nombre del requerimiento** | Pronóstico meteorológico extendido |
| **Descripción del requerimiento** | El sistema obtendrá y almacenará en caché (por 30 minutos) un pronóstico completo que incluya las condiciones actuales, el pronóstico horario para las próximas 48 horas y el pronóstico diario para los próximos 8 días, con datos de temperatura mínima y máxima, humedad, precipitación, viento e índice UV. |

---

**Tabla RF-W03:** Requerimiento funcional RF-W03

| **Identificación del requerimiento** | RF-W03 |
|---|---|
| **Nombre del requerimiento** | Gestión de configuración de umbrales de alerta |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar y actualizar la configuración de umbrales de alerta asociada a un sistema fuzzy. Cada configuración definirá los tipos de alerta habilitados (lluvia intensa, calor extremo, frío extremo, viento fuerte, tormenta eléctrica, entre otros), su valor umbral, el operador de comparación, el número máximo de días de pronóstico a evaluar y si se permiten alertas duplicadas al mismo día. |

---

**Tabla RF-W04:** Requerimiento funcional RF-W04

| **Identificación del requerimiento** | RF-W04 |
|---|---|
| **Nombre del requerimiento** | Inicialización de configuración de alertas por defecto |
| **Descripción del requerimiento** | El sistema permitirá inicializar una configuración de alertas con valores por defecto para un sistema fuzzy que no tenga configuración previa, creando un conjunto base de umbrales recomendados para cultivos hidropónicos. |

---

**Tabla RF-W05:** Requerimiento funcional RF-W05

| **Identificación del requerimiento** | RF-W05 |
|---|---|
| **Nombre del requerimiento** | Evaluación automática periódica de alertas meteorológicas |
| **Descripción del requerimiento** | El sistema ejecutará de forma automática cada 30 minutos un proceso de evaluación que obtendrá el pronóstico más reciente, lo comparará contra los umbrales de todas las configuraciones activas, generará alertas cuando se supere algún umbral y las enviará a los usuarios suscritos a través del servicio de notificaciones. El proceso evaluará tanto el pronóstico diario (hasta el número de días configurado) como ventanas de lluvia acumulada en el pronóstico horario. |

---

**Tabla RF-W06:** Requerimiento funcional RF-W06

| **Identificación del requerimiento** | RF-W06 |
|---|---|
| **Nombre del requerimiento** | Deduplicación de alertas por usuario |
| **Descripción del requerimiento** | El sistema evitará el envío de alertas duplicadas al mismo usuario para el mismo tipo de evento y la misma fecha de pronóstico dentro de un ciclo de evaluación, registrando cada entrega en un log de deduplicación. Este comportamiento será configurable por sistema fuzzy mediante el campo de permiso de alertas repetidas. |

---

**Tabla RF-W07:** Requerimiento funcional RF-W07

| **Identificación del requerimiento** | RF-W07 |
|---|---|
| **Nombre del requerimiento** | Consulta y gestión de alertas generadas |
| **Descripción del requerimiento** | El sistema permitirá consultar el historial de alertas meteorológicas generadas, aplicando filtros por sistema fuzzy, usuario, rango de fechas y estado de lectura. Adicionalmente, permitirá marcar alertas individuales como leídas para un usuario específico. |

---
---

## Chatbot Service

---

**Tabla RF-C01:** Requerimiento funcional RF-C01

| **Identificación del requerimiento** | RF-C01 |
|---|---|
| **Nombre del requerimiento** | Gestión de sesiones de conversación |
| **Descripción del requerimiento** | El sistema permitirá crear, listar y eliminar sesiones de conversación por usuario. Cada sesión agrupará el historial de mensajes de una conversación independiente. El listado soportará paginación mediante parámetros de desplazamiento y límite. La eliminación archivará la sesión sin borrar el historial. |

---

**Tabla RF-C02:** Requerimiento funcional RF-C02

| **Identificación del requerimiento** | RF-C02 |
|---|---|
| **Nombre del requerimiento** | Consulta de historial de mensajes |
| **Descripción del requerimiento** | El sistema permitirá obtener el historial completo de mensajes de una sesión de conversación, incluyendo los mensajes del usuario y las respuestas del asistente en orden cronológico. |

---

**Tabla RF-C03:** Requerimiento funcional RF-C03

| **Identificación del requerimiento** | RF-C03 |
|---|---|
| **Nombre del requerimiento** | Respuesta conversacional con generación aumentada por recuperación (RAG) |
| **Descripción del requerimiento** | El sistema recibirá el mensaje del usuario, recuperará los fragmentos de conocimiento más relevantes de la base vectorial mediante búsqueda semántica y generará una respuesta contextualizada utilizando un modelo de lenguaje grande. La respuesta será transmitida al cliente token por token mediante Server-Sent Events (SSE) para una experiencia de escritura en tiempo real. |

---

**Tabla RF-C04:** Requerimiento funcional RF-C04

| **Identificación del requerimiento** | RF-C04 |
|---|---|
| **Nombre del requerimiento** | Sincronización y reindexación de la base de conocimiento |
| **Descripción del requerimiento** | El sistema recibirá notificaciones de cambios en entidades del servicio fuzzy (creación, actualización o eliminación de sistemas, variables y reglas) y actualizará automáticamente los vectores de conocimiento correspondientes. Adicionalmente, expondrá un endpoint para reindexar toda la base de conocimiento de forma manual cuando sea requerido por un administrador. Ambas operaciones serán accesibles únicamente mediante credenciales máquina a máquina. |

---
---

## BI Service

---

**Tabla RF-B01:** Requerimiento funcional RF-B01

| **Identificación del requerimiento** | RF-B01 |
|---|---|
| **Nombre del requerimiento** | Gestión de versiones de configuración de costos |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar y eliminar versiones de configuración de costos unitarios de recursos (electricidad por kWh, agua por litro, nutrientes por litro), definiendo el rango de fechas de vigencia de cada versión. Al crear una nueva versión, la versión anterior quedará desactivada automáticamente. La consulta soportará filtrado por rango de fechas y listará la versión activa actual. |

---

**Tabla RF-B02:** Requerimiento funcional RF-B02

| **Identificación del requerimiento** | RF-B02 |
|---|---|
| **Nombre del requerimiento** | Gestión de registros de consumo manual |
| **Descripción del requerimiento** | El sistema permitirá registrar, consultar y eliminar entradas de consumo manual de recursos (electricidad en kWh, agua en litros, nutrientes en litros), asociando cada entrada a un período de tiempo y un costo. La consulta soportará filtros por tipo de recurso y rango de fechas. Se dispondrá también de un endpoint de resumen que devolverá los totales agregados de consumo y costo para un período determinado. |

---

**Tabla RF-B03:** Requerimiento funcional RF-B03

| **Identificación del requerimiento** | RF-B03 |
|---|---|
| **Nombre del requerimiento** | Gestión de registros de producción |
| **Descripción del requerimiento** | El sistema permitirá registrar, consultar y eliminar ciclos de producción de cultivos, capturando el nombre del cultivo, las fechas de inicio y cosecha, los kilogramos producidos, el precio de venta por kilogramo y la moneda utilizada. |

---

**Tabla RF-B04:** Requerimiento funcional RF-B04

| **Identificación del requerimiento** | RF-B04 |
|---|---|
| **Nombre del requerimiento** | Cálculo de costos operacionales de actuadores |
| **Descripción del requerimiento** | El sistema calculará el costo eléctrico estimado asociado al funcionamiento de los actuadores del invernadero en un período dado, a partir de la duración de operación de cada actuador y su consumo en vatios, distribuido proporcionalmente según las versiones de configuración de costos vigentes en el período. |

---

**Tabla RF-B05:** Requerimiento funcional RF-B05

| **Identificación del requerimiento** | RF-B05 |
|---|---|
| **Nombre del requerimiento** | Análisis de rentabilidad de ciclo productivo |
| **Descripción del requerimiento** | El sistema calculará la rentabilidad de un ciclo de producción combinando los ingresos por venta (kilogramos producidos × precio), los costos operacionales de actuadores (opcionales) y los costos de consumo manual registrados. Entregará el beneficio neto, el margen de ganancia porcentual, el costo por kilogramo producido, la huella hídrica aproximada y, si se proporciona una inversión inicial, el retorno sobre la inversión (ROI). |

---
