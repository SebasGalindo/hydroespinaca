import pytest
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType


class TestMembershipFunction:
    """Tests for MembershipFunction value object"""

    def test_create_triangular_function_with_valid_parameters_should_succeed(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.TRIANGULAR
        assert mf.parameters == [10.0, 20.0, 30.0]
        assert mf.universe_min == 0.0
        assert mf.universe_max == 50.0

    def test_create_triangular_function_with_wrong_parameter_count_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Función triangular requiere exactamente 3 parámetros"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[10.0, 20.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_triangular_function_with_invalid_parameter_order_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Para función triangular: a <= b <= c"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[30.0, 20.0, 10.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_trapezoidal_function_with_valid_parameters_should_succeed(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRAPEZOIDAL,
            parameters=[10.0, 20.0, 30.0, 40.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.TRAPEZOIDAL
        assert mf.parameters == [10.0, 20.0, 30.0, 40.0]

    def test_create_trapezoidal_function_with_wrong_parameter_count_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Función trapezoidal requiere exactamente 4 parámetros"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRAPEZOIDAL,
                parameters=[10.0, 20.0, 30.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_trapezoidal_function_with_invalid_parameter_order_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Para función trapezoidal: a <= b <= c <= d"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRAPEZOIDAL,
                parameters=[40.0, 30.0, 20.0, 10.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_gaussian_function_with_valid_parameters_should_succeed(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type=MembershipFunctionType.GAUSSIAN,
            parameters=[25.0, 5.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.GAUSSIAN
        assert mf.parameters == [25.0, 5.0]

    def test_create_gaussian_function_with_wrong_parameter_count_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Función gaussiana requiere exactamente 2 parámetros"):
            MembershipFunction(
                function_type=MembershipFunctionType.GAUSSIAN,
                parameters=[25.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_gaussian_function_with_negative_sigma_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Sigma debe ser mayor que 0 para función gaussiana"):
            MembershipFunction(
                function_type=MembershipFunctionType.GAUSSIAN,
                parameters=[25.0, -5.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_gaussian_function_with_zero_sigma_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Sigma debe ser mayor que 0 para función gaussiana"):
            MembershipFunction(
                function_type=MembershipFunctionType.GAUSSIAN,
                parameters=[25.0, 0.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_sigmoid_function_with_valid_parameters_should_succeed(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type=MembershipFunctionType.SIGMOID,
            parameters=[1.0, 25.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.SIGMOID
        assert mf.parameters == [1.0, 25.0]

    def test_create_sigmoid_function_with_wrong_parameter_count_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Función sigmoide requiere exactamente 2 parámetros"):
            MembershipFunction(
                function_type=MembershipFunctionType.SIGMOID,
                parameters=[1.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_bell_function_with_valid_parameters_should_succeed(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type=MembershipFunctionType.BELL,
            parameters=[5.0, 2.0, 25.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.BELL
        assert mf.parameters == [5.0, 2.0, 25.0]

    def test_create_bell_function_with_wrong_parameter_count_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Función campana requiere exactamente 3 parámetros"):
            MembershipFunction(
                function_type=MembershipFunctionType.BELL,
                parameters=[5.0, 2.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_bell_function_with_negative_a_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Parámetros a y b deben ser mayores que 0"):
            MembershipFunction(
                function_type=MembershipFunctionType.BELL,
                parameters=[-5.0, 2.0, 25.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_bell_function_with_negative_b_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Parámetros a y b deben ser mayores que 0"):
            MembershipFunction(
                function_type=MembershipFunctionType.BELL,
                parameters=[5.0, -2.0, 25.0],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_function_with_empty_parameters_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="Los parámetros no pueden estar vacíos"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[],
                universe_min=0.0,
                universe_max=50.0
            )

    def test_create_function_with_invalid_universe_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="universe_min debe ser menor que universe_max"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[10.0, 20.0, 30.0],
                universe_min=50.0,
                universe_max=0.0
            )

    def test_create_function_with_equal_universe_bounds_should_fail(self):
        # Arrange, Act & Assert
        with pytest.raises(ValueError, match="universe_min debe ser menor que universe_max"):
            MembershipFunction(
                function_type=MembershipFunctionType.TRIANGULAR,
                parameters=[10.0, 20.0, 30.0],
                universe_min=25.0,
                universe_max=25.0
            )

    def test_get_parameter_names_for_triangular_should_return_correct_names(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        names = mf.get_parameter_names()

        # Assert
        assert names == ["a", "b", "c"]

    def test_get_parameter_names_for_trapezoidal_should_return_correct_names(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRAPEZOIDAL,
            parameters=[10.0, 20.0, 30.0, 40.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        names = mf.get_parameter_names()

        # Assert
        assert names == ["a", "b", "c", "d"]

    def test_get_parameter_names_for_gaussian_should_return_correct_names(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.GAUSSIAN,
            parameters=[25.0, 5.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        names = mf.get_parameter_names()

        # Assert
        assert names == ["mean", "sigma"]

    def test_get_parameter_names_for_sigmoid_should_return_correct_names(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.SIGMOID,
            parameters=[1.0, 25.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        names = mf.get_parameter_names()

        # Assert
        assert names == ["a", "c"]

    def test_get_parameter_names_for_bell_should_return_correct_names(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.BELL,
            parameters=[5.0, 2.0, 25.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        names = mf.get_parameter_names()

        # Assert
        assert names == ["a", "b", "c"]

    def test_to_dict_should_return_correct_structure(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        result = mf.to_dict()

        # Assert
        assert result["function_type"] == "triangular"
        assert result["parameters"] == [10.0, 20.0, 30.0]
        assert result["universe_min"] == 0.0
        assert result["universe_max"] == 50.0

    def test_from_dict_should_create_valid_membership_function(self):
        # Arrange
        data = {
            "function_type": "trapezoidal",
            "parameters": [10.0, 20.0, 30.0, 40.0],
            "universe_min": 0.0,
            "universe_max": 50.0
        }

        # Act
        mf = MembershipFunction.from_dict(data)

        # Assert
        assert mf.function_type == MembershipFunctionType.TRAPEZOIDAL
        assert mf.parameters == [10.0, 20.0, 30.0, 40.0]
        assert mf.universe_min == 0.0
        assert mf.universe_max == 50.0

    def test_str_representation_should_include_type_and_parameters(self):
        # Arrange
        mf = MembershipFunction(
            function_type=MembershipFunctionType.TRIANGULAR,
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Act
        result = str(mf)

        # Assert
        assert "triangular" in result
        assert "10.0" in result
        assert "20.0" in result
        assert "30.0" in result

    def test_create_function_with_string_type_should_convert_to_enum(self):
        # Arrange & Act
        mf = MembershipFunction(
            function_type="triangular",
            parameters=[10.0, 20.0, 30.0],
            universe_min=0.0,
            universe_max=50.0
        )

        # Assert
        assert mf.function_type == MembershipFunctionType.TRIANGULAR
