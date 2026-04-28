# Diagrama de Clases — Chatbot Service (Capa de Dominio)

Capa de dominio del microservicio `chatbot-service` ubicado en
[software-project/chatbot-service/ChatbotService.Domain/](../software-project/chatbot-service/ChatbotService.Domain/).

Responsabilidad: orquestar conversaciones con el LLM (Gemini) usando RAG sobre
MongoDB Atlas Vector Search, con contexto en tiempo real obtenido vía clientes
HTTP a `fuzzy-service`, `sensor-service` y `actuator-service`.

---

## 1. Entidades y Modelos de Dominio

```mermaid
classDiagram
    direction LR

    class IIdentifiableMutable {
        <<interface>>
        +string Id
        +SetId(string id) void
    }

    class ChatSession {
        +string Id
        +string UserId
        +string Title
        +List~ChatMessage~ Messages
        +bool IsArchived
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +SetId(string id) void
        +AddMessage(ChatMessage msg) void
    }

    class ChatMessage {
        +string Role
        +string Content
        +DateTime Timestamp
        +int? TokensUsed
    }

    class KnowledgeChunk {
        +string Id
        +string SourceType
        +string SourceId
        +string Content
        +float[] Embedding
        +object? Metadata
        +int Version
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +double? Score
        +SetId(string id) void
    }

    class EmbeddingTaskType {
        <<enumeration>>
        RetrievalQuery
        RetrievalDocument
    }

    IIdentifiableMutable <|.. ChatSession
    IIdentifiableMutable <|.. KnowledgeChunk
    ChatSession "1" *-- "*" ChatMessage : Messages
```

---

## 2. Modelos de Hidratación Fuzzy (RAG)

Records inmutables que representan entidades del `fuzzy-service` ya hidratadas
(con sus relaciones resueltas) listas para inyectarse en el prompt del LLM.

```mermaid
classDiagram
    direction TB

    class HydratedFuzzySystem {
        <<record>>
        +string Id
        +string Name
        +string Description
        +string Status
        +string DefuzzificationMethod
        +List~string~ InputVariables
        +List~string~ OutputVariables
        +List~string~ RuleNames
    }

    class HydratedFuzzyVariable {
        <<record>>
        +string Id
        +string Name
        +string SystemName
        +string VariableType
        +string Description
        +double UniverseMin
        +double UniverseMax
        +string ReferenceCode
        +List~HydratedFuzzyTerm~ Terms
    }

    class HydratedFuzzyTerm {
        <<record>>
        +string Id
        +string Label
        +string FunctionType
        +List~double~ Parameters
    }

    class HydratedFuzzyRule {
        <<record>>
        +string Id
        +string Name
        +string SystemName
        +string Description
        +string RuleText
        +List~HydratedRuleCondition~ Conditions
        +List~string~ Connectors
        +List~HydratedRuleConsequent~ Consequents
    }

    class HydratedRuleCondition {
        <<record>>
        +string VariableName
        +string Operator
        +string TermLabel
    }

    class HydratedRuleConsequent {
        <<record>>
        +string VariableName
        +List~string~ TermLabels
        +string AggregationMethod
    }

    HydratedFuzzyVariable "1" *-- "*" HydratedFuzzyTerm : Terms
    HydratedFuzzyRule "1" *-- "*" HydratedRuleCondition : Conditions
    HydratedFuzzyRule "1" *-- "*" HydratedRuleConsequent : Consequents
```

---

## 3. Interfaces de Repositorio, Proveedores y Clientes Externos

```mermaid
classDiagram
    direction LR

    class IChatSessionRepository {
        <<interface>>
        +GetByIdAsync(sessionId) Task~ChatSession?~
        +CreateAsync(session) Task
        +GetSessionsByUserAsync(userId, skip, limit) Task~List~ChatSession~~
        +AddMessageToSessionAsync(sessionId, message) Task
        +DeleteAsync(sessionId) Task~bool~
        +UpdateTitleAsync(sessionId, title) Task
    }

    class IKnowledgeChunkRepository {
        <<interface>>
        +GetBySourceAsync(sourceId, sourceType) Task~KnowledgeChunk?~
        +UpsertAsync(chunk) Task
        +DeleteBySourceAsync(sourceId, sourceType) Task~bool~
        +GetAllBySourceTypeAsync(sourceType) Task~List~KnowledgeChunk~~
        +GetAllAsync() Task~List~KnowledgeChunk~~
    }

    class IEmbeddingProvider {
        <<interface>>
        +GenerateEmbeddingAsync(text, taskType) Task~float[]~
    }

    class IVectorStore {
        <<interface>>
        +SearchSimilarAsync(queryEmbedding, topK, sourceTypeFilter?) Task~List~KnowledgeChunk~~
    }

    class ILlmProvider {
        <<interface>>
        +GenerateStreamAsync(systemInstruction, history, userMessage) IAsyncEnumerable~string~
    }

    class ILiveContextProvider {
        <<interface>>
        +GetSensorReadingsSummaryAsync(lastHours) Task~string~
        +GetRecentEvaluationsSummaryAsync(lastHours) Task~string~
        +GetActuatorStatesSummaryAsync() Task~string~
    }

    class IFuzzyServiceClient {
        <<interface — HTTP>>
        +GetRecentEvaluationsSummaryAsync(lastHours) Task~string~
        +GetEntityByIdAsync(sourceType, sourceId) Task~JsonElement?~
        +GetAllEntitiesByTypeAsync(sourceType) Task~List~JsonElement~~
    }

    class ISensorServiceClient {
        <<interface — HTTP>>
        +GetLatestReadingsSummaryAsync() Task~string~
    }

    class IActuatorServiceClient {
        <<interface — HTTP>>
        +GetActuatorStatesSummaryAsync() Task~string~
    }

    class IFuzzyEntityHydratorService {
        <<interface>>
        +GetHydratedSystemAsync(systemId) Task~HydratedFuzzySystem?~
        +GetHydratedVariableAsync(variableId) Task~HydratedFuzzyVariable?~
        +GetHydratedRuleAsync(ruleId) Task~HydratedFuzzyRule?~
    }

    IChatSessionRepository ..> ChatSession : maneja
    IKnowledgeChunkRepository ..> KnowledgeChunk : maneja
    IEmbeddingProvider ..> EmbeddingTaskType : usa
    IVectorStore ..> KnowledgeChunk : devuelve top-K
    ILlmProvider ..> ChatMessage : recibe history
    IFuzzyEntityHydratorService ..> HydratedFuzzySystem : devuelve
    IFuzzyEntityHydratorService ..> HydratedFuzzyVariable : devuelve
    IFuzzyEntityHydratorService ..> HydratedFuzzyRule : devuelve
    IFuzzyEntityHydratorService ..> IFuzzyServiceClient : usa
```

---

## Notas de diseño

- **RAG sobre MongoDB Atlas Vector Search**: `KnowledgeChunk` almacena `Embedding: float[]` (768 dims, generado por `gemini-embedding-001`) y se indexa en Atlas. `IVectorStore.SearchSimilarAsync` usa `$vectorSearch` para recuperar los top-K más similares al embedding del query.
- **Identificador compuesto de chunks**: la pareja `(SourceType, SourceId)` es la clave funcional — `SourceType ∈ {fuzzy_rule, fuzzy_system, system_manual}`. `Version` se incrementa en cada re-vectorización para detectar cambios.
- **Streaming de respuestas LLM**: `ILlmProvider.GenerateStreamAsync` retorna `IAsyncEnumerable<string>` para tokens incrementales — habilita SSE/streaming hacia el cliente.
- **Contexto vivo separado del RAG**: `ILiveContextProvider` consulta KPIs en tiempo real (sensores, evaluaciones, actuadores) sin gastar embeddings. Internamente delega en los clientes HTTP de cada microservicio.
- **Frontera de microservicios respetada**: `IFuzzyServiceClient`, `ISensorServiceClient`, `IActuatorServiceClient` son puertos HTTP — el chatbot **no** consulta directamente las colecciones Mongo de otros bounded contexts.
- **Hidratación de entidades fuzzy**: `IFuzzyEntityHydratorService` resuelve referencias entre sistemas/variables/términos/reglas y produce records (`HydratedFuzzy*`) listos para inyectar en el prompt sin que el LLM tenga que navegar relaciones.
- **Sesión como aggregate root**: `ChatSession` agrega `ChatMessage` directamente; `AddMessage()` actualiza `UpdatedAt` para mantener el orden cronológico al listar sesiones del usuario.
