import pytest
from FuzzyService.Application.Helpers.ValidationHelper import (
    require_str,
    optional_str,
    ensure_in_set,
    require_id,
    ensure_ids_list,
    validate_pagination,
)
from FuzzyService.Domain.Errors.DomainErrors import ValidationError


class TestRequireStr:
    """Tests for require_str function"""

    def test_require_str_with_valid_string_should_return_trimmed_value(self):
        # Arrange
        value = "  test value  "

        # Act
        result = require_str(value, field="test_field")

        # Assert
        assert result == "test value"

    def test_require_str_with_none_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor no puede estar vacío"):
            require_str(None, field="test_field")

    def test_require_str_with_empty_string_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor no cumple con la longitud mínima requerida"):
            require_str("", field="test_field")

    def test_require_str_with_whitespace_only_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor no cumple con la longitud mínima requerida"):
            require_str("   ", field="test_field")

    def test_require_str_with_min_length_met_should_succeed(self):
        # Arrange
        value = "abc"

        # Act
        result = require_str(value, field="test_field", min_len=3)

        # Assert
        assert result == "abc"

    def test_require_str_with_min_length_not_met_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor no cumple con la longitud mínima requerida"):
            require_str("ab", field="test_field", min_len=3)

    def test_require_str_with_max_length_met_should_succeed(self):
        # Arrange
        value = "abc"

        # Act
        result = require_str(value, field="test_field", max_len=5)

        # Assert
        assert result == "abc"

    def test_require_str_with_max_length_exceeded_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor excede la longitud máxima permitida"):
            require_str("abcdef", field="test_field", max_len=5)

    def test_require_str_without_max_length_constraint_should_allow_any_length(self):
        # Arrange
        value = "a" * 1000

        # Act
        result = require_str(value, field="test_field")

        # Assert
        assert len(result) == 1000


class TestOptionalStr:
    """Tests for optional_str function"""

    def test_optional_str_with_valid_string_should_return_trimmed_value(self):
        # Arrange
        value = "  test value  "

        # Act
        result = optional_str(value, field="test_field")

        # Assert
        assert result == "test value"

    def test_optional_str_with_none_should_return_none(self):
        # Arrange, Act
        result = optional_str(None, field="test_field")

        # Assert
        assert result is None

    def test_optional_str_with_empty_string_should_return_empty_string(self):
        # Arrange
        value = ""

        # Act
        result = optional_str(value, field="test_field")

        # Assert
        assert result == ""

    def test_optional_str_with_max_length_met_should_succeed(self):
        # Arrange
        value = "abc"

        # Act
        result = optional_str(value, field="test_field", max_len=5)

        # Assert
        assert result == "abc"

    def test_optional_str_with_max_length_exceeded_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: El valor excede la longitud máxima permitida"):
            optional_str("abcdef", field="test_field", max_len=5)

    def test_optional_str_without_max_length_constraint_should_allow_any_length(self):
        # Arrange
        value = "a" * 1000

        # Act
        result = optional_str(value, field="test_field")

        # Assert
        assert len(result) == 1000


class TestEnsureInSet:
    """Tests for ensure_in_set function"""

    def test_ensure_in_set_with_value_in_allowed_set_should_return_value(self):
        # Arrange
        value = "option2"
        allowed = ["option1", "option2", "option3"]

        # Act
        result = ensure_in_set(value, field="test_field", allowed=allowed)

        # Assert
        assert result == "option2"

    def test_ensure_in_set_with_value_not_in_allowed_set_should_raise_validation_error(self):
        # Arrange
        value = "invalid_option"
        allowed = ["option1", "option2", "option3"]

        # Act & Assert
        with pytest.raises(ValidationError, match="test_field: Valor inválido, debe ser uno de:"):
            ensure_in_set(value, field="test_field", allowed=allowed)

    def test_ensure_in_set_should_be_case_sensitive(self):
        # Arrange
        value = "Option1"
        allowed = ["option1", "option2"]

        # Act & Assert
        with pytest.raises(ValidationError):
            ensure_in_set(value, field="test_field", allowed=allowed)


class TestRequireId:
    """Tests for require_id function"""

    def test_require_id_with_valid_id_should_return_trimmed_value(self):
        # Arrange
        value = "  test-id-123  "

        # Act
        result = require_id(value, field="test_field")

        # Assert
        assert result == "test-id-123"

    def test_require_id_with_none_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: Identificador inválido"):
            require_id(None, field="test_field")

    def test_require_id_with_empty_string_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: Identificador inválido"):
            require_id("", field="test_field")

    def test_require_id_with_whitespace_only_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="test_field: Identificador inválido"):
            require_id("   ", field="test_field")


class TestEnsureIdsList:
    """Tests for ensure_ids_list function"""

    def test_ensure_ids_list_with_valid_ids_should_return_list(self):
        # Arrange
        values = ["id1", "  id2  ", "id3"]

        # Act
        result = ensure_ids_list(values, field="test_field")

        # Assert
        assert result == ["id1", "id2", "id3"]

    def test_ensure_ids_list_with_none_should_return_empty_list(self):
        # Arrange, Act
        result = ensure_ids_list(None, field="test_field")

        # Assert
        assert result == []

    def test_ensure_ids_list_with_empty_list_should_return_empty_list(self):
        # Arrange
        values = []

        # Act
        result = ensure_ids_list(values, field="test_field")

        # Assert
        assert result == []

    def test_ensure_ids_list_with_none_in_list_should_raise_validation_error(self):
        # Arrange
        values = ["id1", None, "id3"]

        # Act & Assert
        with pytest.raises(ValidationError, match="test_field\\[1\\]: Identificador inválido"):
            ensure_ids_list(values, field="test_field")

    def test_ensure_ids_list_with_empty_string_in_list_should_raise_validation_error(self):
        # Arrange
        values = ["id1", "", "id3"]

        # Act & Assert
        with pytest.raises(ValidationError, match="test_field\\[1\\]: Identificador inválido"):
            ensure_ids_list(values, field="test_field")

    def test_ensure_ids_list_with_whitespace_in_list_should_raise_validation_error(self):
        # Arrange
        values = ["id1", "   ", "id3"]

        # Act & Assert
        with pytest.raises(ValidationError, match="test_field\\[1\\]: Identificador inválido"):
            ensure_ids_list(values, field="test_field")


class TestValidatePagination:
    """Tests for validate_pagination function"""

    def test_validate_pagination_with_valid_params_should_return_tuple(self):
        # Arrange
        skip = 10
        limit = 20

        # Act
        result_skip, result_limit = validate_pagination(skip, limit)

        # Assert
        assert result_skip == 10
        assert result_limit == 20

    def test_validate_pagination_with_none_params_should_return_defaults(self):
        # Arrange, Act
        result_skip, result_limit = validate_pagination(None, None)

        # Assert
        assert result_skip == 0
        assert result_limit == 50

    def test_validate_pagination_with_skip_zero_should_succeed(self):
        # Arrange, Act
        result_skip, result_limit = validate_pagination(0, 10)

        # Assert
        assert result_skip == 0
        assert result_limit == 10

    def test_validate_pagination_with_negative_skip_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="Parámetros de paginación inválidos"):
            validate_pagination(-1, 10)

    def test_validate_pagination_with_zero_limit_should_use_default(self):
        # Arrange, Act
        # Note: When limit is 0 or None, it defaults to 50
        result_skip, result_limit = validate_pagination(0, 0)

        # Assert
        assert result_skip == 0
        assert result_limit == 50

    def test_validate_pagination_with_negative_limit_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="Parámetros de paginación inválidos"):
            validate_pagination(0, -10)

    def test_validate_pagination_with_limit_exceeding_max_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="Parámetros de paginación inválidos"):
            validate_pagination(0, 201, max_limit=200)

    def test_validate_pagination_with_limit_equal_to_max_should_succeed(self):
        # Arrange, Act
        result_skip, result_limit = validate_pagination(0, 200, max_limit=200)

        # Assert
        assert result_skip == 0
        assert result_limit == 200

    def test_validate_pagination_with_custom_max_limit_should_respect_it(self):
        # Arrange, Act
        result_skip, result_limit = validate_pagination(5, 50, max_limit=100)

        # Assert
        assert result_skip == 5
        assert result_limit == 50

    def test_validate_pagination_with_non_numeric_skip_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="Parámetros de paginación inválidos"):
            validate_pagination("invalid", 10)

    def test_validate_pagination_with_non_numeric_limit_should_raise_validation_error(self):
        # Arrange, Act & Assert
        with pytest.raises(ValidationError, match="Parámetros de paginación inválidos"):
            validate_pagination(10, "invalid")
