# Plan de Artículo IEEE — HydroEspinaca v2
**Asesor de investigación:** Claude Sonnet 4.6 · **Fecha:** 23 de abril de 2026  
**Autor del proyecto:** Sebastián Galindo Hernández

---

## Opciones Consideradas

### Opción A — DSS para Análisis de Rentabilidad Económica en Sistemas Hidropónicos IoT
Enfoque: el módulo BI con indicadores ROI, huella hídrica y costo/kg.  
Fortaleza: evidencia empírica ya lista (19 tests, formulas explícitas).  
Debilidad: percibido como extensión de ingeniería; menos profundidad teórica.

### Opción B — RAG Asistido por LLM con Control Difuso Configurable por Usuario en Sistemas Agro-IoT *(ELEGIDA)*
Enfoque: la combinación de recuperación aumentada por generación (RAG) sobre documentos técnicos agrícolas con un motor de control difuso parametrizable por el operador.  
Fortaleza: dos temas de alta relevancia en la literatura actual (LLMs aplicados + fuzzy adaptativo); mayor profundidad teórica; más alineado con Ingeniería de Sistemas / Ciencias de la Computación.

---

## Elección: **OPCIÓN B**

**Justificación:**

1. **Alineación con la carrera.** La Opción B combina dos pilares de Ingeniería de Sistemas: inteligencia artificial aplicada (LLMs + búsqueda vectorial) y sistemas de control inteligente (lógica difusa adaptativa). La Opción A es más de Ingeniería Agrícola/Industrial.
2. **Novedad teórica mayor.** La aplicación de RAG sobre documentación técnica de un sistema de automatización agrícola específico, con recuperación HNSW sobre embeddings de 768 dimensiones, no está extensamente cubierta en literatura de AgriTech.
3. **Doble contribución justificada.** RAG + fuzzy configurable no son temas arbitrariamente combinados: el RAG sirve al operador para entender y configurar el sistema difuso. La sinergia es el argumento narrativo del artículo.
4. **Referente estándar citable.** IEEE Std 1855-2016 (Fuzzy Markup Language) le da base formal a la contribución de fuzzy configurable.
5. **Benchmarks de Gemini Flash documentados.** MMLU-Pro 60%+, GPQA Diamond 90%, AIME 2025 97% — estos datos sirven para justificar la elección del modelo en la sección de Metodología.

La Opción A se menciona en la Introducción como motivación del sistema completo y en Conclusiones como trabajo futuro.

---

## Título Propuesto

> **RAG-Enhanced Knowledge Retrieval and User-Configurable Fuzzy Control for Precision Agriculture in IoT-Based Hydroponic Systems**

Alternativas más específicas:
- *Integrating Retrieval-Augmented Generation and Adaptive Fuzzy Logic for Operator-Driven Control in Hydroponic IoT Systems*
- *A Dual-Layer Intelligence Architecture for Hydroponic Automation: Configurable Fuzzy Control with LLM-Based Knowledge Retrieval*

---

## Estructura IEEE Sección por Sección

---

### Abstract *(150–250 palabras)*

**Qué narrar:**

El problema: los sistemas de automatización hidropónica IoT implementan lógica de control fija definida por expertos en el diseño del sistema. Dos brechas emergen: (1) el operador no puede ajustar el comportamiento del sistema sin modificar código; (2) no existe interfaz para consultar el razonamiento del sistema ni acceder al conocimiento técnico agrícola asociado al cultivo.

La solución propuesta: una arquitectura de doble capa de inteligencia que combina (a) un motor de control difuso Mamdani MIMO con funciones de membresía paramétricas configurables por el usuario (triangular, trapezoidal, gaussiana, sigmoidal) y (b) un módulo de Retrieval-Augmented Generation (RAG) basado en Gemini Flash con búsqueda vectorial HNSW sobre embeddings de 768 dimensiones para recuperación de conocimiento técnico agrícola.

Los métodos: implementación sobre microservicios con Clean Architecture + CQRS; sistema de control validado sobre datos de temperatura, humedad y luminosidad en sistema DFT de espinaca; RAG evaluado con métricas de faithfulness y relevancia contextual.

Los resultados clave: el motor difuso configurable soporta N familias de funciones de membresía con persistencia en MongoDB sin recompilación; el chatbot RAG responde consultas en lenguaje natural sobre el cultivo y el sistema con precisión contextual superior al baseline de búsqueda por palabras clave.

La conclusión: la arquitectura propuesta democratiza la configuración experta de sistemas hidropónicos IoT y provee asistencia inteligente al operador, reduciendo la dependencia de intervención técnica especializada.

> **Evitar:** abrir con "In recent years..." o "Novel approach..." — ambos son banderas rojas para revisores IEEE.

---

### I. Introduction *(~600 palabras, aprox. 1 columna IEEE)*

**Qué narrar:**

**Párrafo 1 — Contexto y motivación:**  
La agricultura hidropónica controlada por IoT ha demostrado eficiencias superiores en uso de agua y productividad por m² frente a la agricultura convencional (citar FAO/Barbosa). Los sistemas de control automático para variables ambientales (temperatura, humedad, pH, conductividad eléctrica, luminosidad) están documentados en literatura. Sin embargo, la adopción masiva en pequeños y medianos productores enfrenta una barrera de conocimiento: el operador interactúa con un sistema cuya lógica le es opaca y que no puede ajustar sin asistencia técnica especializada.

**Párrafo 2 — El doble gap específico:**  
Gap 1 (control): los sistemas existentes implementan reglas difusas fijas en tiempo de diseño. Si las condiciones del cultivo cambian (variedad diferente, estación climática, objetivos de producción distintos), el sistema no puede adaptarse sin recompilación o redeploy. Los estándares como IEEE Std 1855-2016 (FML) proponen una representación parametrizable de sistemas difusos, pero su adopción en sistemas IoT productivos es escasa.  
Gap 2 (conocimiento): el operador no tiene interfaz para consultar el razonamiento del sistema ni para acceder a documentación técnica especializada sobre su cultivo en lenguaje natural. Los avances recientes en Large Language Models y técnicas RAG habilitan este caso de uso, pero su aplicación en automatización agrícola específica es un área emergente.

**Párrafo 3 — Solución propuesta:**  
HydroEspinaca v2 extiende el sistema base v1 (control difuso Mamdani MIMO sobre espinaca hidropónica DFT, validado con 791 tests unitarios [citar v1]) con dos módulos complementarios: (a) un motor de control difuso con funciones de membresía paramétricas configurables en tiempo de ejecución mediante una interfaz de usuario, y (b) un chatbot RAG que indexa documentación técnica agrícola con embeddings de 768 dimensiones y recuperación aproximada HNSW, respondiendo preguntas en lenguaje natural sobre el cultivo y el sistema.

**Párrafo 4 — Contribuciones del artículo (lista numerada):**
1. Diseño e implementación de un motor de control difuso Mamdani MIMO con funciones de membresía paramétricas configurables en tiempo de ejecución, sin recompilación, sobre arquitectura de microservicios
2. Integración de un módulo RAG con Gemini Flash 2.0 sobre MongoDB Atlas Vector Search (HNSW, 768 dimensiones, similitud coseno) para recuperación de conocimiento técnico agrícola en lenguaje natural
3. Demostración de la sinergia entre ambos módulos: el RAG asiste al operador en la comprensión y configuración del sistema difuso
4. Validación sobre un sistema hidropónico DFT real con espinaca (*Spinacia oleracea*) en condiciones controladas

**Párrafo 5 — Organización del artículo:**  
Sección II → Related Work; Sección III → Arquitectura del Sistema; Sección IV → Motor Difuso Configurable; Sección V → Módulo RAG; Sección VI → Resultados y Evaluación; Sección VII → Discusión; Sección VIII → Conclusiones.

---

### II. Related Work *(~700 palabras, aprox. 1.2 columnas)*

**Qué narrar:** Cuatro bloques temáticos. Cada bloque cierra con una frase que posiciona explícitamente la diferencia de este trabajo.

---

**Bloque A — Control difuso en sistemas hidropónicos e invernaderos IoT:**

Describir el estado del arte: sistemas con control Mamdani, Sugeno, PID difuso para variables como temperatura, pH, CE, luminosidad. Mencionar sistemas NFT, DFT, aeroponía. La gran mayoría define funciones de membresía triangulares o trapezoidales fijas en tiempo de diseño. Algunos trabajos recientes usan reglas difusas generadas por algoritmos evolutivos (GA) o PSO, pero siguen siendo fijas post-optimización.

*Posicionamiento:* ninguno de estos trabajos expone las funciones de membresía como parámetros configurables por el operador en tiempo de ejecución mediante una interfaz de usuario; la configuración experta requiere modificación directa del código o reentrenamiento del modelo.

---

**Bloque B — Sistemas difusos adaptativos y estándares de representación:**

Mencionar Adaptive Neuro-Fuzzy Inference Systems (ANFIS), Fuzzy Cognitive Maps, Type-2 Fuzzy Logic como líneas de investigación de sistemas difusos no estáticos. Citar IEEE Std 1855-2016 (Fuzzy Markup Language) como el estándar que define una representación XML portable y parametrizable de sistemas difusos. Mencionar trabajos que lo aplican en contextos de automatización.

*Posicionamiento:* la adopción del paradigma de parametrización en sistemas IoT agrícolas en producción (no solo prototipo) con persistencia de configuración en base de datos NoSQL y recarga en caliente es escasa o inexistente en literatura revisada.

---

**Bloque C — LLMs y RAG en agricultura de precisión:**

Describir el trabajo seminal de Lewis et al. (2020) que define RAG como la combinación de un retriever denso con un generador LLM. Mencionar aplicaciones agrícolas de LLMs: chatbots de asesoría de cultivos, diagnóstico de enfermedades por imagen + texto, recomendaciones de fertilización. Destacar limitaciones: los LLMs genéricos alucinan sobre especificidades técnicas de sistemas IoT propietarios; el RAG sobre documentación del propio sistema reduce este problema.

*Posicionamiento:* la aplicación de RAG específicamente sobre documentación técnica de un sistema hidropónico IoT propio (no knowledge bases genéricas de agronomía) para asistir en la configuración y comprensión del sistema de control no ha sido reportada en literatura.

---

**Bloque D — Búsqueda vectorial ANN y embeddings en sistemas de recuperación:**

Mencionar HNSW (Malkov & Yashunin, 2020) como el algoritmo de búsqueda aproximada de vecinos más cercanos de referencia: O(log n) con >95% de precisión recall. Comparar con alternativas (FAISS, ScaNN, IVF). Justificar MongoDB Atlas Vector Search como solución que unifica almacenamiento documental + vectorial en un mismo motor, reduciendo la complejidad operacional.

*Posicionamiento:* la mayoría de los trabajos RAG en aplicaciones IoT usan bases de datos vectoriales separadas (Pinecone, Weaviate, Chroma). La integración nativa en MongoDB permite que los documentos técnicos, los metadatos del sistema y los vectores de embedding coexistan en el mismo cluster, simplificando la arquitectura y la consistencia de datos.

---

### III. System Architecture *(~700 palabras + figura obligatoria)*

**Qué narrar:**

**A. Visión general de HydroEspinaca v2:**  
Diagrama de arquitectura de 9 microservicios (figura obligatoria). Describir brevemente cada servicio:
- **IoT Service:** recibe telemetría de sensores (temperatura, humedad, luminosidad) vía MQTT/HTTP
- **Control Service:** ejecuta el motor de inferencia difusa, activa actuadores (bomba, difusor, calefactor, LED, ventilador)
- **Auth Service:** autenticación JWT para web y mobile
- **Notification Service:** push notifications (Firebase FCM) + WhatsApp (Meta API) + scheduler Quartz con cron de 7 campos
- **BI Service:** cálculo de rentabilidad, gestión de consumos y costos (mencionado brevemente, no es el foco)
- **BFF Service:** Backend For Frontend, único punto de entrada, agrega y adapta respuestas
- **Chatbot Service:** RAG con Gemini Flash + Vector Search (foco del artículo)
- **Weather Service:** integración OpenWeatherMap One Call API 3.0 para alertas proactivas
- **Shared / Common:** tipos compartidos, DTOs, constantes

Patrón transversal: Clean Architecture (Domain → Application → Infrastructure → API) + CQRS mediado por MediatR en todos los servicios. Justificar brevemente: separación de responsabilidades, testabilidad, evolución independiente de microservicios.

**B. Tecnologías clave (tabla):**

| Capa | Tecnología | Justificación técnica |
|------|------------|-----------------------|
| Runtime backend | .NET 9 / ASP.NET Core | Alto rendimiento, soporte LTS, ecosistema CQRS maduro |
| Base de datos | MongoDB Atlas | Flexibilidad NoSQL + Vector Search nativo |
| Mensajería interna | MediatR (in-process) | CQRS sin broker externo, reducción de complejidad |
| Validación | FluentValidation | Separación de reglas de negocio del handler |
| Tests | xUnit + Moq + FluentAssertions | Suite estándar .NET para CQRS |
| LLM | Gemini Flash 2.0 (Google AI) | Velocidad + coherencia en contexto largo |
| Embeddings | gemini-embedding-001 | 768 dimensiones, optimizado para búsqueda semántica |
| Vector Search | MongoDB Atlas HNSW | ANN O(log n), similitud coseno |
| Frontend Web | Next.js 15 + TypeScript | SSR + App Router |
| Mobile | Expo React Native | Cross-platform iOS/Android |
| Infraestructura | Docker + Oracle Cloud + Cloudflare + Nginx + Certbot | Despliegue productivo |

**C. Diagrama de flujo del chatbot RAG (figura 2 — secuencia):**
1. Usuario escribe pregunta en lenguaje natural (web o mobile)
2. BFF → Chatbot Service: `POST /chat`
3. Chatbot Service genera embedding de la consulta con `gemini-embedding-001` (768-dim)
4. MongoDB Atlas Vector Search (HNSW) recupera top-K documentos por similitud coseno
5. Contexto recuperado + pregunta → prompt estructurado → Gemini Flash 2.0
6. Gemini Flash genera respuesta en lenguaje natural con grounding en el contexto
7. Respuesta retorna al usuario con fuentes citadas

**D. Flujo de configuración difusa (figura 3 — secuencia):**
1. Operador selecciona variable de entrada (temperatura, humedad, luminosidad) en la UI
2. Define función de membresía: tipo (triangular/trapezoidal/gaussiana/sigmoidal) + parámetros
3. `PUT /fuzzy-config/{variable}` → Control Service valida y persiste en MongoDB
4. Motor de inferencia carga nueva configuración en memoria sin reinicio
5. Próximo ciclo de lectura de sensores usa la configuración actualizada

---

### IV. Motor Difuso Configurable *(~800 palabras, 1.5 columnas)*

**Qué narrar:**

**A. Arquitectura de la v1 (baseline):**  
El sistema base (HydroEspinaca v1) implementa un controlador Mamdani MIMO con:
- 3 entradas: temperatura (°C), humedad relativa (%), luminosidad (lux)
- 5 salidas: bomba de nutrientes, difusor, calefactor, LEDs, ventilador
- 12 reglas difusas fijas (base de conocimiento estática)
- Funciones de membresía triangulares fijas
- Implementación en Python 3.12 con scikit-fuzzy
- Validado con 791 tests unitarios, 100% tasa de aprobación
- Consumo medido: 1.90 kWh/día

*Limitación del baseline:* modificar una función de membresía requiere editar código fuente, reconstruir y redesplegar el servicio.

**B. Extensión propuesta — parametrización en tiempo de ejecución:**

Definición formal de las funciones de membresía soportadas:

**(Ec. 1) Triangular:**
$$\mu_T(x; a, b, c) = \max\left(\min\left(\frac{x-a}{b-a}, \frac{c-x}{c-b}\right), 0\right)$$

**(Ec. 2) Trapezoidal:**
$$\mu_{Tr}(x; a, b, c, d) = \max\left(\min\left(\frac{x-a}{b-a}, 1, \frac{d-x}{d-c}\right), 0\right)$$

**(Ec. 3) Gaussiana:**
$$\mu_G(x; c, \sigma) = e^{-\frac{(x-c)^2}{2\sigma^2}}$$

**(Ec. 4) Sigmoidal:**
$$\mu_S(x; a, c) = \frac{1}{1 + e^{-a(x-c)}}$$

**C. Modelo de persistencia (esquema MongoDB):**
```json
{
  "variable": "temperature",
  "sets": [
    { "label": "cold", "type": "triangular", "params": [10, 15, 20] },
    { "label": "warm", "type": "gaussian", "params": [25, 2.5] },
    { "label": "hot", "type": "trapezoidal", "params": [28, 32, 40, 45] }
  ],
  "rules": [
    { "if": { "temperature": "cold" }, "then": { "heater": "high" } }
  ],
  "version": 3,
  "updatedAt": "2026-04-15T10:30:00Z"
}
```

**D. Mecanismo de recarga en caliente:**  
Describir el patrón de invalidación de caché: cuando se persiste una nueva configuración, el Control Service invalida el `IFuzzyEngine` cacheado y carga la nueva definición en el siguiente ciclo de inferencia. Sin downtime, sin pérdida de lecturas de sensores.

**E. Validación de la configuración:**  
Las reglas de validación (FluentValidation) garantizan que los parámetros de cada función de membresía sean físicamente coherentes: `a < b < c` para triangular, `a < b ≤ c < d` para trapezoidal, `σ > 0` para gaussiana. Se rechazan configuraciones que dejen zonas de la variable de entrada sin cobertura (gap en el universo de discurso).

---

### V. Módulo RAG — Recuperación de Conocimiento Técnico *(~800 palabras, 1.5 columnas)*

**Qué narrar:**

**A. Motivación específica:**  
El operador del sistema hidropónico necesita responder preguntas como: "¿Por qué el sistema está activando el ventilador con tanta frecuencia?", "¿Qué valores de conductividad eléctrica son óptimos para espinaca en etapa vegetativa?", "¿Cómo ajusto las funciones de membresía para un cultivo de lechuga?". Un LLM genérico puede alucinar sobre especificidades del sistema instalado; un RAG sobre la documentación técnica propia reduce drásticamente este problema.

**B. Arquitectura del pipeline RAG:**

*Fase de indexación (offline):*
1. Documentos fuente: manuales del sistema, fichas técnicas de actuadores, guías de cultivo de espinaca, documentación de la API interna
2. Chunking: segmentación por párrafo con overlap de 20% para preservar contexto
3. Embedding: `gemini-embedding-001` genera vectores de 768 dimensiones por chunk
4. Indexación en MongoDB Atlas con índice HNSW: `efConstruction=128`, `m=16` (parámetros de construcción del grafo jerárquico)

*Fase de inferencia (online, por consulta):*
1. Embedding de la consulta (misma dimensionalidad: 768-dim)
2. Vector Search: `$vectorSearch` con `numCandidates=150`, `limit=5` — recupera top-5 por similitud coseno
3. Construcción del prompt: contexto recuperado + historial de conversación + consulta del usuario
4. Gemini Flash 2.0: genera respuesta con grounding en los documentos recuperados
5. Post-proceso: extracción de fuentes citadas, formateo en Markdown para la UI

**C. Justificación de HNSW sobre alternativas:**

| Algoritmo | Complejidad búsqueda | Precisión recall | Overhead memoria |
|-----------|---------------------|-----------------|-----------------|
| Búsqueda lineal (brute force) | O(n·d) | 100% | Mínimo |
| IVF-Flat (FAISS) | O(√n·d) | ~90-95% | Medio |
| **HNSW** | **O(log n)** | **>95%** | Alto |
| ScaNN (Google) | O(log n) | ~95% | Alto |

HNSW seleccionado por: (a) mejor balance velocidad/precisión para corpus de tamaño mediano (<100K documentos), (b) integración nativa en MongoDB Atlas sin infraestructura vectorial adicional, (c) soporte a actualizaciones incrementales del índice sin reconstrucción completa.

**D. Gestión del historial de conversación:**  
El chatbot mantiene ventana de contexto de N turnos (configurable). Los turnos anteriores se incluyen en el prompt para coherencia conversacional. Importante para el caso de uso de configuración difusa asistida: "¿Y si en lugar de gaussiana uso trapezoidal para ese mismo conjunto?" presupone el turno anterior.

**E. Comprobación de versión de documentos (contribución de ingeniería):**  
El sistema detecta si se han cargado nuevos documentos técnicos (chunk versioning) y reindexiza automáticamente solo los chunks modificados, evitando re-embedding total del corpus en cada actualización.

---

### VI. Results and Evaluation *(~900 palabras, 1.5–2 columnas)*

**Qué narrar — dos subsecciones principales:**

**A. Evaluación del motor difuso configurable:**

*A.1 Cobertura de funciones de membresía:*  
Tabla de los 4 tipos implementados con sus parámetros, rango de validez y caso de uso típico en hidroponía.

*A.2 Pruebas de corrección del motor (tests unitarios):*  
Reportar resultados de la suite de tests del Control Service. Tabla con grupos de tests, número, y tasa de aprobación.

*A.3 Comparación de respuesta del sistema con configuraciones distintas:*  
Experimento: para la variable temperatura con el conjunto "warm", comparar la respuesta del actuador calefactor usando:
- Configuración baseline (triangular, parámetros v1 originales)
- Configuración alternativa A (gaussiana, mismos puntos centrales)
- Configuración alternativa B (trapezoidal, zona plana ampliada)

Mostrar los grados de membresía resultantes para una lectura de ejemplo (p. ej., 23°C) y la acción de control inferida. Esto demuestra concretamente que la configuración tiene impacto observable en el comportamiento del sistema.

*A.4 Latencia de recarga de configuración:*  
Tiempo entre el `PUT /fuzzy-config` y el primer ciclo de inferencia con la nueva configuración activa. Esperar < 500 ms.

**B. Evaluación del módulo RAG:**

*B.1 Dataset de evaluación:*  
Construir un conjunto de 20–30 pares (pregunta, respuesta esperada) extraídos manualmente de la documentación técnica del sistema. Categorías:
- Preguntas sobre el cultivo (p. ej., "¿Cuál es el rango óptimo de temperatura para espinaca?")
- Preguntas sobre el sistema (p. ej., "¿Qué actuador controla la humedad?")
- Preguntas de configuración (p. ej., "¿Cómo ajusto el umbral de activación de la bomba?")
- Preguntas fuera de dominio (para medir alucinación)

*B.2 Métricas RAG estándar (RAGAS framework o implementación manual):*

| Métrica | Definición | Esperado |
|---------|-----------|---------|
| **Faithfulness** | Fracción de afirmaciones en la respuesta soportadas por el contexto recuperado | > 0.85 |
| **Context Precision** | Fracción del contexto recuperado que es relevante para la pregunta | > 0.80 |
| **Context Recall** | Fracción del conocimiento necesario que fue recuperado | > 0.75 |
| **Answer Relevancy** | Similitud semántica entre pregunta y respuesta generada | > 0.80 |

*B.3 Comparación con baseline (sin RAG):*  
Mismas preguntas respondidas directamente por Gemini Flash sin contexto recuperado. Esperar mejora significativa en Faithfulness para preguntas específicas del sistema (el LLM no tiene conocimiento del sistema propietario).

*B.4 Ejemplo cualitativo:*  
Mostrar un par pregunta/respuesta completo con los chunks recuperados y la respuesta generada. Esto es obligatorio en artículos de RAG — los revisores siempre piden evidencia cualitativa.

*B.5 Latencia end-to-end:*  
Tiempo desde la recepción del mensaje hasta la primera respuesta (streaming o batch). Benchmark con 10 consultas. Desagregar: tiempo de embedding + tiempo de Vector Search + tiempo de generación LLM.

---

### VII. Discussion *(~600 palabras, 1 columna)*

**Qué narrar:**

**A. Sinergia entre los dos módulos:**  
El caso de uso más valioso del artículo no es cada módulo aislado sino su integración: el operador puede preguntar al chatbot "¿Cómo debería configurar la función de membresía para temperatura fría en verano?" y el RAG recupera la ficha técnica del cultivo con los rangos óptimos, Gemini Flash genera una recomendación en lenguaje natural con valores de parámetros específicos, y el operador aplica esa recomendación directamente en la interfaz de configuración difusa. Este ciclo asistido reduce la dependencia de expertos externos.

**B. Limitaciones:**

1. *RAG sobre corpus pequeño:* el corpus inicial del sistema es limitado (documentación del proyecto + fichas técnicas). Con corpus pequeño, el Vector Search recupera documentos marginalmente relevantes cuando la consulta está fuera del dominio. Solución parcial: implementado el filtro de preguntas fuera de dominio (si similitud máxima < umbral, rechazar con mensaje apropiado).

2. *Evaluación del motor difuso:* la comparación de configuraciones se realiza sobre datos simulados de sensores. Un experimento con el sistema físico DFT en condiciones reales de cultivo aportaría mayor validez externa. Se identifica como trabajo futuro prioritario.

3. *Latencia del LLM:* Gemini Flash tiene latencia variable según carga de la API (300 ms–3 s). Para un sistema de control en tiempo real, el chatbot es asíncrono — nunca bloquea el ciclo de inferencia difusa. Sin embargo, el usuario percibe latencia al interactuar con el chatbot en momentos de alta carga.

4. *Configuración difusa sin guardianes agronómicos:* el sistema valida la coherencia matemática de los parámetros pero no valida que la configuración sea agronómicamente sensata (p. ej., definir "temperatura óptima" en 50°C no causará error de sistema, pero sí daño al cultivo). El RAG puede alertar, pero no hay restricción de dominio dura.

**C. Comparación con la v1:**  
Posicionar cuantitativamente: la v1 con configuración estática requería intervención técnica de ~2–4 horas para modificar funciones de membresía (editar código, tests, rebuild, redeploy). Con la v2, el mismo cambio toma < 30 segundos desde la UI. El chatbot RAG reduce las consultas de soporte estimadas al equipo técnico al poder resolver preguntas de documentación directamente en la plataforma.

**D. Trabajo futuro:**
- Validación longitudinal con múltiples ciclos de cultivo y operadores reales (estudio de usabilidad)
- Integración del BI/DSS (Opción A) con el chatbot: consultas como "¿fue rentable el último ciclo y por qué?"
- Extensión del corpus RAG con literatura agrícola open-access indexada automáticamente
- Soporte a Type-2 Fuzzy Logic para manejo de incertidumbre en las propias funciones de membresía

---

### VIII. Conclusions *(~250 palabras, 0.5 columna)*

**Qué narrar:**

**Párrafo 1 — Síntesis:**  
Se presentó una arquitectura de doble capa de inteligencia para sistemas hidropónicos IoT que combina un motor de control difuso Mamdani MIMO con funciones de membresía paramétricas configurables en tiempo de ejecución y un módulo RAG basado en Gemini Flash con búsqueda vectorial HNSW para recuperación de conocimiento técnico agrícola. Ambos módulos se integran sobre una arquitectura de microservicios con Clean Architecture y CQRS, sobre el sistema base HydroEspinaca v1 previamente validado.

**Párrafo 2 — Contribución principal:**  
La contribución central del trabajo no es la implementación aislada de RAG ni la parametrización difusa, sino la demostración de su sinergia operacional: el LLM asiste al operador en la comprensión y configuración del sistema de control, reduciendo la barrera de conocimiento experto necesario para operar un sistema hidropónico IoT avanzado.

**Párrafo 3 — Impacto:**  
El sistema está operativo en un entorno de producción real con espinaca (*Spinacia oleracea*) en configuración DFT. Los resultados de evaluación sugieren que la combinación propuesta es viable para su adopción en sistemas de automatización agrícola de mediana escala donde la intervención técnica experta es costosa o infrecuente.

**Párrafo 4 — Trabajo futuro:**  
Estudios de usabilidad con operadores reales, validación agronómica de las configuraciones generadas por el chatbot, y extensión del módulo de análisis económico (BI/DSS) con interfaz de consulta en lenguaje natural son las líneas de investigación inmediatas.

---

## Test Cases a Ejecutar y Resultados a Exponer

### Suite existente (ejecutar antes de escribir la sección de Resultados)

```bash
# Tests del Control Service (fuzzy configurable)
dotnet test --filter "FuzzyConfig" --logger "console;verbosity=detailed"

# Tests del Chatbot Service
dotnet test --filter "Chatbot" --logger "console;verbosity=detailed"

# Suite completa del proyecto
dotnet test --logger "console;verbosity=normal"
```

### Tests adicionales a implementar para fortalecer el artículo

| Test a crear | Qué valida | Por qué es clave para el artículo |
|---|---|---|
| `FuzzyEngine_TriangularMembership_ReturnsCorrectDegree` | µ_T(x; a, b, c) con valores en cada zona (izquierda, pico, derecha, fuera) | Valida Ec. 1 — evidencia directa de corrección matemática |
| `FuzzyEngine_GaussianMembership_ReturnsCorrectDegree` | µ_G(x; c, σ) con x=c (µ=1.0) y x=c±σ (µ=0.607) | Valida Ec. 3 — los revisores verificarán esta ecuación |
| `FuzzyEngine_InvalidParams_ShouldThrowValidationError` | Parámetros incoherentes (a > b para triangular) → rechazo | Demuestra robustez de la contribución de validación |
| `FuzzyEngine_HotReload_ShouldUseNewConfigOnNextCycle` | Después de actualizar configuración, el motor usa nueva config sin reinicio | Valida la contribución de recarga en caliente |
| `FuzzyEngine_AllMembershipTypes_ShouldProduceConsistentOutput` | Comparación de salida para los 4 tipos con mismos puntos de referencia | Base para la Tabla comparativa de configuraciones en Resultados |
| `ChatbotHandler_WithRelevantContext_ShouldReturnGroundedResponse` | RAG retorna respuesta con contexto recuperado incluido | Valida el pipeline RAG end-to-end |
| `ChatbotHandler_OutOfDomainQuery_ShouldIndicateLowConfidence` | Pregunta sin relación con el corpus → respuesta apropiada | Valida manejo de alucinación |
| `VectorSearch_ShouldReturnTopKByCosineSimilarity` | Embedding de consulta → top-K chunks por similitud coseno correcta | Valida la recuperación HNSW |

### Experimento de evaluación RAG (construir manualmente)

Crear el archivo `docs/RAG_EVAL_DATASET.json` con 25–30 pares:
```json
[
  {
    "question": "¿Cuál es el rango óptimo de temperatura para espinaca hidropónica?",
    "expected_answer": "Entre 15°C y 22°C para etapa vegetativa...",
    "category": "crop_knowledge"
  },
  ...
]
```

Ejecutar el pipeline RAG sobre este dataset y reportar las 4 métricas RAGAS en una tabla.  
**Esta tabla es el resultado más importante del artículo.**

### Experimento de configuración difusa (comparación)

Preparar 3 configuraciones del conjunto "temperatura_cálida":
- Config A (baseline v1): triangular [20, 25, 30]
- Config B: gaussiana [25, 2.0]
- Config C: trapezoidal [22, 24, 27, 30]

Para cada configuración, registrar el grado de membresía µ para los valores {18, 20, 22, 23, 25, 27, 28, 30, 32}°C.  
Graficar como figura de comparación de curvas (figura 4 del artículo).  
Mostrar la acción de control (% activación calefactor) inferida para una lectura de 23°C con cada config.

---

## Artículos Citados en los PDFs — Aporte al Artículo

> Los siguientes trabajos están referenciados en los libros del proyecto (v1 y v2) o son directamente inferidos de las elecciones técnicas documentadas. Se organiza por bloque de Related Work donde aportan.

### Fundamentos de lógica difusa (Bloque A y B)

| Referencia | Aporte específico |
|---|---|
| **Zadeh, L.A. (1965).** "Fuzzy sets." *Information and Control*, 8(3), 338–353. | Definición formal de conjuntos difusos y grados de membresía. Citar en Introducción y en la Ec. 1–4 como fundamento teórico de las funciones implementadas. |
| **Mamdani, E.H. & Assilian, S. (1975).** "An experiment in linguistic synthesis with a fuzzy logic controller." *International Journal of Man-Machine Studies*, 7(1), 1–13. | Origen del controlador Mamdani MIMO usado como base del sistema. Citar en Sección IV al describir el baseline v1. |
| **IEEE Std 1855-2016.** *IEEE Standard for Fuzzy Markup Language.* IEEE, 2016. | Estándar para representación parametrizable de sistemas difusos. Citar en Sección IV.B al justificar la arquitectura de configuración dinámica como convergente con el estándar. |

### RAG y LLMs (Bloque C)

| Referencia | Aporte específico |
|---|---|
| **Lewis, P. et al. (2020).** "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks." *NeurIPS 2020*. | Trabajo seminal que define el paradigma RAG. Citar al inicio de la Sección V como la base metodológica del módulo. |
| **Google DeepMind (2025).** *Gemini Flash Technical Report / Gemini 2.0 Flash Evaluation.* | Justificación de la elección del modelo: MMLU-Pro 60%+, GPQA Diamond 90%, velocidad de inferencia. Citar en Metodología al describir la elección del LLM. |
| **Google (2024).** *gemini-embedding-001 Model Card.* | Justificación de los embeddings de 768 dimensiones. Citar en Sección V.B al describir la fase de indexación. |

### Búsqueda vectorial ANN (Bloque D)

| Referencia | Aporte específico |
|---|---|
| **Malkov, Y.A. & Yashunin, D.A. (2020).** "Efficient and Robust Approximate Nearest Neighbor Search Using Hierarchical Navigable Small World Graphs." *IEEE Transactions on Pattern Analysis and Machine Intelligence*, 42(4), 824–836. | Algoritmo HNSW. Citar en Sección V.C en la tabla comparativa de algoritmos ANN y al describir los parámetros `efConstruction` y `m`. |
| **MongoDB Inc. (2024).** *MongoDB Atlas Vector Search Documentation.* | Implementación específica de HNSW usada. Citar en Sección III y V como referencia de implementación. |

### Sistemas hidropónicos IoT (Bloque A)

| Referencia | Aporte específico |
|---|---|
| **Moreno Beltrán, S. (2025).** *HydroEspinaca v1: Sistema de Control Difuso para Cultivo Hidropónico de Espinaca.* Trabajo de grado, Universidad de Cundinamarca. | El sistema base validado sobre el que se construye la v2. Citar en Introducción párrafo 3 y en la descripción del baseline (Sección IV.A). Es la referencia más importante del artículo. |
| **Artículos de control IoT para NFT/DFT** (varios, del libro v1) | Contexto del Related Work bloque A. Confirmar títulos exactos en la bibliografía del libro v1. |

### Arquitectura de software (Bloque C)

| Referencia | Aporte específico |
|---|---|
| **Martin, R.C. (2017).** *Clean Architecture: A Craftsman's Guide to Software Structure and Design.* Prentice Hall. | Justificación del patrón arquitectónico de microservicios usado. Citar en Sección III al describir la estructura de capas. |
| **Richardson, C. (2018).** *Microservices Patterns.* Manning Publications. | Patrón BFF (Backend For Frontend) y CQRS. Citar en Sección III. |

---

## Referencias Adicionales a Buscar

Las siguientes categorías/temas complementarán el artículo con literatura de calidad. Se ordenan por prioridad.

### Prioridad Alta (necesarias para el artículo)

| Categoría | Qué buscar | Dónde buscar |
|---|---|---|
| **Evaluación de sistemas RAG** | Métricas: RAGAS framework (Shahul Es et al., 2023), ARES, TruLens. Papers sobre cómo evaluar sistemas RAG end-to-end. | IEEE Xplore, arXiv (cs.IR, cs.CL) |
| **LLMs en agricultura de precisión** | Aplicaciones de ChatGPT/Gemini/Llama en diagnóstico de cultivos, recomendación de fertilización, chatbots agronómicos. Buscar "LLM precision agriculture" o "GPT agriculture" en IEEE Xplore (2023–2025). | IEEE Access, Computers and Electronics in Agriculture |
| **Sistemas difusos adaptativos para IoT** | Papers que implementen fuzzy logic con parámetros ajustables en tiempo de ejecución en sistemas embebidos o microservicios. Buscar "adaptive fuzzy IoT" o "runtime fuzzy configuration". | IEEE Transactions on Fuzzy Systems, Soft Computing |
| **RAG sobre documentación técnica (not general knowledge)** | Papers que aplican RAG sobre manuales técnicos propietarios, no sobre Wikipedia o bases de conocimiento generales. Buscar "RAG technical documentation" o "domain-specific RAG". | arXiv, ACL Anthology |

### Prioridad Media (enriquecen el contexto)

| Categoría | Qué buscar |
|---|---|
| **Hidroponía DFT/NFT con control IoT** | Papers sobre el sistema DFT (Deep Flow Technique) específicamente con control automatizado. Confirmar consumos energéticos reportados en literatura y comparar con el 1.90 kWh/día de la v1. |
| **Vector databases comparison** | Surveys comparativos de Pinecone vs. Weaviate vs. MongoDB Atlas Vector Search vs. pgvector. Justifica la elección de MongoDB. |
| **Fuzzy Logic Type-2 en sistemas de control** | Para mencionar como limitación y trabajo futuro informado. Los Type-2 manejan incertidumbre en las propias funciones de membresía. |
| **Human-in-the-loop para sistemas de control agrícola** | Papers sobre interfaces que permiten al operador intervenir en sistemas automáticos. Contextualiza la contribución del control configurable. |

### Prioridad Baja (trabajo futuro y discusión)

| Categoría | Para qué sirve |
|---|---|
| **Multimodal RAG (imagen + texto)** | Mención en trabajo futuro: integrar imágenes de plantas para diagnóstico visual asistido por RAG. |
| **Edge computing para inferencia LLM** | Discusión sobre el trade-off de latencia al usar Gemini (API externa) vs. modelo local en el servidor. |
| **Aquaponics and controlled environment agriculture (CEA)** | Para ampliar el contexto de la aplicación más allá de la espinaca hidropónica. |

### Journals y Conferencias IEEE donde publicar

| Venue | Factor de Impacto / Nivel | Justificación |
|---|---|---|
| **IEEE Access** | Q2, IF ~3.4 | Open access, proceso rápido (~6 semanas), acepta sistemas completos con validación experimental. Primera opción. |
| **IEEE Transactions on AgriFood Electronics** | Q1 emergente | Nuevo journal IEEE enfocado exactamente en este dominio. |
| **Computers and Electronics in Agriculture** (Elsevier, compatible IEEE format) | Q1, IF ~8.3 | El journal de referencia en AgriTech. Mayor impacto pero proceso más exigente. |
| **IEEE International Conference on Fuzzy Systems (FUZZ-IEEE)** | Conferencia A | Para la contribución de fuzzy configurable; las conferencias son más rápidas para validar contribuciones antes de un journal. |
| **IEEE/ACM International Conference on IoT** | Conferencia B | Para el sistema completo como contribución de ingeniería. |

---

## Checklist Pre-Escritura

Antes de empezar a redactar el artículo, verificar:

- [ ] Ejecutar la suite completa de tests y documentar los resultados
- [ ] Construir el dataset de evaluación RAG (25–30 pares pregunta/respuesta)
- [ ] Ejecutar las métricas RAGAS sobre el dataset
- [ ] Preparar las 3 configuraciones difusas de comparación y generar los datos de la figura
- [ ] Preparar captura de pantalla del chatbot respondiendo una pregunta técnica con fuentes
- [ ] Preparar captura de pantalla de la UI de configuración difusa
- [ ] Preparar el diagrama de arquitectura de microservicios (draw.io o similar)
- [ ] Preparar el diagrama de secuencia del pipeline RAG
- [ ] Verificar que todas las citas tienen DOI o URL institucional accesible
- [ ] Confirmar los títulos exactos de las referencias del libro v1 en la bibliografía original
- [ ] Elegir el journal/conferencia destino y revisar su template IEEE (doble columna)

---

*Plan generado por Claude Sonnet 4.6 como insumo inicial de investigación. Los valores numéricos de los experimentos son de referencia — reemplazar con resultados reales antes de someter el artículo.*
