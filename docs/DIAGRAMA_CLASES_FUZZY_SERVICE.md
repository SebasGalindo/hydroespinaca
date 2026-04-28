# Diagrama de Clases — Fuzzy Service (Capa de Dominio)

Capa de dominio del microservicio `fuzzy-service` (Python / FastAPI / Pydantic v2)
ubicado en
[software-project/fuzzy-service/FuzzyService/Domain/](../software-project/fuzzy-service/FuzzyService/Domain/).

Responsabilidad: modelar sistemas fuzzy Mamdani completos (variables, términos,
reglas con consecuentes multi-término), ejecutar inferencia y persistir
evaluaciones para auditoría, comunicándose con `actuator-service`,
`sensor-service` y MQTT mediante puertos.

Por la cantidad de tipos, el dominio se descompone en cuatro vistas.

---

## 1. Value Objects de Identidad

Jerarquía de IDs basada en `RootModel[str]` que admite tanto `ObjectId` (BSON)
como `UUID`, y normaliza el formato Mongo `{"$oid": "..."}`.

```mermaid
classDiagram
    direction TB

    class DomainBaseModel {
        <<Pydantic BaseModel>>
        +model_config: ConfigDict
        +__init__(*args, **kwargs)
    }

    class DomainId {
        <<RootModel~str~ — frozen>>
        +root: str
        +generate() DomainId
        +__str__() str
        +__eq__(other) bool
        +__hash__() int
    }

    class FuzzySystemId
    class FuzzyVariableId
    class FuzzyTermId
    class FuzzyRuleId
    class FuzzyRoutineId
    class FuzzyEvaluationId
    class ActuatorId

    DomainId <|-- FuzzySystemId
    DomainId <|-- FuzzyVariableId
    DomainId <|-- FuzzyTermId
    DomainId <|-- FuzzyRuleId
    DomainId <|-- FuzzyRoutineId
    DomainId <|-- FuzzyEvaluationId
    DomainId <|-- ActuatorId
```

---

## 2. Enumeraciones del Dominio

```mermaid
classDiagram
    direction LR

    class FuzzySystemStatus {
        <<enumeration>>
        DRAFT
        ACTIVE
        INACTIVE
        TESTING
        +get_operational_statuses()
        +get_editable_statuses()
    }

    class FuzzyVariableType {
        <<enumeration>>
        INPUT = "input"
        OUTPUT = "output"
    }

    class EvaluationStatus {
        <<enumeration>>
        PENDING
        PROCESSING
        COMPLETED
        FAILED
        CANCELLED
    }

    class MembershipFunctionType {
        <<enumeration>>
        TRIANGULAR
        TRAPEZOIDAL
        GAUSSIAN
        SIGMOID
        BELL
        PI_SHAPED
        S_SHAPED
        Z_SHAPED
        LINEAR
        CONSTANT
    }

    class DefuzzificationMethod {
        <<enumeration>>
        CENTROID
        BISECTOR
        MOM
        SOM
        LOM
        WEIGHTED_AVERAGE
    }

    class AggregationMethod {
        <<enumeration>>
        MAX
        SUM
        PROBOR
    }

    class LogicalOperator {
        <<enumeration>>
        IS
        IS_NOT
        NOT
        GREATER_THAN
        LESS_THAN
        GREATER_EQUAL
        LESS_EQUAL
        BETWEEN
        IN
        NOT_IN
    }

    class RuleConnector {
        <<enumeration>>
        AND
        OR
    }

    class AndOperatorMethod {
        <<enumeration>>
        MIN
        PROD
    }

    class OrOperatorMethod {
        <<enumeration>>
        MAX
        SUM
        PROBOR
    }

    class NotOperatorMethod {
        <<enumeration>>
        COMPLEMENT
    }

    class PowerRange {
        <<enumeration>>
        PERCENT_0_100
        +min: float
        +max: float
    }

    class DurationRange {
        <<enumeration>>
        SECONDS_5_60
        +min: float
        +max: float
    }
```

---

## 3. Entidades, Value Objects de Configuración y Evaluación

```mermaid
classDiagram
    direction LR

    class DomainBaseModel {
        <<base>>
    }

    class FuzzySystem {
        +id: FuzzySystemId?
        +name: str
        +status: FuzzySystemStatus
        +defuzzification_method: DefuzzificationMethod
        +operators: OperatorsConfig
        +input_variable_ids: List~FuzzyVariableId~
        +output_variable_ids: List~FuzzyVariableId~
        +rule_ids: List~FuzzyRuleId~
        +created_at, updated_at, created_by
        +add_input_variable(id)
        +add_output_variable(id)
        +add_rule(id)
        +activate() / deactivate() / set_testing_mode()
        +is_operational() bool
        +is_editable() bool
        +update_configuration(...)
    }

    class FuzzyVariable {
        +id: FuzzyVariableId?
        +name: str
        +description: str
        +variable_type: "input" | "output"
        +actuator_type: "PWM" | "DIGITAL"?
        +defuzzification_threshold: float
        +universe_min, universe_max: float?
        +reference_code: str?
        +terms: List~FuzzyTermId~
        +add_term(id) / remove_term(id) / reorder_terms(...)
        +is_input() bool
        +is_output() bool
    }

    class FuzzyTerm {
        +id: FuzzyTermId?
        +variable_id: FuzzyVariableId?
        +label: str
        +membership_function: MembershipFunction
        +update_label(label)
        +update_membership_function(mf)
    }

    class FuzzyRule {
        +id: FuzzyRuleId?
        +name: str
        +system_id: FuzzySystemId?
        +description: str?
        +conditions: List~Dict~
        +connectors: List~RuleConnector~
        +consequents: List~RuleConsequent~
        +add_condition(varId, op, value, connector?)
        +remove_condition(varId)
        +update_condition(varId, op, value)
        +set_connector_at(idx, conn)
        +add_consequent(c) / remove_consequent(varId)
        +get_rule_text() str
    }

    class RuleConsequent {
        +variable_id: FuzzyVariableId
        +terms: List~FuzzyTermId~
        +aggregation_method: "max" | "sum" | "probabilistic_or"
        +add_term(id) / remove_term(id)
        +has_term(id) bool
    }

    class MembershipFunction {
        <<value object>>
        +function_type: MembershipFunctionType
        +parameters: List~float~
        +universe_min, universe_max: float
        +get_parameter_names() List~str~
    }

    class OperatorsConfig {
        <<value object>>
        +and_method: AndOperatorMethod = MIN
        +or_method: OrOperatorMethod = MAX
        +not_method: NotOperatorMethod = COMPLEMENT
    }

    class RuleCondition {
        <<value object>>
        +condition_id: str
        +sensor_name: str
        +operator: LogicalOperator
        +target_value: str | List~str~
        +is_linguistic_condition() bool
    }

    class FuzzyValue {
        <<value object>>
        +crisp_value: float
        +membership_degree: float
        +linguistic_label: str?
        +is_fully_member() bool
        +is_partial_member() bool
    }

    class FuzzySet {
        <<value object>>
        +name: str
        +values: Tuple~FuzzyValue~
        +get_max_membership() FuzzyValue
        +get_alpha_cut(alpha) Tuple~FuzzyValue~
        +get_support() Tuple~FuzzyValue~
        +get_core() Tuple~FuzzyValue~
    }

    class FuzzyEvaluation {
        +id: FuzzyEvaluationId?
        +system_id: FuzzySystemId?
        +timestamp: datetime?
        +inputs: List~InputValue~
        +activated_rules: List~RuleActivation~
        +add_input(sensorId, value)
        +add_rule_activation(ra)
    }

    class InputValue {
        <<value object>>
        +sensor_id: str
        +value: float
    }

    class OutputValue {
        <<value object>>
        +reference_code: str
        +power: "ON"|"OFF"?
        +dutyCycle: float?
        +duration: float
    }

    class RuleActivation {
        <<value object>>
        +rule_id: FuzzyRuleId | str
        +firing_strength: float [0..1]
        +output_values: List~OutputValue~
    }

    DomainBaseModel <|-- FuzzySystem
    DomainBaseModel <|-- FuzzyVariable
    DomainBaseModel <|-- FuzzyTerm
    DomainBaseModel <|-- FuzzyRule
    DomainBaseModel <|-- RuleConsequent
    DomainBaseModel <|-- MembershipFunction
    DomainBaseModel <|-- OperatorsConfig
    DomainBaseModel <|-- RuleCondition
    DomainBaseModel <|-- FuzzyValue
    DomainBaseModel <|-- FuzzySet
    DomainBaseModel <|-- FuzzyEvaluation
    DomainBaseModel <|-- InputValue
    DomainBaseModel <|-- OutputValue
    DomainBaseModel <|-- RuleActivation

    FuzzySystem "1" o-- "*" FuzzyVariable : input + output ids
    FuzzySystem "1" o-- "*" FuzzyRule : rule_ids
    FuzzySystem "1" *-- "1" OperatorsConfig : operators
    FuzzyVariable "1" o-- "*" FuzzyTerm : terms
    FuzzyTerm "1" *-- "1" MembershipFunction : mf
    FuzzyRule "1" *-- "*" RuleConsequent : consequents
    RuleConsequent ..> FuzzyVariable : variable_id
    RuleConsequent ..> FuzzyTerm : terms
    FuzzyEvaluation "1" *-- "*" InputValue : inputs
    FuzzyEvaluation "1" *-- "*" RuleActivation : activated_rules
    RuleActivation "1" *-- "*" OutputValue : output_values
    FuzzySet "1" *-- "*" FuzzyValue : values
```

---

## 4. Interfaces de Repositorio y Puertos Externos

```mermaid
classDiagram
    direction LR

    class IFuzzySystemRepository {
        <<ABC>>
        +create(system) FuzzySystem
        +get_by_id(id) FuzzySystem?
        +get_by_name(name) FuzzySystem?
        +get_all(skip, limit) List
        +update(system) FuzzySystem
        +update_status(id, status) FuzzySystem
        +delete(id) bool
        +get_active_systems(...) List
        +deactivate_all_active() int
        +get_systems_with_input_variable(varId)
        +get_systems_with_output_variable(varId)
        +filter_systems(filters, ...)
    }

    class IFuzzyVariableRepository {
        <<ABC>>
        +create(var) / update / delete
        +get_by_id / get_by_name / get_all
        +get_by_system_id(systemId)
        +get_by_type(varType)
        +get_input_variables_by_system(id)
        +get_output_variables_by_system(id)
        +get_variables_with_term(termId)
        +create_many(...) / update_many_terms(...)
    }

    class IFuzzyTermRepository {
        <<ABC>>
        +create / update / delete
        +get_by_id / get_all
        +get_by_variable_id(varId)
        +get_by_label(varId, label)
        +search_by_label(varId, pattern)
        +create_many(terms)
    }

    class IFuzzyRuleRepository {
        <<ABC>>
        +create / update / delete
        +get_by_id / get_all
        +get_by_system_id(systemId)
        +get_by_name(systemId, name)
        +get_rules_using_variable(varId)
        +get_rules_with_connector(conn)
        +get_all_rules_name_description(...)
        +create_many(rules)
    }

    class IFuzzyEvaluationRepository {
        <<ABC>>
        +create(eval) / update / delete
        +get_by_id / get_all
        +get_by_system_id(systemId)
        +get_by_rule_id(ruleId)
        +get_by_date_range(start, end)
        +filter_evaluations(filters)
    }

    class IFuzzyEngine {
        <<ABC — domain service>>
        +fuzzify_sensor_readings(vars, terms, readings) List~FuzzificationResult~
        +complete_fuzzy_evaluation(system, vars, terms, rules, readings, isSimulation) Dict
    }

    class IActuatorService {
        <<ABC — port>>
        +send_routines(payload) bool
        +is_available() bool
        +validate_output_exists(outputId) bool
        +get_output_details(outputId) Dict?
    }

    class ISensorService {
        <<ABC — port>>
        +validate_variable_exists(varId) bool
        +get_variable_details(varId) Dict?
        +is_available() bool
    }

    class IMqttService {
        <<ABC — port>>
        +publish(topic, payload, qos, retain)
        +subscribe(topic, qos)
        +unsubscribe(topic)
        +is_connected() bool
    }

    IFuzzySystemRepository ..> FuzzySystem : maneja
    IFuzzyVariableRepository ..> FuzzyVariable : maneja
    IFuzzyTermRepository ..> FuzzyTerm : maneja
    IFuzzyRuleRepository ..> FuzzyRule : maneja
    IFuzzyEvaluationRepository ..> FuzzyEvaluation : maneja
    IFuzzyEngine ..> FuzzySystem : evalúa
    IFuzzyEngine ..> FuzzyVariable
    IFuzzyEngine ..> FuzzyTerm
    IFuzzyEngine ..> FuzzyRule
```

---

## Notas de diseño

- **Pydantic v2 como base de dominio**: `DomainBaseModel` configura `validate_assignment=True`, `extra="forbid"`, `populate_by_name=True` y soporte para argumentos posicionales — habilita validaciones invariantes en cada mutación sin acoplar al framework.
- **IDs polimórficos por subclase**: `DomainId` es un `RootModel[str]` frozen que valida tanto `ObjectId` BSON como UUID. Las subclases (`FuzzySystemId`, etc.) dan tipado fuerte al pasar IDs entre repos sin permitir mezclar.
- **Mamdani con consecuentes multi-término**: `RuleConsequent.terms: List[FuzzyTermId]` permite que una regla active varios términos por variable de salida; `aggregation_method` decide cómo combinarlos (`max`/`sum`/`probabilistic_or`). Habilita agregación multi-regla antes de defuzzificar.
- **Reglas separan condiciones y conectores**: `len(connectors) == max(0, len(conditions) - 1)` se valida en `_validate_and_finalize`. Sólo se permiten `IS` / `IS_NOT` (etiquetas lingüísticas) — los operadores numéricos están explícitamente bloqueados.
- **`reference_code` como puente entre microservicios**: variables de entrada mapean al `code` de un sensor (`sensor-service`); variables de salida mapean al `code` del actuator (`actuator-service`). Evita compartir IDs internos entre bounded contexts.
- **Estados con políticas explícitas**: `FuzzySystemStatus.get_operational_statuses()` y `get_editable_statuses()` definen reglas — el sistema valida activación contra `_validate_for_activation` (mín. 1 input, 1 output, 1 regla).
- **`OutputValue` con campos mutuamente exclusivos**: `power` (digital ON/OFF) o `dutyCycle` (PWM 0-100) — el `model_validator` lo verifica. `duration` se valida contra el rango de la enum `DurationRange`.
- **Puertos hexagonales**: `IFuzzyEngine` (servicio de dominio), `IActuatorService`, `ISensorService` e `IMqttService` son ABCs en el dominio que la infraestructura implementa — el dominio no conoce HTTP ni el broker MQTT.
- **Auditoría con `FuzzyEvaluation`**: cada inferencia se persiste con `inputs` (lecturas de sensores en ese momento) y `activated_rules` (con `firing_strength` y `output_values` resultantes) — útil para reentrenar reglas y para el contexto del chatbot.
