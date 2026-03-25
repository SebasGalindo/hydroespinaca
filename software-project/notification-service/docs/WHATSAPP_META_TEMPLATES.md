# Plantillas de WhatsApp — Meta Cloud API

Guía completa para crear y configurar las plantillas de mensajes de WhatsApp Business
que utiliza el microservicio de notificaciones de HydroEspinaca.

> **Importante**: Todas las plantillas se crean manualmente desde el
> [Administrador de WhatsApp de Meta](https://business.facebook.com/latest/whatsapp_manager/message_templates).
> Una vez aprobadas por Meta, el código las referencia por nombre internamente.

---

## Convenciones generales

| Campo | Valor |
|-------|-------|
| **Categoría** | UTILITY |
| **Idioma** | Spanish (COL) |
| **Pie de página (todas)** | `HydroEspinaca 🌱 — Tu asistente de invernadero` |
| **Tipo de variable** | Todas las variables son de tipo **Texto** |

Las variables `{{N}}` de Meta son posicionales (1, 2, 3…). Cuando un dato no está
disponible, el código enviará `"Sin datos"` o `"N/A"` según corresponda.

---

## 1. `resumen_diario_invernadero`

Enviada una vez al día según la hora configurada por el usuario.
Contiene un resumen completo de sensores, actuadores, sistema fuzzy y pronóstico.

### Encabezado (texto)

```
📊 Resumen diario de tu invernadero — {{1}}
```

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `{{1}}` | Fecha del resumen | `04/03/2026` |

### Cuerpo

```
¡Hola! 👋 Aquí tienes el resumen de lo que pasó en tu invernadero durante las últimas 24 horas.

🌡️ *Temperatura ambiente*
Promedio: {{1}}°C - Mínima: {{2}}°C - Máxima: {{3}}°C

💧 *Humedad relativa*
Promedio: {{4}}% - Mínima: {{5}}% - Máxima: {{6}}%

💡 *Luminosidad*
Promedio: {{7}} lux - Mínima: {{8}} lux - Máxima: {{9}} lux

🧪 *pH de la solución*
Promedio: {{10}} - Mínima: {{11}} - Máxima: {{12}}

🌊 *Temperatura del tanque*
Promedio: {{13}}°C - Mínima: {{14}}°C - Máxima: {{15}}°C

📏 *Nivel de agua del tanque*
Promedio: {{16}} L - Mínima: {{17}} L - Máxima: {{18}} L

⚙️ *Actuadores — tiempo activo*
🌀 Ventiladores: {{19}} min
🔥 Termoventilador: {{20}} min
💡 Luz amplio espectro: {{21}} min
🫧 Piedra difusora: {{22}} min
💧 Bomba de agua: {{23}} min
♨️ Calefactor de agua: {{24}} min
🌫️ Humidificador: {{25}} min

🧠 *Sistema Fuzzy* ({{26}})
Evaluaciones realizadas: {{27}}

Puedes consutar a detalle en la plataforma.
```

| Variable | Descripción | Ejemplo con datos | Ejemplo sin datos |
|----------|-------------|-------------------|-------------------|
| `{{1}}` | Temp. ambiente promedio | `24.5` | `Sin datos` |
| `{{2}}` | Temp. ambiente mínima | `20.1` | `--` |
| `{{3}}` | Temp. ambiente máxima | `29.3` | `--` |
| `{{4}}` | Humedad relativa promedio | `78` | `Sin datos` |
| `{{5}}` | Humedad relativa mínima | `65` | `--` |
| `{{6}}` | Humedad relativa máxima | `92` | `--` |
| `{{7}}` | Luminosidad promedio (lux) | `12450` | `Sin datos` |
| `{{8}}` | Luminosidad mínima (lux) | `3200` | `--` |
| `{{9}}` | Luminosidad máxima (lux) | `28500` | `--` |
| `{{10}}` | pH promedio | `6.2` | `Sin datos` |
| `{{11}}` | pH mínimo | `5.8` | `--` |
| `{{12}}` | pH máximo | `6.7` | `--` |
| `{{13}}` | Temp. del tanque promedio | `22.0` | `Sin datos` |
| `{{14}}` | Temp. del tanque mínima | `19.5` | `--` |
| `{{15}}` | Temp. del tanque máxima | `24.8` | `--` |
| `{{16}}` | Nivel de agua promedio (L) | `32.5` | `Sin datos` |
| `{{17}}` | Nivel de agua mínimo (L) | `28.0` | `--` |
| `{{18}}` | Nivel de agua máximo (L) | `35.2` | `--` |
| `{{19}}` | Ventiladores (minutos) | `180` | `0` |
| `{{20}}` | Termoventilador (minutos) | `45` | `0` |
| `{{21}}` | Luz amplio espectro (minutos) | `720` | `0` |
| `{{22}}` | Piedra difusora (minutos) | `60` | `0` |
| `{{23}}` | Bomba de agua (minutos) | `90` | `0` |
| `{{24}}` | Calefactor de agua (minutos) | `30` | `0` |
| `{{25}}` | Humidificador (minutos) | `120` | `0` |
| `{{26}}` | Nombre del sistema fuzzy activo | `Invernadero_Espinaca` | `Sin sistema activo` |
| `{{27}}` | Cantidad de evaluaciones fuzzy | `48` | `0` |

### Pie de página

```
HydroEspinaca 🌱 — Tu asistente de invernadero
```

### Vista previa — con datos

> **📊 Resumen diario de tu invernadero — 04/03/2026**
>
> ¡Hola! 👋 Aquí tienes el resumen de lo que pasó en tu invernadero durante las últimas 24 horas.
>
> 🌡️ **Temperatura ambiente**
> Promedio: 24.5°C - Mínima: 20.1°C - Máxima: 29.3°C
>
> 💧 **Humedad relativa**
> Promedio: 78% - Mínima: 65% - Máxima: 92%
>
> 💡 **Luminosidad**
> Promedio: 12450 lux - Mínima: 3200 lux - Máxima: 28500 lux
>
> 🧪 **pH de la solución**
> Promedio: 6.2 - Mínima: 5.8 - Máxima: 6.7
>
> 🌊 **Temperatura del tanque**
> Promedio: 22.0°C - Mínima: 19.5°C - Máxima: 24.8°C
>
> 📏 **Nivel de agua del tanque**
> Promedio: 32.5 L - Mínima: 28.0 L - Máxima: 35.2 L
>
> ⚙️ **Actuadores — tiempo activo**
> 🌀 Ventiladores: 180 min
> 🔥 Termoventilador: 45 min
> 💡 Luz amplio espectro: 720 min
> 🫧 Piedra difusora: 60 min
> 💧 Bomba de agua: 90 min
> ♨️ Calefactor de agua: 30 min
> 🌫️ Humidificador: 120 min
>
> 🧠 **Sistema Fuzzy** (Invernadero_Espinaca)
> Evaluaciones realizadas: 48
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

### Vista previa — sin datos de sensores

> **📊 Resumen diario de tu invernadero — 04/03/2026**
>
> ¡Hola! 👋 Aquí tienes el resumen de lo que pasó en tu invernadero durante las últimas 24 horas.
>
> 🌡️ **Temperatura ambiente**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> 💧 **Humedad relativa**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> 💡 **Luminosidad**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> 🧪 **pH de la solución**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> 🌊 **Temperatura del tanque**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> 📏 **Nivel de agua del tanque**
> Promedio: Sin datos - Mínima: -- - Máxima: --
>
> ⚙️ **Actuadores — tiempo activo**
> 🌀 Ventiladores: 0 min
> 🔥 Termoventilador: 0 min
> 💡 Luz amplio espectro: 0 min
> 🫧 Piedra difusora: 0 min
> 💧 Bomba de agua: 0 min
> ♨️ Calefactor de agua: 0 min
> 🌫️ Humidificador: 0 min
>
> 🧠 **Sistema Fuzzy** (Sin sistema activo)
> Evaluaciones realizadas: 0
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

### Nota sobre el pronóstico del clima

El pronóstico meteorológico **no se incluye** en la plantilla de resumen diario de WhatsApp.
Esto es intencional: la data de pronóstico ya se entrega vía alertas meteorológicas
(plantilla `alerta_meteorologica`) y los datos de sensores son el valor central del resumen.
De ser necesario en el futuro, se puede crear una variante con pronóstico.

---

## 2. `alerta_meteorologica`

Enviada cuando el sistema detecta condiciones climáticas que superan los umbrales
configurados por el usuario. **Una sola plantilla cubre todos los tipos de alerta
meteorológica** (calor, frío, lluvia, viento, etc.) gracias a variables que describen
el tipo y la condición.

### ¿Por qué una sola plantilla y no una por tipo de alerta?

- Hay **10 tipos** de alerta meteorológica en el sistema (`extreme_heat`, `extreme_cold`,
  `high_humidity`, `low_humidity`, `heavy_rain`, `thunderstorm`, `high_cloudiness`,
  `strong_wind`, `extreme_uv`, `government`). Crear 10 plantillas sería difícil de
  mantener y muy lento de aprobar en Meta.
- Una sola plantilla con variables para el tipo, valor y recomendación cubre
  **todos los escenarios** y el mensaje sigue siendo descriptivo y claro.
- Cuando se agrupan varias alertas (ej: calor + viento al mismo tiempo), se envía
  un resumen usando la misma plantilla.

### Encabezado (texto)

```
⚠️ Alerta meteorológica — {{1}}
```

| Variable | Descripción | Ejemplo (1 alerta) | Ejemplo (varias) |
|----------|-------------|---------------------|-------------------|
| `{{1}}` | Tipo de alerta o resumen | `Calor extremo` | `3 alertas detectadas` |

### Cuerpo

```
¡Atención! Se detectaron condiciones climáticas que pueden afectar tu invernadero.

🔴 *Severidad*: {{1}}

📋 *Detalle*
{{2}}

💡 *Recomendación*
{{3}}

📅 *Fecha del pronóstico*: {{4}}

Revisa las alertas activas en la app.
```

| Variable | Descripción | Ej. 1 alerta | Ej. múltiples alertas |
|----------|-------------|--------------|-----------------------|
| `{{1}}` | Severidad | `Advertencia` | `Crítica` |
| `{{2}}` | Detalle/descripción | `Se pronostica un valor de 38.2°C, mayor al umbral (35.0°C).` | `🔥 Calor extremo: 38.2°C (umbral: 35°C) · 💨 Viento fuerte: 12.5 m/s (umbral: 10 m/s)` |
| `{{3}}` | Recomendación | `Temperatura exterior alta. Considere aumentar la ventilación y revisar la rutina fuzzy.` | `Revisa cada alerta en la app para ver las recomendaciones individuales.` |
| `{{4}}` | Fecha del pronóstico | `05/03/2026` | `05/03/2026` |

### Pie de página

```
HydroEspinaca 🌱 — Tu asistente de invernadero
```

### Vista previa — alerta individual (calor extremo)

> **⚠️ Alerta meteorológica — Calor extremo**
>
> ¡Atención! Se detectaron condiciones climáticas que pueden afectar tu invernadero.
>
> 🔴 **Severidad**: Advertencia
>
> 📋 **Detalle**
> Se pronostica un valor de 38.2°C, mayor al umbral (35.0°C).
>
> 💡 **Recomendación**
> Temperatura exterior alta. Considere aumentar la ventilación y revisar la rutina fuzzy.
>
> 📅 **Fecha del pronóstico**: 05/03/2026
>
> Revisa las alertas activas en la app.
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

### Vista previa — alertas agrupadas (3 alertas simultáneas)

> **⚠️ Alerta meteorológica — 3 alertas detectadas**
>
> ¡Atención! Se detectaron condiciones climáticas que pueden afectar tu invernadero.
>
> 🔴 **Severidad**: Crítica
>
> 📋 **Detalle**
> 🔥 Calor extremo: 38.2°C (umbral: 35°C) · 💨 Viento fuerte: 12.5 m/s (umbral: 10 m/s) · ☁️ Nubosidad alta: 92% (umbral: 80%)
>
> 💡 **Recomendación**
> Revisa cada alerta en la app para ver las recomendaciones individuales.
>
> 📅 **Fecha del pronóstico**: 05/03/2026
>
> Revisa las alertas activas en la app.
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

### Vista previa — tormenta eléctrica

> **⚠️ Alerta meteorológica — Tormenta eléctrica**
>
> ¡Atención! Se detectaron condiciones climáticas que pueden afectar tu invernadero.
>
> 🔴 **Severidad**: Crítica
>
> 📋 **Detalle**
> Se ha detectado riesgo por tormenta eléctrica.
>
> 💡 **Recomendación**
> Revise conexiones eléctricas y considere modos de operación conservadores.
>
> 📅 **Fecha del pronóstico**: 06/03/2026
>
> Revisa las alertas activas en la app.
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

### Vista previa — alerta gubernamental

> **⚠️ Alerta meteorológica — Alerta oficial del gobierno**
>
> ¡Atención! Se detectaron condiciones climáticas que pueden afectar tu invernadero.
>
> 🔴 **Severidad**: Advertencia
>
> 📋 **Detalle**
> Alerta gubernamental activa.
>
> 💡 **Recomendación**
> Tome las precauciones necesarias para proteger sus cultivos.
>
> 📅 **Fecha del pronóstico**: 07/03/2026
>
> Revisa las alertas activas en la app.
>
> _HydroEspinaca 🌱 — Tu asistente de invernadero_

---

## Mapeo en el código

Referencia rápida de cómo `MetaWhatsAppTemplateMapper` conecta cada `templateKey`
del sistema con la plantilla de Meta y de dónde extrae cada variable.

### `daily_summary` → `resumen_diario_invernadero`

| Variable Meta | Origen en el código |
|---------------|---------------------|
| Header `{{1}}` | `data["date"]` (fecha formateada `dd/MM/yyyy`) |
| Body `{{1}}`–`{{3}}` | Temperatura ambiente: `data["temp_avg"]`, `data["temp_min"]`, `data["temp_max"]` |
| Body `{{4}}`–`{{6}}` | Humedad relativa: `data["hum_avg"]`, `data["hum_min"]`, `data["hum_max"]` |
| Body `{{7}}`–`{{9}}` | Luminosidad: `data["lux_avg"]`, `data["lux_min"]`, `data["lux_max"]` |
| Body `{{10}}`–`{{12}}` | pH: `data["ph_avg"]`, `data["ph_min"]`, `data["ph_max"]` |
| Body `{{13}}`–`{{15}}` | Temp. tanque: `data["tank_temp_avg"]`, `data["tank_temp_min"]`, `data["tank_temp_max"]` |
| Body `{{16}}`–`{{18}}` | Nivel de agua: `data["water_level_avg"]`, `data["water_level_min"]`, `data["water_level_max"]` |
| Body `{{19}}`–`{{25}}` | Actuadores (min activo): `data["act_ventiladores"]` … `data["act_humidificador"]` |
| Body `{{26}}` | `data["fuzzy_system"]` — Nombre del sistema fuzzy activo |
| Body `{{27}}` | `data["fuzzy_evaluations"]` — Cantidad de evaluaciones fuzzy |

### `weather_alert` → `alerta_meteorologica`

| Variable Meta | Origen en el código |
|---------------|---------------------|
| Header `{{1}}` | 1 alerta: tipo humanizado del `AlertType` · Múltiples: `"{count} alertas detectadas"` |
| Body `{{1}}` | Severidad humanizada: `"Advertencia"` / `"Crítica"` |
| Body `{{2}}` | 1 alerta: `message.Body` truncado a 400 chars · Múltiples: resumen concatenado con emojis |
| Body `{{3}}` | 1 alerta: `data["recommendation"]` truncado a 250 chars · Múltiples: texto genérico |
| Body `{{4}}` | `data["forecastDate"]` o fecha actual |

---

## Creación en Facebook Business Manager

### Pasos para crear cada plantilla

1. Ir a **Administrador de WhatsApp** → **Plantillas de mensajes** → **Crear plantilla**
2. Seleccionar categoría: **Utilidad**
3. Nombre: copiar exactamente el nombre de la plantilla (ej: `resumen_diario_invernadero`)
4. Idioma: **Spanish (COL)**
5. Completar **Encabezado**, **Cuerpo** y **Pie de página** copiando el texto de arriba
6. En cada variable, seleccionar tipo **Texto** y poner el valor de ejemplo de la tabla
7. Hacer clic en **Enviar para revisión**

### Valores de ejemplo requeridos por Meta

Meta exige un ejemplo por cada variable al crear la plantilla. Usar estos:

#### `resumen_diario_invernadero`

| Sección | Variable | Valor de ejemplo |
|---------|----------|------------------|
| Header | `{{1}}` | `04/03/2026` |
| Body | `{{1}}` | `24.5` |
| Body | `{{2}}` | `20.1` |
| Body | `{{3}}` | `29.3` |
| Body | `{{4}}` | `78` |
| Body | `{{5}}` | `65` |
| Body | `{{6}}` | `92` |
| Body | `{{7}}` | `12450` |
| Body | `{{8}}` | `3200` |
| Body | `{{9}}` | `28500` |
| Body | `{{10}}` | `6.2` |
| Body | `{{11}}` | `5.8` |
| Body | `{{12}}` | `6.7` |
| Body | `{{13}}` | `22.0` |
| Body | `{{14}}` | `19.5` |
| Body | `{{15}}` | `24.8` |
| Body | `{{16}}` | `32.5` |
| Body | `{{17}}` | `28.0` |
| Body | `{{18}}` | `35.2` |
| Body | `{{19}}` | `180` |
| Body | `{{20}}` | `45` |
| Body | `{{21}}` | `720` |
| Body | `{{22}}` | `60` |
| Body | `{{23}}` | `90` |
| Body | `{{24}}` | `30` |
| Body | `{{25}}` | `120` |
| Body | `{{26}}` | `Invernadero_Espinaca` |
| Body | `{{27}}` | `48` |

#### `alerta_meteorologica`

| Sección | Variable | Valor de ejemplo |
|---------|----------|------------------|
| Header | `{{1}}` | `Calor extremo` |
| Body | `{{1}}` | `Advertencia` |
| Body | `{{2}}` | `Se pronostica un valor de 38.2°C, mayor al umbral (35.0°C).` |
| Body | `{{3}}` | `Temperatura exterior alta. Considere aumentar la ventilación y revisar la rutina fuzzy.` |
| Body | `{{4}}` | `05/03/2026` |

---

## Consideración de límites de Meta

| Límite | Valor | ¿Nos afecta? |
|--------|-------|--------------|
| Caracteres en encabezado | 60 | No. El más largo es ~52 chars |
| Caracteres en cuerpo | 1024 | ⚠️ El resumen diario llega a ~850 chars. Holgura suficiente |
| Caracteres en pie de página | 60 | No. Son ~43 chars |
| Variables en cuerpo | 99 | No. Máximo 27 variables |
| Variables en encabezado | 1 | Cumplimos exacto |
