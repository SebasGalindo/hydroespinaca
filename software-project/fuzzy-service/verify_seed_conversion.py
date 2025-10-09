#!/usr/bin/env python3
"""
Verify that seed data conversion from routines to consequents works correctly.
"""
import asyncio
from unittest.mock import Mock, AsyncMock

from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId, DomainId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType


async def test_routine_to_consequents():
    """Test conversion of routine to consequents."""
    # Import the conversion function
    import sys
    sys.path.insert(0, '.')
    from FuzzyService.Infrastructure.Configuration.SeedData import _convert_routine_to_consequents

    # Create mock terms
    power_var_id = FuzzyVariableId("68e05364d86d6edc39982875")  # Power variable
    duration_var_id = FuzzyVariableId("68e05364d86d6edc39982876")  # Duration variable

    term_power_low = FuzzyTerm(
        id=FuzzyTermId("68e05364d86d6edc39982890"),
        variable_id=power_var_id,
        label="potenciaBaja",
        membership_function=MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0.0, 20.0, 40.0],
            universe_min=0.0,
            universe_max=100.0
        )
    )

    term_power_high = FuzzyTerm(
        id=FuzzyTermId("68e05364d86d6edc39982891"),
        variable_id=power_var_id,
        label="potenciaAlta",
        membership_function=MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[60.0, 80.0, 100.0],
            universe_min=0.0,
            universe_max=100.0
        )
    )

    term_duration_short = FuzzyTerm(
        id=FuzzyTermId("68e05364d86d6edc39982892"),
        variable_id=duration_var_id,
        label="corta",
        membership_function=MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[0.0, 5.0, 10.0],
            universe_min=0.0,
            universe_max=60.0
        )
    )

    # Create routine with steps
    routine = FuzzyRoutine(
        routine_name="Test Routine",
        steps=[
            RoutineStep(
                step_id=1,
                condition="temp > 25",
                power_term_id=DomainId(str(term_power_high.id)),
                duration_term_id=DomainId(str(term_duration_short.id))
            ),
            RoutineStep(
                step_id=2,
                condition="temp <= 25",
                power_term_id=DomainId(str(term_power_low.id)),
                duration_term_id=DomainId(str(term_duration_short.id))
            )
        ]
    )

    # Mock repository
    repo_term = AsyncMock()

    async def mock_get_by_id(term_id):
        term_map = {
            str(term_power_low.id): term_power_low,
            str(term_power_high.id): term_power_high,
            str(term_duration_short.id): term_duration_short
        }
        return term_map.get(str(term_id))

    repo_term.get_by_id = mock_get_by_id

    # Test conversion
    consequents = await _convert_routine_to_consequents(routine, repo_term)

    # Verify results
    print(f"✅ Conversion successful!")
    print(f"   Generated {len(consequents)} consequents from routine")

    for i, cons in enumerate(consequents, 1):
        print(f"\n   Consequent {i}:")
        print(f"   - Variable ID: {cons.variable_id}")
        print(f"   - Terms: {[str(t) for t in cons.terms]}")
        print(f"   - Aggregation: {cons.aggregation_method}")

    # Validate
    assert len(consequents) == 2, f"Expected 2 consequents (power + duration), got {len(consequents)}"

    # Find power and duration consequents
    power_cons = next((c for c in consequents if str(c.variable_id) == str(power_var_id)), None)
    duration_cons = next((c for c in consequents if str(c.variable_id) == str(duration_var_id)), None)

    assert power_cons is not None, "Power consequent not found"
    assert duration_cons is not None, "Duration consequent not found"

    assert len(power_cons.terms) == 2, f"Expected 2 power terms, got {len(power_cons.terms)}"
    assert len(duration_cons.terms) == 1, f"Expected 1 duration term, got {len(duration_cons.terms)}"

    print(f"\n✅ All validations passed!")
    print(f"   - 2 consequents created (power, duration)")
    print(f"   - Power consequent has 2 terms (low, high)")
    print(f"   - Duration consequent has 1 term (short)")

    return True


if __name__ == "__main__":
    try:
        result = asyncio.run(test_routine_to_consequents())
        exit(0 if result else 1)
    except Exception as e:
        print(f"❌ Test failed: {e}")
        import traceback
        traceback.print_exc()
        exit(1)
