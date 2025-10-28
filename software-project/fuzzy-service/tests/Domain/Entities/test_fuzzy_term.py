import pytest
from datetime import datetime, timezone, timedelta
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType


class TestFuzzyTerm:
    """Tests for FuzzyTerm entity"""

    def test_create_fuzzy_term_with_minimal_data_should_initialize_defaults(self):
        # Arrange & Act
        term = FuzzyTerm(label="high")

        # Assert
        assert term.label == "high"
        assert term.membership_function is not None
        assert term.created_at is not None
        assert term.updated_at is not None

    def test_create_fuzzy_term_with_complete_data_should_succeed(self):
        # Arrange
        term_id = FuzzyTermId.generate()
        variable_id = FuzzyVariableId.generate()
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[20.0, 25.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        term = FuzzyTerm(
            id=term_id,
            variable_id=variable_id,
            label="high",
            membership_function=mf
        )

        # Assert
        assert term.id == term_id
        assert term.variable_id == variable_id
        assert term.label == "high"
        assert term.membership_function.function_type == MembershipFunctionType.TRIANGULAR

    def test_create_fuzzy_term_with_empty_label_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede estar vacía"):
            FuzzyTerm(label="")

    def test_create_fuzzy_term_with_whitespace_label_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede estar vacía"):
            FuzzyTerm(label="   ")

    def test_create_fuzzy_term_with_label_too_long_should_fail(self):
        # Arrange
        long_label = "a" * 31

        # Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede exceder 30 caracteres"):
            FuzzyTerm(label=long_label)

    def test_create_fuzzy_term_should_trim_label_whitespace(self):
        # Arrange & Act
        term = FuzzyTerm(label="  medium  ")

        # Assert
        assert term.label == "medium"

    def test_created_at_should_be_set_automatically(self):
        # Arrange
        before = datetime.now(timezone.utc)

        # Act
        term = FuzzyTerm(label="low")

        # Assert
        after = datetime.now(timezone.utc)
        assert before <= term.created_at <= after

    def test_updated_at_should_equal_created_at_initially(self):
        # Arrange & Act
        term = FuzzyTerm(label="medium")

        # Assert
        assert term.created_at == term.updated_at

    def test_update_label_with_valid_label_should_succeed(self):
        # Arrange
        term = FuzzyTerm(label="low")
        original_updated_at = term.updated_at

        # Act
        term.update_label("high")

        # Assert
        assert term.label == "high"
        assert term.updated_at > original_updated_at

    def test_update_label_with_empty_label_should_fail(self):
        # Arrange
        term = FuzzyTerm(label="medium")

        # Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede estar vacía"):
            term.update_label("")

    def test_update_label_with_whitespace_label_should_fail(self):
        # Arrange
        term = FuzzyTerm(label="medium")

        # Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede estar vacía"):
            term.update_label("   ")

    def test_update_label_with_label_too_long_should_fail(self):
        # Arrange
        term = FuzzyTerm(label="medium")
        long_label = "a" * 31

        # Act & Assert
        with pytest.raises(ValueError, match="La etiqueta lingüística no puede exceder 30 caracteres"):
            term.update_label(long_label)

    def test_update_label_should_trim_whitespace(self):
        # Arrange
        term = FuzzyTerm(label="low")

        # Act
        term.update_label("  high  ")

        # Assert
        assert term.label == "high"

    def test_update_membership_function_with_valid_function_should_succeed(self):
        # Arrange
        term = FuzzyTerm(label="medium")
        original_updated_at = term.updated_at
        new_mf = MembershipFunction(
            function_type=MembershipFunctionType.GAUSSIAN,
            parameters=[25.0, 5.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        term.update_membership_function(new_mf)

        # Assert
        assert term.membership_function.function_type == MembershipFunctionType.GAUSSIAN
        assert term.updated_at > original_updated_at

    def test_update_membership_function_with_invalid_type_should_fail(self):
        # Arrange
        term = FuzzyTerm(label="medium")

        # Act & Assert
        with pytest.raises(ValueError, match="La función de membresía debe ser una instancia de MembershipFunction"):
            term.update_membership_function("not a membership function")

    def test_to_dict_should_return_correct_structure(self):
        # Arrange
        term_id = FuzzyTermId.generate()
        variable_id = FuzzyVariableId.generate()
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )
        term = FuzzyTerm(
            id=term_id,
            variable_id=variable_id,
            label="high",
            membership_function=mf
        )

        # Act
        result = term.to_dict()

        # Assert
        assert result["_id"] == str(term_id)
        assert result["variableId"] == str(variable_id)
        assert result["label"] == "high"
        assert result["mf"]["type"] == "triangular"
        assert result["mf"]["params"] == [10.0, 20.0, 30.0]
        assert "createdAt" in result
        assert "updatedAt" in result

    def test_to_dict_without_ids_should_return_none_for_ids(self):
        # Arrange
        term = FuzzyTerm(label="medium")

        # Act
        result = term.to_dict()

        # Assert
        assert result["_id"] is None
        assert result["variableId"] is None
        assert result["label"] == "medium"

    def test_str_representation_should_include_label_and_type(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRAPEZOIDAL,
            parameters=[10.0, 20.0, 30.0, 40.0],
            universe_min=0.0,
            universe_max=50.0
        )
        term = FuzzyTerm(label="very_high", membership_function=mf)

        # Act
        result = str(term)

        # Assert
        assert "very_high" in result
        assert "trapezoidal" in result

    def test_repr_should_include_all_key_information(self):
        # Arrange
        term_id = FuzzyTermId.generate()
        variable_id = FuzzyVariableId.generate()
        mf = MembershipFunction(
            function_type=MembershipFunctionType.GAUSSIAN,
            parameters=[25.0, 5.0],
            universe_min=0.0,
            universe_max=50.0
        )
        term = FuzzyTerm(
            id=term_id,
            variable_id=variable_id,
            label="medium",
            membership_function=mf
        )

        # Act
        result = repr(term)

        # Assert
        assert "FuzzyTerm" in result
        assert "medium" in result
        assert "gaussian" in result

    def test_validate_membership_function_with_invalid_type_should_fail(self):
        # Arrange, Act & Assert
        # Pydantic v2 raises ValidationError (not ValueError) for invalid types
        from pydantic import ValidationError as PydanticValidationError
        with pytest.raises(PydanticValidationError):
            FuzzyTerm(
                label="test",
                membership_function="not a membership function"
            )

    def test_multiple_updates_should_increment_updated_at(self):
        # Arrange
        term = FuzzyTerm(label="low")
        original_updated_at = term.updated_at

        # Act
        term.update_label("medium")
        first_update = term.updated_at

        term.update_label("high")
        second_update = term.updated_at

        # Assert
        assert original_updated_at <= first_update <= second_update
