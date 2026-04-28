# Requerimientos Funcionales — Servicios Actualizados

Servicios: **Notification** y **Fuzzy**

---

## Notification Service

---

**Tabla RF-N01:** Requerimiento funcional RF-N01

| **Identificación del requerimiento** | RF-N01 |
|---|---|
| **Nombre del requerimiento** | Envío de notificaciones multicanal |
| **Descripción del requerimiento** | El sistema enviará notificaciones a los usuarios a través de los canales habilitados en sus preferencias (correo electrónico, notificación push móvil o web, y WhatsApp). El envío multicanal agrupará todos los canales activos del usuario en una sola solicitud, garantizando entrega coordinada. El canal de correo electrónico soportará clave de idempotencia para evitar envíos duplicados. |

---

**Tabla RF-N02:** Requerimiento funcional RF-N02

| **Identificación del requerimiento** | RF-N02 |
|---|---|
| **Nombre del requerimiento** | Gestión de grupos de notificación |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar y eliminar grupos de notificación, que representan listas de destinatarios con nombre para el envío de comunicaciones colectivas. |

---

**Tabla RF-N03:** Requerimiento funcional RF-N03

| **Identificación del requerimiento** | RF-N03 |
|---|---|
| **Nombre del requerimiento** | Gestión de preferencias de notificación por usuario |
| **Descripción del requerimiento** | El sistema permitirá consultar y actualizar las preferencias de notificación de cada usuario, incluyendo los canales habilitados (correo, push, WhatsApp), la activación del resumen diario, el horario de silencio (quiet hours) y la suscripción a alertas meteorológicas de sistemas fuzzy específicos. |

---

**Tabla RF-N04:** Requerimiento funcional RF-N04

| **Identificación del requerimiento** | RF-N04 |
|---|---|
| **Nombre del requerimiento** | Gestión de suscripciones push |
| **Descripción del requerimiento** | El sistema permitirá registrar, consultar y eliminar suscripciones de notificación push asociadas a dispositivos de usuario (tokens Expo para móvil o Web Push para navegador), identificando la plataforma y el nombre del dispositivo para facilitar la gestión de múltiples dispositivos por usuario. |

---

**Tabla RF-N05:** Requerimiento funcional RF-N05

| **Identificación del requerimiento** | RF-N05 |
|---|---|
| **Nombre del requerimiento** | Generación y envío automático de resumen diario |
| **Descripción del requerimiento** | El sistema ejecutará diariamente un proceso programado que recopilará datos de las últimas 24 horas de los servicios de sensores, actuadores, sistema fuzzy y clima, generará un resumen en formato HTML y texto plano, y lo enviará a los usuarios que tengan habilitada esta funcionalidad y no se encuentren en horario de silencio. |

---

**Tabla RF-N06:** Requerimiento funcional RF-N06

| **Identificación del requerimiento** | RF-N06 |
|---|---|
| **Nombre del requerimiento** | Consulta de historial de notificaciones |
| **Descripción del requerimiento** | El sistema permitirá consultar el historial de notificaciones enviadas a un usuario, con soporte para filtros por canal, estado de entrega y rango de fechas, permitiendo al usuario revisar comunicaciones anteriores y verificar el estado de entrega de cada una. |

---
---

## Fuzzy Service

---

**Tabla RF-F01:** Requerimiento funcional RF-F01

| **Identificación del requerimiento** | RF-F01 |
|---|---|
| **Nombre del requerimiento** | Gestión de sistemas fuzzy |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar, activar y eliminar sistemas de lógica difusa. El listado soportará paginación y filtros por nombre, estado activo y rango de fechas de creación. La activación de un sistema desactivará automáticamente cualquier otro sistema previamente activo, garantizando que solo uno esté en ejecución en todo momento. La consulta individual devolverá el sistema con todas sus variables y reglas asociadas. |

---

**Tabla RF-F02:** Requerimiento funcional RF-F02

| **Identificación del requerimiento** | RF-F02 |
|---|---|
| **Nombre del requerimiento** | Gestión de variables fuzzy |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar y eliminar variables de lógica difusa (de tipo entrada o salida), definiendo su nombre, valores mínimo y máximo, referencia a la variable de sensor o actuador correspondiente, y los términos lingüísticos asociados. Se dispondrá de consulta por sistema y de endpoints específicos para agregar o remover términos de una variable existente. |

---

**Tabla RF-F03:** Requerimiento funcional RF-F03

| **Identificación del requerimiento** | RF-F03 |
|---|---|
| **Nombre del requerimiento** | Gestión de términos fuzzy |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar y eliminar términos lingüísticos (como "bajo", "medio", "alto") asociados a variables fuzzy, definiendo el tipo de función de pertenencia (triangular, trapezoidal, gaussiana, entre otras) y sus parámetros de forma. |

---

**Tabla RF-F04:** Requerimiento funcional RF-F04

| **Identificación del requerimiento** | RF-F04 |
|---|---|
| **Nombre del requerimiento** | Gestión de reglas fuzzy |
| **Descripción del requerimiento** | El sistema permitirá crear, consultar, actualizar y eliminar reglas de inferencia fuzzy, compuestas por condiciones antecedentes (variable-término), conectores lógicos (AND/OR) y consecuentes (variable de salida con término y peso). Se dispondrá de endpoints específicos para modificar condiciones individuales, los conectores lógicos de la regla y el consecuente de forma independiente. |

---

**Tabla RF-F05:** Requerimiento funcional RF-F05

| **Identificación del requerimiento** | RF-F05 |
|---|---|
| **Nombre del requerimiento** | Evaluación y persistencia de inferencias fuzzy |
| **Descripción del requerimiento** | El sistema ejecutará el motor de inferencia fuzzy (método Mamdani) sobre el sistema activo a partir de los valores de entrada proporcionados, determinará las reglas disparadas, calculará los valores de salida defuzzificados y persistirá el resultado de la evaluación con sus entradas, reglas activadas, salidas y marca temporal. |

---

**Tabla RF-F06:** Requerimiento funcional RF-F06

| **Identificación del requerimiento** | RF-F06 |
|---|---|
| **Nombre del requerimiento** | Simulación de inferencia fuzzy sin persistencia |
| **Descripción del requerimiento** | El sistema permitirá ejecutar el motor de inferencia fuzzy con valores de entrada arbitrarios sobre un sistema específico sin almacenar el resultado, con el propósito de verificar el comportamiento de las reglas durante la fase de diseño y ajuste del sistema. |

---

**Tabla RF-F07:** Requerimiento funcional RF-F07

| **Identificación del requerimiento** | RF-F07 |
|---|---|
| **Nombre del requerimiento** | Consulta de historial y estadísticas de evaluaciones |
| **Descripción del requerimiento** | El sistema permitirá consultar el historial de evaluaciones fuzzy con filtros por sistema, rango de fechas y ordenamiento configurable, con soporte de paginación. Dispondrá de un endpoint de evaluaciones recientes (últimas 24 horas por defecto, configurable hasta 7 días) y un endpoint de estadísticas que entregará conteos por sistema, totales diarios y promedios de resultados. |

---

**Tabla RF-F08:** Requerimiento funcional RF-F08

| **Identificación del requerimiento** | RF-F08 |
|---|---|
| **Nombre del requerimiento** | Clonado, exportación e importación de sistemas fuzzy |
| **Descripción del requerimiento** | El sistema permitirá crear una copia completa de un sistema fuzzy existente incluyendo todas sus variables, términos y reglas (clonado profundo). Adicionalmente, permitirá exportar un sistema a formato JSON portable e importar sistemas desde ese mismo formato, facilitando la transferencia de configuraciones entre entornos. |

---
