"""Tests básicos para funcionalidades completamente implementadas.

Este archivo contiene tests simples para componentes que están 100% implementados
y no requieren configuraciones complejas.
"""

import pytest
from datetime import datetime, timezone
from domain.models import (
    Variable, FuzzySet, MembershipFunctionType,
    ActuatorState, ControlParameters, FuzzyCondition,
    FuzzyRule, LogicalOperator, ThresholdRule,
    ActuatorMapping, ActuatorType, OutputPlan, Routine
)
from application.dtos import ReadingInput, ReadingBatch


class TestDomainModels:
    """Tests para modelos de dominio básicos."""
    
    def test_fuzzy_set_creation(self):
        """Test creación básica de conjunto difuso."""
        fuzzy_set = FuzzySet(
            name="bajo",
            membership_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0, 10, 20],
            description="Valor bajo"
        )
        
        assert fuzzy_set.name == "bajo"
        assert fuzzy_set.membership_type == MembershipFunctionType.TRIANGULAR
        assert fuzzy_set.parameters == [0, 10, 20]
        assert fuzzy_set.description == "Valor bajo"
    
    def test_fuzzy_set_parameter_validation(self):
        """Test validación de parámetros de conjuntos difusos."""
        # Triangular válido
        triangular = FuzzySet(
            name="test",
            membership_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0, 5, 10]
        )
        assert triangular.validate_parameters() == True
        
        # Triangular inválido (pocos parámetros)
        triangular_invalid = FuzzySet(
            name="test",
            membership_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0, 5]  # Faltan parámetros
        )
        assert triangular_invalid.validate_parameters() == False
        
        # Trapezoidal válido
        trapezoidal = FuzzySet(
            name="test",
            membership_type=MembershipFunctionType.TRAPEZOIDAL,
            parameters=[0, 2, 8, 10]
        )
        assert trapezoidal.validate_parameters() == True
        
        # Gaussiano válido
        gaussian = FuzzySet(
            name="test",
            membership_type=MembershipFunctionType.GAUSSIAN,
            parameters=[5, 1.5]  # media, desviación
        )
        assert gaussian.validate_parameters() == True
    
    def test_variable_creation(self):
        """Test creación básica de variable."""
        variable = Variable(
            id="temp",
            name="Temperatura",
            unit="°C",
            min_value=0.0,
            max_value=50.0,
            fuzzy_sets=[
                FuzzySet(
                    name="baja",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[0, 0, 25]
                ),
                FuzzySet(
                    name="alta",
                    membership_type=MembershipFunctionType.TRIANGULAR,
                    parameters=[25, 50, 50]
                )
            ]
        )
        
        assert variable.id == "temp"
        assert variable.name == "Temperatura"
        assert variable.unit == "°C"
        assert variable.min_value == 0.0
        assert variable.max_value == 50.0
        assert len(variable.fuzzy_sets) == 2
    
    def test_actuator_state_creation(self):
        """Test creación básica de estado de actuador."""
        now = datetime.now(timezone.utc)
        state = ActuatorState(
            actuatorId="pump_001",
            last_target=75,
            last_emitted_at=now,
            last_hold_seconds=300
        )
        
        assert state.actuatorId == "pump_001"
        assert state.last_target == 75
        assert state.last_emitted_at == now
        assert state.last_hold_seconds == 300
    
    def test_actuator_state_hysteresis_logic(self):
        """Test lógica de histeresis en estado de actuador."""
        state = ActuatorState(
            actuatorId="pump_001",
            last_target=50,
            last_emitted_at=datetime.now(timezone.utc),
            last_hold_seconds=300
        )
        
        # Cambio pequeño - debe saltarse por histeresis
        assert state.should_skip_hysteresis(52, 5.0) == True
        
        # Cambio grande - no debe saltarse
        assert state.should_skip_hysteresis(60, 5.0) == False
        
        # Cambio exacto en el límite
        assert state.should_skip_hysteresis(55, 5.0) == False
    
    def test_actuator_state_cooldown_logic(self):
        """Test lógica de cooldown en estado de actuador."""
        from datetime import timedelta
        
        # Estado reciente (hace 10 segundos)
        recent_time = datetime.now(timezone.utc)
        state = ActuatorState(
            actuatorId="pump_001",
            last_target=50,
            last_emitted_at=recent_time,
            last_hold_seconds=300
        )
        
        # Cooldown de 30 segundos - debe saltarse
        assert state.should_skip_cooldown(30) == True
        
        # Simular tiempo pasado
        state.last_emitted_at = datetime.now(timezone.utc) - timedelta(seconds=35)
        assert state.should_skip_cooldown(30) == False
    
    def test_control_parameters_creation(self):
        """Test creación de parámetros de control."""
        params = ControlParameters(
            hysteresis_delta=5.0,
            cooldown_seconds=30,
            defuzzification_method="centroid",
            evaluation_interval_seconds=10,
            min_confidence_threshold=0.1
        )
        
        assert params.hysteresis_delta == 5.0
        assert params.cooldown_seconds == 30
        assert params.defuzzification_method == "centroid"
        assert params.evaluation_interval_seconds == 10
        assert params.min_confidence_threshold == 0.1


class TestApplicationDTOs:
    """Tests para DTOs de aplicación básicos."""
    
    def test_reading_input_creation(self):
        """Test creación básica de entrada de lectura."""
        reading = ReadingInput(
            physicalId="temp_001",
            variableId="temperature",
            value=25.5
        )
        
        assert reading.physicalId == "temp_001"
        assert reading.variableId == "temperature"
        assert reading.value == 25.5
    
    def test_reading_batch_creation(self):
        """Test creación básica de lote de lecturas."""
        now = datetime.now(timezone.utc)
        batch = ReadingBatch(
            esp32Id="esp32_001",
            timestamp=now,
            readings=[
                ReadingInput(
                    physicalId="temp_001",
                    variableId="temperature",
                    value=25.5
                ),
                ReadingInput(
                    physicalId="hum_001",
                    variableId="humidity",
                    value=60.0
                )
            ]
        )
        
        assert batch.esp32Id == "esp32_001"
        assert batch.timestamp == now
        assert len(batch.readings) == 2
        assert batch.readings[0].variableId == "temperature"
        assert batch.readings[1].variableId == "humidity"


class TestFuzzyLogicModels:
    """Tests para modelos de lógica difusa."""
    
    def test_fuzzy_condition_creation(self):
        """Test creación de condición difusa."""
        condition = FuzzyCondition(
            variable_id="temperature",
            fuzzy_set_name="high",
            weight=0.8
        )
        
        assert condition.variable_id == "temperature"
        assert condition.fuzzy_set_name == "high"
        assert condition.weight == 0.8
    
    def test_fuzzy_rule_creation(self):
        """Test creación de regla difusa."""
        from domain.models import FuzzyRule, FuzzyCondition, LogicalOperator
        
        rule = FuzzyRule(
            id="rule_001",
            name="Temperatura alta -> Ventilador alto",
            description="Regla de prueba",
            conditions=[
                FuzzyCondition(variable_id="temp", fuzzy_set_name="high")
            ],
            operator=LogicalOperator.AND,
            output_variable_id="fan",
            output_fuzzy_set_name="high",
            priority=5
        )
        
        assert rule.id == "rule_001"
        assert rule.name == "Temperatura alta -> Ventilador alto"
        assert len(rule.conditions) == 1
        assert rule.operator == LogicalOperator.AND
        assert rule.priority == 5
        assert rule.active == True
    
    def test_fuzzy_rule_conditions_text(self):
        """Test generación de texto de condiciones."""
        from domain.models import FuzzyRule, FuzzyCondition, LogicalOperator
        
        rule = FuzzyRule(
            id="rule_001",
            name="Test Rule",
            conditions=[
                FuzzyCondition(variable_id="temp", fuzzy_set_name="high", weight=0.8),
                FuzzyCondition(variable_id="humidity", fuzzy_set_name="low")
            ],
            operator=LogicalOperator.OR,
            output_variable_id="fan",
            output_fuzzy_set_name="high"
        )
        
        text = rule.evaluate_conditions_text()
        assert "temp es high (peso: 0.8)" in text
        assert "humidity es low" in text
        assert " O " in text
    
    def test_threshold_rule_creation(self):
        """Test creación de regla de umbral."""
        from domain.models import ThresholdRule
        
        rule = ThresholdRule(
            variableId="temperature",
            greater_than=25.0,
            output_name="fan_power",
            target=80,
            hold_seconds=300
        )
        
        assert rule.variableId == "temperature"
        assert rule.greater_than == 25.0
        assert rule.output_name == "fan_power"
        assert rule.target == 80
        assert rule.hold_seconds == 300


class TestActuatorModels:
    """Tests para modelos de actuadores."""
    
    def test_actuator_mapping_creation(self):
        """Test creación de mapeo de actuador."""
        from domain.models import ActuatorMapping, ActuatorType
        
        mapping = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="esp32_greenhouse_01",
            actuator_type=ActuatorType.VARIABLE,
            on_threshold=30.0
        )
        
        assert mapping.output_name == "fan_power"
        assert mapping.actuatorId == "fan_001"
        assert mapping.esp32Id == "esp32_greenhouse_01"
        assert mapping.actuator_type == ActuatorType.VARIABLE
        assert mapping.on_threshold == 30.0
    
    def test_output_plan_creation(self):
        """Test creación de plan de salida."""
        from domain.models import OutputPlan, ActuatorMapping, ActuatorType
        
        actuator = ActuatorMapping(
            output_name="pump_water",
            actuatorId="pump_001",
            esp32Id="esp32_01",
            actuator_type=ActuatorType.ON_OFF
        )
        
        plan = OutputPlan(
            actuator=actuator,
            target=75,
            hold_seconds=300,
            fuzzy_rule="IF temp IS high THEN pump IS on"
        )
        
        assert plan.actuator.output_name == "pump_water"
        assert plan.target == 75
        assert plan.hold_seconds == 300
        assert plan.fuzzy_rule == "IF temp IS high THEN pump IS on"


class TestRoutineModel:
    """Tests para modelo de rutina."""
    
    def test_routine_creation(self):
        """Test creación básica de rutina."""
        from domain.models import Routine, ControlParameters
        
        routine = Routine(
            id="routine_001",
            name="Control de Temperatura",
            description="Rutina para controlar temperatura del invernadero",
            active=True,
            input_variables=["temperature", "humidity"],
            output_variables=["fan_power", "pump_water"]
        )
        
        assert routine.id == "routine_001"
        assert routine.name == "Control de Temperatura"
        assert routine.active == True
        assert len(routine.input_variables) == 2
        assert len(routine.output_variables) == 2
        assert isinstance(routine.control_parameters, ControlParameters)
    
    def test_routine_add_fuzzy_rule(self):
        """Test agregar regla difusa a rutina."""
        from domain.models import Routine, FuzzyRule, FuzzyCondition, LogicalOperator
        
        routine = Routine(id="test", name="Test Routine")
        
        rule = FuzzyRule(
            id="rule_001",
            name="Test Rule",
            conditions=[FuzzyCondition(variable_id="temp", fuzzy_set_name="high")],
            operator=LogicalOperator.AND,
            output_variable_id="fan",
            output_fuzzy_set_name="high"
        )
        
        routine.add_fuzzy_rule(rule)
        assert len(routine.fuzzy_rules) == 1
        assert routine.fuzzy_rules[0].id == "rule_001"
    
    def test_routine_remove_fuzzy_rule(self):
        """Test eliminar regla difusa de rutina."""
        from domain.models import Routine, FuzzyRule, FuzzyCondition, LogicalOperator
        
        routine = Routine(id="test", name="Test Routine")
        
        rule = FuzzyRule(
            id="rule_001",
            name="Test Rule",
            conditions=[FuzzyCondition(variable_id="temp", fuzzy_set_name="high")],
            operator=LogicalOperator.AND,
            output_variable_id="fan",
            output_fuzzy_set_name="high"
        )
        
        routine.add_fuzzy_rule(rule)
        assert len(routine.fuzzy_rules) == 1
        
        removed = routine.remove_fuzzy_rule("rule_001")
        assert removed == True
        assert len(routine.fuzzy_rules) == 0
        
        # Intentar eliminar regla inexistente
        removed = routine.remove_fuzzy_rule("nonexistent")
        assert removed == False
    
    def test_routine_get_actuator_for_output(self):
        """Test buscar actuador por nombre de salida."""
        from domain.models import Routine, ActuatorMapping, ActuatorType
        
        routine = Routine(id="test", name="Test Routine")
        
        mapping = ActuatorMapping(
            output_name="fan_power",
            actuatorId="fan_001",
            esp32Id="esp32_01",
            actuator_type=ActuatorType.VARIABLE
        )
        
        routine.outputs.append(mapping)
        
        found = routine.get_actuator_for_output("fan_power")
        assert found is not None
        assert found.actuatorId == "fan_001"
        
        not_found = routine.get_actuator_for_output("nonexistent")
        assert not_found is None


class TestBasicValidations:
    """Tests para validaciones básicas."""
    
    def test_membership_function_types_enum(self):
        """Test que los tipos de función de membresía están definidos correctamente."""
        assert MembershipFunctionType.TRIANGULAR == "triangular"
        assert MembershipFunctionType.TRAPEZOIDAL == "trapezoidal"
        assert MembershipFunctionType.GAUSSIAN == "gaussian"
        assert MembershipFunctionType.SIGMOID == "sigmoid"
    
    def test_actuator_types_enum(self):
        """Test que los tipos de actuadores están definidos correctamente."""
        from domain.models import ActuatorType
        assert ActuatorType.ON_OFF == "on_off"
        assert ActuatorType.VARIABLE == "variable"
    
    def test_logical_operators_enum(self):
        """Test que los operadores lógicos están definidos correctamente."""
        from domain.models import LogicalOperator
        assert LogicalOperator.AND == "and"
        assert LogicalOperator.OR == "or"
    
    def test_variable_range_validation(self):
        """Test validación básica de rangos de variables."""
        variable = Variable(
            id="test",
            name="Test Variable",
            unit="unit",
            min_value=0.0,
            max_value=100.0,
            fuzzy_sets=[]
        )
        
        # Verificar que min < max
        assert variable.min_value < variable.max_value
        
        # Verificar que los valores están en el rango esperado
        assert variable.min_value >= 0.0
        assert variable.max_value <= 1000.0  # Rango razonable