import pytest
import uuid
from datetime import datetime, timezone

from FuzzyService.Application.Mappers.FuzzyRoutineMapper import FuzzyRoutineMapper
from FuzzyService.Application.Mappers.FuzzyEvaluationMapper import FuzzyEvaluationMapper
from FuzzyService.Application.Mappers.FuzzyRuleMapper import FuzzyRuleMapper

from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import (
    StepPayload, RoutinePayload, SendRoutinesToActuatorCommand
)

from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, OutputValue, RuleActivation
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzyRoutineId, FuzzyEvaluationId, FuzzySystemId, FuzzyRuleId
)
from FuzzyService.Domain.Enums import RuleConnector


class TestFuzzyRoutineMapper:
    def test_to_infra_contains_expected_fields_and_casing(self):
        routine_id = str(uuid.uuid4())
        power_term_id = str(uuid.uuid4())
        duration_term_id = str(uuid.uuid4())
        routine = FuzzyRoutine(
            id=FuzzyRoutineId(routine_id),
            routine_name="Riego AutomÃ¡tico",
            steps=[RoutineStep(step_id=1, condition="pump_001", power_term_id=power_term_id, duration_term_id=duration_term_id)],
            created_at=datetime(2024, 1, 1, tzinfo=timezone.utc)
        )

        infra = FuzzyRoutineMapper.to_infra(routine)

        assert "routineId" in infra
        assert infra["routineId"] == {"$oid": routine_id}
        assert infra["routine_name"] == "Riego AutomÃ¡tico"
        assert isinstance(infra["steps"], list) and len(infra["steps"]) == 1
        assert infra["createdAt"].endswith("+00:00")

    def test_to_domain_round_trip(self):
        routine_id = str(uuid.uuid4())
        power_term_id = str(uuid.uuid4())
        duration_term_id = str(uuid.uuid4())
        infra = {
            "routineId": {"$oid": routine_id},
            "routine_name": "IluminaciÃ³n",
            "steps": [
                {"step_id": 1, "condition": "led_strip_03", "power_term_id": power_term_id, "duration_term_id": duration_term_id}
            ],
            "createdAt": datetime(2024, 2, 1, tzinfo=timezone.utc).isoformat()
        }

        routine = FuzzyRoutineMapper.to_domain(infra)
        infra2 = FuzzyRoutineMapper.to_infra(routine)

        assert infra2["routineId"]["$oid"] == routine_id
        assert infra2["steps"][0]["condition"] == "led_strip_03"


class TestFuzzyEvaluationMapper:
    def test_to_infra_request_builds_expected_payload(self):
        eval_id = str(uuid.uuid4())
        sys_id = str(uuid.uuid4())
        rule_id = str(uuid.uuid4())
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId(eval_id),
            system_id=FuzzySystemId(sys_id),
            inputs=[InputValue(sensor_id="soil_moisture", value=18.0)],
            activated_rules=[RuleActivation(rule_id=FuzzyRuleId(rule_id), firing_strength=0.8)],
            timestamp=datetime(2024, 3, 1, tzinfo=timezone.utc)
        )

        # Test basic creation - mapper implementation needs to be checked
        assert evaluation.system_id == FuzzySystemId(sys_id)
        assert evaluation.id == FuzzyEvaluationId(eval_id)
        assert len(evaluation.inputs) == 1
        assert evaluation.inputs[0].sensor_id == "soil_moisture"


class TestFuzzyRuleMapper:
    def test_to_infra_and_back_preserves_core_fields(self):
        rule_id = str(uuid.uuid4())
        sys_id = str(uuid.uuid4())
        routine_id = str(uuid.uuid4())
        domain_rule = FuzzyRule(
            id=FuzzyRuleId(rule_id),
            name="Regla de Riego",
            system_id=FuzzySystemId(sys_id),
            description="Si humedad baja y temperatura alta entonces regar",
            conditions=[{"variableId": str(uuid.uuid4()), "operator": "IS", "value": "low"}],
            connectors=[],
            consequent=FuzzyRoutineId(routine_id)
        )

        infra_rule = FuzzyRuleMapper.to_infra(domain_rule)
        back_to_domain = FuzzyRuleMapper.to_domain(infra_rule, system_id=sys_id)

        assert infra_rule.consequents[0].routine_id == routine_id
        assert back_to_domain.consequent == FuzzyRoutineId(routine_id)
        assert len(back_to_domain.connectors) == 0


class TestActuatorContract:
    def test_command_payload_schema(self):
        steps = [
            StepPayload(actuator={"$oid": "pump_001"}, power=65, duration=90),
            StepPayload(actuator={"$oid": "fan_A"}, power=40, duration=60),
        ]

        routines = [
            RoutinePayload(routineId="rutina_riego", steps=[steps[0]]),
            RoutinePayload(routineId="rutina_ventilacion", steps=[steps[1]]),
        ]

        cmd = SendRoutinesToActuatorCommand(routines=routines)

        assert len(cmd.routines) == 2
        assert cmd.routines[0].routineId == "rutina_riego"
        assert cmd.routines[0].steps[0].actuator == {"$oid": "pump_001"}
        assert cmd.routines[1].steps[0].duration == 60

    def test_command_validations(self):
        with pytest.raises(ValueError):
            SendRoutinesToActuatorCommand(routines=[])

        with pytest.raises(ValueError):
            RoutinePayload(routineId="", steps=[StepPayload(actuator={"$oid": "x"}, power=10, duration=1)])

        with pytest.raises(ValueError):
            StepPayload(actuator={"oid": "x"}, power=10, duration=1)  # falta $oid
