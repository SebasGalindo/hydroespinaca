import pytest
from FuzzyService.Domain.ValueObjects.FuzzyValue import FuzzyValue, FuzzySet


class TestFuzzyValue:
    """Tests for FuzzyValue value object"""

    def test_create_fuzzy_value_with_valid_data_should_succeed(self):
        # Arrange & Act
        fuzzy_value = FuzzyValue(
            crisp_value=25.5,
            membership_degree=0.8,
            linguistic_label="medium"
        )

        # Assert
        assert fuzzy_value.crisp_value == 25.5
        assert fuzzy_value.membership_degree == 0.8
        assert fuzzy_value.linguistic_label == "medium"

    def test_create_fuzzy_value_without_label_should_succeed(self):
        # Arrange & Act
        fuzzy_value = FuzzyValue(
            crisp_value=10.0,
            membership_degree=0.5
        )

        # Assert
        assert fuzzy_value.crisp_value == 10.0
        assert fuzzy_value.membership_degree == 0.5
        assert fuzzy_value.linguistic_label is None

    def test_create_fuzzy_value_with_membership_below_zero_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="El grado de membresía debe estar entre 0 y 1"):
            FuzzyValue(
                crisp_value=10.0,
                membership_degree=-0.1
            )

    def test_create_fuzzy_value_with_membership_above_one_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="El grado de membresía debe estar entre 0 y 1"):
            FuzzyValue(
                crisp_value=10.0,
                membership_degree=1.5
            )

    def test_is_fully_member_with_degree_one_should_return_true(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=1.0)

        # Act
        result = fuzzy_value.is_fully_member()

        # Assert
        assert result is True

    def test_is_fully_member_with_degree_less_than_one_should_return_false(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=0.99)

        # Act
        result = fuzzy_value.is_fully_member()

        # Assert
        assert result is False

    def test_is_not_member_with_degree_zero_should_return_true(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=0.0)

        # Act
        result = fuzzy_value.is_not_member()

        # Assert
        assert result is True

    def test_is_not_member_with_degree_greater_than_zero_should_return_false(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=0.01)

        # Act
        result = fuzzy_value.is_not_member()

        # Assert
        assert result is False

    def test_is_partial_member_with_degree_between_zero_and_one_should_return_true(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=0.5)

        # Act
        result = fuzzy_value.is_partial_member()

        # Assert
        assert result is True

    def test_is_partial_member_with_degree_zero_should_return_false(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=0.0)

        # Act
        result = fuzzy_value.is_partial_member()

        # Assert
        assert result is False

    def test_is_partial_member_with_degree_one_should_return_false(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=10.0, membership_degree=1.0)

        # Act
        result = fuzzy_value.is_partial_member()

        # Assert
        assert result is False

    def test_to_dict_should_return_correct_structure(self):
        # Arrange
        fuzzy_value = FuzzyValue(
            crisp_value=25.5,
            membership_degree=0.8,
            linguistic_label="high"
        )

        # Act
        result = fuzzy_value.to_dict()

        # Assert
        assert result["crisp_value"] == 25.5
        assert result["membership_degree"] == 0.8
        assert result["linguistic_label"] == "high"

    def test_from_dict_should_create_valid_fuzzy_value(self):
        # Arrange
        data = {
            "crisp_value": 30.0,
            "membership_degree": 0.9,
            "linguistic_label": "very_high"
        }

        # Act
        fuzzy_value = FuzzyValue.from_dict(data)

        # Assert
        assert fuzzy_value.crisp_value == 30.0
        assert fuzzy_value.membership_degree == 0.9
        assert fuzzy_value.linguistic_label == "very_high"

    def test_str_representation_with_label_should_include_label(self):
        # Arrange
        fuzzy_value = FuzzyValue(
            crisp_value=25.5,
            membership_degree=0.75,
            linguistic_label="medium"
        )

        # Act
        result = str(fuzzy_value)

        # Assert
        assert "25.5" in result
        assert "0.750" in result
        assert "medium" in result

    def test_str_representation_without_label_should_not_include_label(self):
        # Arrange
        fuzzy_value = FuzzyValue(crisp_value=25.5, membership_degree=0.75)

        # Act
        result = str(fuzzy_value)

        # Assert
        assert "25.5" in result
        assert "0.750" in result


class TestFuzzySet:
    """Tests for FuzzySet value object"""

    def test_create_fuzzy_set_with_valid_data_should_succeed(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.5),
            FuzzyValue(crisp_value=20.0, membership_degree=1.0),
            FuzzyValue(crisp_value=30.0, membership_degree=0.3),
        )

        # Act
        fuzzy_set = FuzzySet(name="temperature", values=values)

        # Assert
        assert fuzzy_set.name == "temperature"
        assert len(fuzzy_set.values) == 3

    def test_create_fuzzy_set_with_empty_name_should_fail(self):
        # Arrange
        values = (FuzzyValue(crisp_value=10.0, membership_degree=0.5),)

        # Act & Assert
        with pytest.raises(ValueError, match="El nombre del conjunto difuso no puede estar vacío"):
            FuzzySet(name="", values=values)

    def test_create_fuzzy_set_with_whitespace_name_should_fail(self):
        # Arrange
        values = (FuzzyValue(crisp_value=10.0, membership_degree=0.5),)

        # Act & Assert
        with pytest.raises(ValueError, match="El nombre del conjunto difuso no puede estar vacío"):
            FuzzySet(name="   ", values=values)

    def test_create_fuzzy_set_with_empty_values_should_fail(self):
        # Arrange & Act & Assert
        with pytest.raises(ValueError, match="Un conjunto difuso no puede estar vacío"):
            FuzzySet(name="test", values=tuple())

    def test_get_max_membership_should_return_value_with_highest_degree(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.3),
            FuzzyValue(crisp_value=20.0, membership_degree=0.9),
            FuzzyValue(crisp_value=30.0, membership_degree=0.5),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        result = fuzzy_set.get_max_membership()

        # Assert
        assert result.crisp_value == 20.0
        assert result.membership_degree == 0.9

    def test_get_min_membership_should_return_value_with_lowest_degree(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.8),
            FuzzyValue(crisp_value=20.0, membership_degree=0.2),
            FuzzyValue(crisp_value=30.0, membership_degree=0.5),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        result = fuzzy_set.get_min_membership()

        # Assert
        assert result.crisp_value == 20.0
        assert result.membership_degree == 0.2

    def test_get_support_should_return_only_values_with_positive_membership(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.0),
            FuzzyValue(crisp_value=20.0, membership_degree=0.5),
            FuzzyValue(crisp_value=30.0, membership_degree=0.8),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        support = fuzzy_set.get_support()

        # Assert
        assert len(support) == 2
        assert all(v.membership_degree > 0.0 for v in support)

    def test_get_core_should_return_only_values_with_membership_one(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.5),
            FuzzyValue(crisp_value=20.0, membership_degree=1.0),
            FuzzyValue(crisp_value=30.0, membership_degree=1.0),
            FuzzyValue(crisp_value=40.0, membership_degree=0.9),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        core = fuzzy_set.get_core()

        # Assert
        assert len(core) == 2
        assert all(v.membership_degree == 1.0 for v in core)

    def test_get_alpha_cut_should_return_values_above_threshold(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.3),
            FuzzyValue(crisp_value=20.0, membership_degree=0.6),
            FuzzyValue(crisp_value=30.0, membership_degree=0.9),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        alpha_cut = fuzzy_set.get_alpha_cut(0.5)

        # Assert
        assert len(alpha_cut) == 2
        assert all(v.membership_degree >= 0.5 for v in alpha_cut)

    def test_get_alpha_cut_with_invalid_alpha_below_zero_should_fail(self):
        # Arrange
        values = (FuzzyValue(crisp_value=10.0, membership_degree=0.5),)
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act & Assert
        with pytest.raises(ValueError, match="Alpha debe estar entre 0 y 1"):
            fuzzy_set.get_alpha_cut(-0.1)

    def test_get_alpha_cut_with_invalid_alpha_above_one_should_fail(self):
        # Arrange
        values = (FuzzyValue(crisp_value=10.0, membership_degree=0.5),)
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act & Assert
        with pytest.raises(ValueError, match="Alpha debe estar entre 0 y 1"):
            fuzzy_set.get_alpha_cut(1.5)

    def test_size_should_return_number_of_values(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.3),
            FuzzyValue(crisp_value=20.0, membership_degree=0.6),
            FuzzyValue(crisp_value=30.0, membership_degree=0.9),
        )
        fuzzy_set = FuzzySet(name="test", values=values)

        # Act
        size = fuzzy_set.size()

        # Assert
        assert size == 3

    def test_to_dict_should_return_correct_structure(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.5),
            FuzzyValue(crisp_value=20.0, membership_degree=1.0),
        )
        fuzzy_set = FuzzySet(name="test_set", values=values)

        # Act
        result = fuzzy_set.to_dict()

        # Assert
        assert result["name"] == "test_set"
        assert len(result["values"]) == 2
        assert result["values"][0]["crisp_value"] == 10.0
        assert result["values"][1]["crisp_value"] == 20.0

    def test_from_dict_should_create_valid_fuzzy_set(self):
        # Arrange
        data = {
            "name": "humidity",
            "values": [
                {"crisp_value": 40.0, "membership_degree": 0.7, "linguistic_label": "medium"},
                {"crisp_value": 80.0, "membership_degree": 1.0, "linguistic_label": "high"},
            ]
        }

        # Act
        fuzzy_set = FuzzySet.from_dict(data)

        # Assert
        assert fuzzy_set.name == "humidity"
        assert len(fuzzy_set.values) == 2
        assert fuzzy_set.values[0].crisp_value == 40.0
        assert fuzzy_set.values[1].membership_degree == 1.0

    def test_str_representation_should_include_name_and_size(self):
        # Arrange
        values = (
            FuzzyValue(crisp_value=10.0, membership_degree=0.5),
            FuzzyValue(crisp_value=20.0, membership_degree=1.0),
        )
        fuzzy_set = FuzzySet(name="test_set", values=values)

        # Act
        result = str(fuzzy_set)

        # Assert
        assert "test_set" in result
        assert "2" in result
