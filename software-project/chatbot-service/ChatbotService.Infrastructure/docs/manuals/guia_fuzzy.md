# Guía del Sistema de Lógica Difusa (Fuzzy Logic)

## Introducción

El sistema HydroEspinaca utiliza lógica difusa para controlar los actuadores del invernadero de forma inteligente. En lugar de umbrales binarios (on/off), el sistema evalúa reglas difusas que producen salidas graduales, logrando un control más suave y eficiente. esto dependiendo del tipo de actuador, cuando es analogo
el sistema fuzzy permite no solo controlar la duración de activación sino también su potencia, por otro lado
cuando es un actuador conectado por PIN digital al firmawe se controla su activación ON/OFF y su duración por terminos linguisticos. 

## Conceptos clave

### Variables lingüísticas
Cada sensor o actuador se modela como una **variable lingüística** con múltiples **conjuntos difusos** (membership functions). Por ejemplo:

- **Temperatura** → conjuntos: "baja", "media", "alta"
- **Humedad** → conjuntos: "seca", "normal", "húmeda"
- **Intensidad de ventilador** → conjuntos: "apagado", "bajo", "medio", "alto"

### Funciones de membresía
Cada conjunto difuso define una forma (triangular, trapezoidal, gaussiana) que mapea un valor numérico a un grado de pertenencia entre 0 y 1.

Ejemplo: si la temperatura es 26 °C, su grado de pertenencia a "alta" podría ser 0.7 y a "media" 0.3.

### Reglas difusas
Son sentencias tipo IF-THEN que combinan antecedentes con operadores AND/OR:

```
IF temperatura IS alta AND humedad IS baja
THEN ventilador IS alto AND riego IS medio
```

## Sistemas difusos en HydroEspinaca

### Sistema de climatización
Controla ventiladores y extractores basándose en temperatura y humedad.

**Variables de entrada:**
- `temperatura_ambiente`: rango 10 – 45 °C
- `humedad_relativa`: rango 20 – 100 %

**Variables de salida:**
- `intensidad_ventilador`: rango 0 – 100 %
- `intensidad_extractor`: rango 0 – 100 %

### Sistema de riego
Controla la bomba de solución nutritiva y ajustes de pH.

**Variables de entrada:**
- `humedad_sustrato`: rango 0 – 100 %
- `ec_solucion`: rango 0 – 5 mS/cm
- `ph_solucion`: rango 3 – 9

**Variables de salida:**
- `tiempo_riego`: rango 0 – 60 minutos
- `ajuste_ph`: rango -2 – +2 unidades

### Sistema de iluminación
Controla luces suplementarias basándose en luz natural y fotoperiodo.

**Variables de entrada:**
- `luz_natural`: rango 0 – 100000 lux
- `horas_luz_acumuladas`: rango 0 – 24 h

**Variables de salida:**
- `intensidad_luz_artificial`: rango 0 – 100 %

## Proceso de evaluación

1. **Fuzzificación**: los valores numéricos de los sensores se convierten en grados de pertenencia.
2. **Inferencia**: se evalúan todas las reglas aplicables, combinando antecedentes con AND (mínimo) u OR (máximo).
3. **Defuzzificación**: se usa el método del centroide (center of gravity) para obtener un valor numérico de salida.

## Creación de reglas en la plataforma

Para crear un sistema difuso desde la interfaz web:

1. Ir a **Configuración > Sistemas Difusos**.
2. Crear un nuevo sistema: nombre, descripción y lista de variables (entrada/salida).
3. Para cada variable, definir sus conjuntos difusos con la forma y los parámetros correspondientes.
4. Crear reglas asociando antecedentes (variables de entrada con sus conjuntos) a consecuentes (variables de salida con sus conjuntos).
5. Activar el sistema para que el evaluador lo utilice periódicamente.

## Frecuencia de evaluación

El motor difuso evalúa los sistemas activos cada 30 segundos por defecto. Este intervalo es configurable por sistema. Los resultados de cada evaluación se persisten en la colección `fuzzy_evaluations` para análisis posterior.
