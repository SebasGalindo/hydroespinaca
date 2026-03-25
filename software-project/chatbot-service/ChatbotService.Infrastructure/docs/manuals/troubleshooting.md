# Guía de Troubleshooting — HydroEspinaca

## Problemas de conectividad

### El ESP32 no se conecta al Wi-Fi
**Síntomas**: LED de estado parpadea rápido, no aparece "online" en el dashboard.

**Causas posibles y soluciones**:
1. **SSID/contraseña incorrectos**: verificar en `secrets.h` que las credenciales son correctas.
2. **Router en 5 GHz**: el ESP32 solo soporta 2.4 GHz. Asegurar que la red 2.4 GHz está activa.
3. **Distancia excesiva**: mover el ESP32 más cerca del router o agregar un repetidor.
4. **Demasiados dispositivos conectados**: algunos routers tienen límite de dispositivos simultáneos.
5. **Canal Wi-Fi congestionado**: cambiar el canal del router a uno menos congestionado (1, 6 u 11).

### El ESP32 se conecta a Wi-Fi pero no al broker MQTT
**Síntomas**: Wi-Fi conectado pero no se publican datos de sensores.

**Soluciones**:
1. Verificar que el servicio Mosquitto está corriendo: `docker logs mosquitto`.
2. Verificar la configuración TLS: certificados válidos y no expirados.
3. Confirmar host y puerto del broker en `secrets.h` (por defecto puerto 8883 TLS).
4. Revisar reglas de firewall del servidor (puerto 8883 debe estar abierto).
5. Verificar credenciales MQTT (usuario/contraseña).

### Datos de sensores se publican pero no aparecen en la web
**Soluciones**:
1. Verificar que el sensor-service está corriendo: `docker logs sensor-service`.
2. Revisar los logs del sensor-service buscando errores de parsing MQTT.
3. Confirmar que el topic MQTT coincide con el esperado por el sensor-service.
4. Verificar conexión del sensor-service a MongoDB.

## Problemas de sensores

### Lecturas de temperatura/humedad en 0 o NaN
**Causas**:
1. **Cable suelto**: verificar conexiones del DHT22 al GPIO 4.
2. **Sensor dañado**: probar con otro DHT22.
3. **Pull-up resistor faltante**: debe haber resistencia de 4.7kΩ entre VCC y DATA.
4. **Frecuencia de lectura alta**: el DHT22 necesita mínimo 2 segundos entre lecturas.

### Lecturas de EC inestables o incorrectas
**Causas y soluciones**:
1. **Sonda sucia**: limpiar con agua destilada y cepillo suave.
2. **Burbujas de aire**: asegurar que la sonda está completamente sumergida.
3. **Necesita calibración**: seguir SOP-003 con solución estándar conocida.
4. **Interferencia eléctrica**: alejar cables de la sonda de motores o relays.
5. **Temperatura**: la EC varía con la temperatura; el firmware compensa, pero verificar.

### Lecturas de pH inexactas
**Soluciones**:
1. **Calibración vencida**: recalibrar cada 30 días (SOP-003).
2. **Sonda seca**: la sonda de pH debe mantenerse húmeda cuando no está en uso.
3. **Sonda envejecida**: vida útil típica de 12 – 18 meses. Reemplazar si la calibración no converge.
4. **Solución buffer contaminada**: usar buffers frescos para calibración.

## Problemas de actuadores

### La bomba de agua no enciende
**Verificaciones**:
1. Confirmar que el relay en GPIO 25 emite un "click" al activarse.
2. Medir voltaje en el relay con multímetro.
3. Verificar la alimentación externa de la bomba (12V o 220V según modelo).
4. Revisar en el dashboard si el sistema difuso está generando señal de riego.
5. Verificar el nivel del tanque — el sistema puede bloquear la bomba si detecta nivel bajo.

### El ventilador no responde al PWM
**Verificaciones**:
1. Confirmar que la alimentación externa de 12V está conectada.
2. Medir señal PWM en GPIO 27 con osciloscopio o LED de prueba.
3. Verificar que el duty cycle no es 0 % — revisar evaluaciones difusas.
4. Probar con valor fijo de 50 % desde comando MQTT para aislar si es hardware o software.
5. Verificar el driver/transistor si se usa uno entre el ESP32 y el ventilador.

### LED Grow Lights no cambian de intensidad
**Verificaciones**:
1. Confirmar señal PWM en GPIO 12.
2. Verificar fuente de alimentación de la tira LED.
3. Algunos drivers LED necesitan señal invertida — revisar configuración del firmware.

## Problemas del sistema difuso

### Las evaluaciones difusas no se ejecutan
**Verificaciones**:
1. Confirmar que al menos un sistema difuso está en estado "activo".
2. Verificar que tiene reglas definidas (un sistema sin reglas no produce salida).
3. Revisar logs del fuzzy-service: `docker logs fuzzy-service`.
4. Confirmar que el scheduler interno no está en pausa.

### Los actuadores no se ajustan según las evaluaciones
**Causas**:
1. **Evaluación correcta pero comando no enviado**: verificar logs del actuator-service.
2. **Comando enviado pero no recibido por ESP32**: verificar conexión MQTT del dispositivo.
3. **Reglas mal configuradas**: los consecuentes de las reglas pueden no cubrir el rango necesario. Revisar las membership functions de las variables de salida.
4. **Conflicto entre reglas**: reglas contradictorias pueden generar valores neutros (centroide en el medio).

### Output de evaluación siempre en 50 % (valor medio)
**Causa probable**: las reglas definidas se están cancelando mutuamente. Esto ocurre cuando hay igual cantidad de reglas activas con consecuentes "alto" y "bajo".

**Solución**: revisar las reglas y sus pesos, asegurar que cubren adecuadamente el espacio de entrada.

## Problemas de la plataforma web

### No puedo iniciar sesión
1. Verificar que el auth-service está corriendo.
2. Confirmar usuario y contraseña.
3. Limpiar cookies del navegador.
4. Verificar que el BFF (backend-for-frontend) está respondiendo en su puerto.

### El dashboard no muestra datos en tiempo real
1. Verificar WebSocket connection en la consola del navegador (F12).
2. Confirmar que el servicio de notificaciones está corriendo.
3. Verificar que no hay bloqueadores de contenido interfiriendo con WebSockets.

### Error 502 Bad Gateway
1. Verificar que todos los servicios Docker están corriendo: `docker ps`.
2. Revisar logs de Nginx: `docker logs nginx`.
3. Confirmar que los puertos internos coinciden con la configuración de Nginx.
4. Reiniciar servicios: `docker-compose down && docker-compose up -d`.

## Problemas del chatbot

### El chatbot no responde o responde con error
1. Verificar que la API key de Gemini está configurada y es válida.
2. Revisar logs del chatbot-service: `docker logs chatbot-service`.
3. Confirmar que hay chunks indexados en la colección `knowledge_chunks`.
4. Verificar conectividad a Internet del servidor (Gemini requiere acceso a `generativelanguage.googleapis.com`).

### Respuestas irrelevantes o genéricas
1. **Pocos chunks indexados**: ejecutar reindexación desde el endpoint `/api/rag/reindex`.
2. **Vector search index no creado**: verificar que el índice `knowledge_vector_index` existe en MongoDB Atlas.
3. **Embeddings desactualizados**: si se modificaron sistemas difusos, sincronizar con `/api/rag/sync`.
4. **Prompt demasiado corto**: verificar que el contexto vectorial se está incluyendo en la generación.
