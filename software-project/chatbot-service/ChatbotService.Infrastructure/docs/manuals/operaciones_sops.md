# Procedimientos Operativos Estándar (SOPs)

## SOP-001: Inicio diario del sistema

### Objetivo
Verificar que todos los componentes del invernadero están operativos al inicio del día.

### Procedimiento
1. Revisar el dashboard web y confirmar que el ESP32 reporta estado "online".
2. Verificar que las lecturas de sensores son razonables:
   - Temperatura: entre 10 °C y 40 °C.
   - Humedad: entre 20 % y 100 %.
   - EC: entre 0.5 y 5.0 mS/cm.
   - pH: entre 4.0 y 8.0.
3. Revisar el nivel del tanque de solución nutritiva (indicador en dashboard o sensor de nivel).
4. Confirmar que los sistemas difusos están en estado "activo" en la sección de configuración.
5. Revisar notificaciones pendientes del día anterior.

## SOP-002: Preparación de solución nutritiva

### Objetivo
Preparar la solución nutritiva para espinaca hidropónica NFT.

### Materiales
- Agua potable o filtrada (preferible RO).
- Solución concentrada A (macronutrientes).
- Solución concentrada B (micronutrientes).
- pH Down (ácido fosfórico) y pH Up (hidróxido de potasio).
- Medidor de EC y pH (o usar sensores del sistema).

### Procedimiento
1. Llenar el tanque con agua base hasta el nivel indicado (mínimo 50 L).
2. Agregar solución A: 2 mL por litro de agua. Mezclar bien.
3. Agregar solución B: 2 mL por litro de agua. Mezclar bien.
4. Medir EC → objetivo: 1.8 – 2.3 mS/cm. Ajustar añadiendo agua (diluir) o más concentrado.
5. Medir pH → objetivo: 5.8 – 6.5. Ajustar con pH Down o pH Up.
6. Registrar los valores en el sistema si se realizó manualmente.

### Frecuencia
- **Cambio completo**: cada 7 – 14 días.
- **Revisión de EC/pH**: diaria (automatizada por sensores).
- **Relleno de nivel**: según consumo (2 – 5 L/día en promedio).

## SOP-003: Calibración de sensores

### Objetivo
Mantener la precisión de los sensores de EC y pH.

### Calibración del sensor de pH
1. Preparar soluciones buffer pH 4.0 y pH 7.0 (temperatura ambiente).
2. Enjuagar la sonda con agua destilada.
3. Sumergir en buffer pH 7.0, esperar estabilización (30 seg), registrar valor raw.
4. Sumergir en buffer pH 4.0, esperar estabilización, registrar valor raw.
5. En el firmware, actualizar los offsets de calibración y reiniciar el ESP32.

### Calibración del sensor de EC
1. Preparar solución estándar de 1413 μS/cm.
2. Enjuagar la sonda con agua destilada.
3. Sumergir en solución estándar, registrar valor raw.
4. Aplicar factor de corrección en el firmware.

### Frecuencia
- pH: cada 30 días o si las lecturas se desvían >0.3 del valor esperado.
- EC: cada 60 días o tras cambio de sonda.

## SOP-004: Trasplante de plántulas al canal NFT

### Procedimiento
1. Las plántulas deben tener mínimo 2 hojas verdaderas (~14 días desde siembra).
2. Verificar que las raíces asoman por debajo del sustrato (espuma fenólica o cubo de lana de roca).
3. Colocar el cubo con la plántula en el agujero del canal NFT.
4. Asegurar que las raíces tocan el film de solución nutritiva.
5. No exponer las plántulas a luz directa intensa las primeras 24 horas.
6. Monitorear marchitamiento las primeras 48 horas.

## SOP-005: Revisión semanal del sistema

### Lista de verificación
- [ ] Limpiar filtros de la bomba de agua.
- [ ] Inspeccionar raíces por coloración (blancas = sanas, marrones = posible Pythium).
- [ ] Verificar flujo uniforme en todos los canales NFT.
- [ ] Revisar conexiones eléctricas del ESP32 y actuadores.
- [ ] Descargar y revisar logs de evaluaciones difusas de la semana.
- [ ] Limpiar sondas de EC y pH con agua destilada.
- [ ] Verificar que el ventilador y extractor giran libremente.
- [ ] Revisar el historial de notificaciones por anomalías.

## SOP-006: Cosecha de espinaca

### Momento de cosecha
- Hojas con tamaño de 10 – 15 cm (baby spinach: 5 – 8 cm).
- Generalmente 30 – 45 días post-siembra.
- Cosechar preferiblemente al inicio del día (mayor turgencia).

### Procedimiento
1. Cortar hojas exteriores con tijera limpia, dejando el punto de crecimiento central.
2. No arrancar la planta completa si se desea seguir cosechando (método "cut and come again").
3. Después de 3 – 4 cosechas sucesivas, retirar la planta agotada y replantar.
4. Lavar las hojas con agua fría inmediatamente después del corte.
5. Almacenar a 4 °C en bolsa perforada (vida útil: 5 – 7 días).

## SOP-007: Respuesta a alertas del sistema

### Alerta: Temperatura alta (>28 °C)
1. Verificar que ventilador y extractor están funcionando a máxima capacidad.
2. Si es posible, abrir ventilación manual adicional.
3. Considerar sombreado temporal si la causa es radiación solar directa.
4. Revisar evaluaciones difusas recientes para confirmar que el sistema respondió.

### Alerta: pH fuera de rango
1. Medir manualmente con kit de gotas para confirmar lectura del sensor.
2. Si el sensor es correcto, ajustar con pH Down/Up según corresponda.
3. Si el sensor difiere significativamente, proceder a calibración (SOP-003).

### Alerta: EC baja (<1.5 mS/cm)
1. Indica dilución por consumo o adición excesiva de agua.
2. Agregar concentrado A y B en proporción hasta alcanzar rango objetivo.
3. Si persiste, puede indicar que la planta está en fase de alta absorción — normal.

### Alerta: ESP32 offline
1. Verificar alimentación eléctrica del dispositivo.
2. Verificar conexión Wi-Fi (distancia al router, interferencias).
3. Reiniciar el ESP32 desconectando y reconectando alimentación.
4. Si persiste, revisar logs en el monitor serial (USB) para diagnóstico.
