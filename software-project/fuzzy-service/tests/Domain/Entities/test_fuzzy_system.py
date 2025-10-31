import pytest
from datetime import datetime, timezone
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyRuleId
from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Enums.DefuzzificationMethod import DefuzzificationMethod


class TestFuzzySystemCreation:
    """Test suite for FuzzySystem creation and validation"""

    def test_create_fuzzy_system_with_minimal_data_should_succeed(self):
        """Should create FuzzySystem with minimal required data"""
        # Act
        system = FuzzySystem(name="Test System")

        # Assert
        assert system.name == "Test System"
        assert system.status == FuzzySystemStatus.DRAFT
        assert system.defuzzification_method == DefuzzificationMethod.CENTROID
        assert len(system.input_variable_ids) == 0
        assert len(system.output_variable_ids) == 0
        assert len(system.rule_ids) == 0
        assert system.created_at is not None

    def test_create_fuzzy_system_with_empty_name_should_fail(self):
        """Should raise error when name is empty"""
        # Act & Assert
        with pytest.raises(ValueError, match="nombre del sistema no puede estar vacío"):
            FuzzySystem(name="")

    def test_create_fuzzy_system_with_whitespace_name_should_fail(self):
        """Should raise error when name is only whitespace"""
        # Act & Assert
        with pytest.raises(ValueError, match="nombre del sistema no puede estar vacío"):
            FuzzySystem(name="   ")

    def test_create_fuzzy_system_with_short_name_should_fail(self):
        """Should raise error when name is too short"""
        # Act & Assert
        with pytest.raises(ValueError, match="debe tener al menos 3 caracteres"):
            FuzzySystem(name="AB")

    def test_create_fuzzy_system_with_long_name_should_fail(self):
        """Should raise error when name exceeds 100 characters"""
        # Act & Assert
        with pytest.raises(ValueError, match="no puede exceder 100 caracteres"):
            FuzzySystem(name="A" * 101)

    def test_create_fuzzy_system_with_duplicate_input_variables_should_fail(self):
        """Should raise error when input variables contain duplicates"""
        # Arrange
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="variables de entrada duplicadas"):
            FuzzySystem(
                name="Test System",
                input_variable_ids=[var_id, var_id]
            )

    def test_create_fuzzy_system_with_duplicate_output_variables_should_fail(self):
        """Should raise error when output variables contain duplicates"""
        # Arrange
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="variables de salida duplicadas"):
            FuzzySystem(
                name="Test System",
                output_variable_ids=[var_id, var_id]
            )

    def test_create_fuzzy_system_with_duplicate_rules_should_fail(self):
        """Should raise error when rules contain duplicates"""
        # Arrange
        rule_id = FuzzyRuleId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="reglas duplicadas"):
            FuzzySystem(
                name="Test System",
                rule_ids=[rule_id, rule_id]
            )

    def test_create_fuzzy_system_with_variable_in_both_input_and_output_should_fail(self):
        """Should raise error when a variable is both input and output"""
        # Arrange
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="no puede ser tanto de entrada como de salida"):
            FuzzySystem(
                name="Test System",
                input_variable_ids=[var_id],
                output_variable_ids=[var_id]
            )


class TestFuzzySystemVariableManagement:
    """Test suite for FuzzySystem variable management methods"""

    def test_add_input_variable_should_succeed(self):
        """Should add input variable to system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()

        # Act
        system.add_input_variable(var_id)

        # Assert
        assert var_id in system.input_variable_ids
        assert len(system.input_variable_ids) == 1

    def test_add_duplicate_input_variable_should_fail(self):
        """Should raise error when adding duplicate input variable"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_input_variable(var_id)

        # Act & Assert
        with pytest.raises(ValueError, match="ya existe como entrada"):
            system.add_input_variable(var_id)

    def test_add_input_variable_that_is_output_should_fail(self):
        """Should raise error when adding input variable that already exists as output"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_output_variable(var_id)

        # Act & Assert
        with pytest.raises(ValueError, match="ya existe como salida"):
            system.add_input_variable(var_id)

    def test_add_output_variable_should_succeed(self):
        """Should add output variable to system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()

        # Act
        system.add_output_variable(var_id)

        # Assert
        assert var_id in system.output_variable_ids
        assert len(system.output_variable_ids) == 1

    def test_add_duplicate_output_variable_should_fail(self):
        """Should raise error when adding duplicate output variable"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_output_variable(var_id)

        # Act & Assert
        with pytest.raises(ValueError, match="ya existe como salida"):
            system.add_output_variable(var_id)

    def test_add_output_variable_that_is_input_should_fail(self):
        """Should raise error when adding output variable that already exists as input"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_input_variable(var_id)

        # Act & Assert
        with pytest.raises(ValueError, match="ya existe como entrada"):
            system.add_output_variable(var_id)

    def test_remove_input_variable_should_succeed(self):
        """Should remove input variable from system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_input_variable(var_id)

        # Act
        system.remove_input_variable(var_id)

        # Assert
        assert var_id not in system.input_variable_ids
        assert len(system.input_variable_ids) == 0

    def test_remove_nonexistent_input_variable_should_fail(self):
        """Should raise error when removing input variable that doesn't exist"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="no existe como entrada"):
            system.remove_input_variable(var_id)

    def test_remove_output_variable_should_succeed(self):
        """Should remove output variable from system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_output_variable(var_id)

        # Act
        system.remove_output_variable(var_id)

        # Assert
        assert var_id not in system.output_variable_ids
        assert len(system.output_variable_ids) == 0

    def test_remove_nonexistent_output_variable_should_fail(self):
        """Should raise error when removing output variable that doesn't exist"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="no existe como salida"):
            system.remove_output_variable(var_id)


class TestFuzzySystemRuleManagement:
    """Test suite for FuzzySystem rule management methods"""

    def test_add_rule_should_succeed(self):
        """Should add rule to system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()

        # Act
        system.add_rule(rule_id)

        # Assert
        assert rule_id in system.rule_ids
        assert len(system.rule_ids) == 1

    def test_add_duplicate_rule_should_fail(self):
        """Should raise error when adding duplicate rule"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()
        system.add_rule(rule_id)

        # Act & Assert
        with pytest.raises(ValueError, match="ya existe en el sistema"):
            system.add_rule(rule_id)

    def test_remove_rule_should_succeed(self):
        """Should remove rule from system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()
        system.add_rule(rule_id)

        # Act
        system.remove_rule(rule_id)

        # Assert
        assert rule_id not in system.rule_ids
        assert len(system.rule_ids) == 0

    def test_remove_nonexistent_rule_should_fail(self):
        """Should raise error when removing rule that doesn't exist"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()

        # Act & Assert
        with pytest.raises(ValueError, match="no existe en el sistema"):
            system.remove_rule(rule_id)


class TestFuzzySystemStatusManagement:
    """Test suite for FuzzySystem status management"""

    def test_activate_system_with_complete_configuration_should_succeed(self):
        """Should activate system when it has all required components"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act
        system.activate()

        # Assert
        assert system.status == FuzzySystemStatus.ACTIVE

    def test_activate_system_without_input_variables_should_fail(self):
        """Should raise error when activating system without input variables"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act & Assert
        with pytest.raises(ValueError, match="al menos una variable de entrada"):
            system.activate()

    def test_activate_system_without_output_variables_should_fail(self):
        """Should raise error when activating system without output variables"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act & Assert
        with pytest.raises(ValueError, match="al menos una variable de salida"):
            system.activate()

    def test_activate_system_without_rules_should_fail(self):
        """Should raise error when activating system without rules"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())

        # Act & Assert
        with pytest.raises(ValueError, match="al menos una regla"):
            system.activate()

    def test_activate_already_active_system_should_do_nothing(self):
        """Should not raise error when activating already active system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.activate()

        # Act
        system.activate()  # Should not raise error

        # Assert
        assert system.status == FuzzySystemStatus.ACTIVE

    def test_deactivate_active_system_should_succeed(self):
        """Should deactivate active system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.activate()

        # Act
        system.deactivate()

        # Assert
        assert system.status == FuzzySystemStatus.INACTIVE

    def test_deactivate_non_active_system_should_do_nothing(self):
        """Should not raise error when deactivating non-active system"""
        # Arrange
        system = FuzzySystem(name="Test System")

        # Act
        system.deactivate()  # Should not raise error

        # Assert
        assert system.status == FuzzySystemStatus.DRAFT

    def test_set_testing_mode_with_complete_configuration_should_succeed(self):
        """Should set testing mode when system has all required components"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act
        system.set_testing_mode()

        # Assert
        assert system.status == FuzzySystemStatus.TESTING

    def test_set_testing_mode_without_complete_configuration_should_fail(self):
        """Should raise error when setting testing mode without complete configuration"""
        # Arrange
        system = FuzzySystem(name="Test System")

        # Act & Assert
        with pytest.raises(ValueError, match="al menos una variable de entrada"):
            system.set_testing_mode()


class TestFuzzySystemQueryMethods:
    """Test suite for FuzzySystem query methods"""

    def test_is_operational_with_active_status_should_return_true(self):
        """Should return True for active system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.activate()

        # Act & Assert
        assert system.is_operational() is True

    def test_is_operational_with_testing_status_should_return_true(self):
        """Should return True for testing system"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.set_testing_mode()

        # Act & Assert
        assert system.is_operational() is True

    def test_is_editable_with_draft_status_should_return_true(self):
        """Should return True for draft system"""
        # Arrange
        system = FuzzySystem(name="Test System")

        # Act & Assert
        assert system.is_editable() is True

    def test_get_total_variables_should_return_sum(self):
        """Should return total count of input and output variables"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())

        # Act
        total = system.get_total_variables()

        # Assert
        assert total == 3

    def test_get_total_rules_should_return_count(self):
        """Should return total count of rules"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_rule(FuzzyRuleId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act
        total = system.get_total_rules()

        # Assert
        assert total == 3

    def test_has_variable_with_existing_input_variable_should_return_true(self):
        """Should return True when variable exists as input"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_input_variable(var_id)

        # Act & Assert
        assert system.has_variable(var_id) is True

    def test_has_variable_with_existing_output_variable_should_return_true(self):
        """Should return True when variable exists as output"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()
        system.add_output_variable(var_id)

        # Act & Assert
        assert system.has_variable(var_id) is True

    def test_has_variable_with_nonexistent_variable_should_return_false(self):
        """Should return False when variable doesn't exist"""
        # Arrange
        system = FuzzySystem(name="Test System")
        var_id = FuzzyVariableId.generate()

        # Act & Assert
        assert system.has_variable(var_id) is False

    def test_has_rule_with_existing_rule_should_return_true(self):
        """Should return True when rule exists"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()
        system.add_rule(rule_id)

        # Act & Assert
        assert system.has_rule(rule_id) is True

    def test_has_rule_with_nonexistent_rule_should_return_false(self):
        """Should return False when rule doesn't exist"""
        # Arrange
        system = FuzzySystem(name="Test System")
        rule_id = FuzzyRuleId.generate()

        # Act & Assert
        assert system.has_rule(rule_id) is False


class TestFuzzySystemConfiguration:
    """Test suite for FuzzySystem configuration methods"""

    def test_update_configuration_in_draft_mode_should_succeed(self):
        """Should update configuration when system is in editable state"""
        # Arrange
        system = FuzzySystem(name="Test System")
        new_method = DefuzzificationMethod.BISECTOR
        new_operators = OperatorsConfig()

        # Act
        system.update_configuration(
            defuzzification_method=new_method,
            operators=new_operators
        )

        # Assert
        assert system.defuzzification_method == new_method
        assert system.operators == new_operators

    def test_update_configuration_in_active_mode_should_fail(self):
        """Should raise error when updating configuration in active state"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())
        system.activate()

        # Act & Assert
        with pytest.raises(ValueError, match="No se puede modificar"):
            system.update_configuration(defuzzification_method=DefuzzificationMethod.BISECTOR)

    def test_update_configuration_with_partial_data_should_update_only_provided(self):
        """Should update only provided configuration fields"""
        # Arrange
        system = FuzzySystem(name="Test System")
        original_operators = system.operators
        new_method = DefuzzificationMethod.MOM

        # Act
        system.update_configuration(defuzzification_method=new_method)

        # Assert
        assert system.defuzzification_method == new_method
        assert system.operators == original_operators


class TestFuzzySystemSerialization:
    """Test suite for FuzzySystem serialization methods"""

    def test_to_dict_should_return_complete_dictionary(self):
        """Should convert FuzzySystem to dictionary"""
        # Arrange
        system = FuzzySystem(
            name="Test System",
            id=FuzzySystemId.generate()
        )
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act
        result = system.to_dict()

        # Assert
        assert result["name"] == "Test System"
        assert result["id"] is not None
        assert result["status"] == "DRAFT"
        assert "defuzzification_method" in result
        assert "operators" in result
        assert len(result["input_variable_ids"]) == 1
        assert len(result["output_variable_ids"]) == 1
        assert len(result["rule_ids"]) == 1

    def test_str_representation_should_include_key_info(self):
        """Should return string representation with key information"""
        # Arrange
        system = FuzzySystem(name="Test System")
        system.add_input_variable(FuzzyVariableId.generate())
        system.add_output_variable(FuzzyVariableId.generate())
        system.add_rule(FuzzyRuleId.generate())

        # Act
        result = str(system)

        # Assert
        assert "Test System" in result
        assert "DRAFT" in result
        assert "2 vars" in result
        assert "1 rules" in result
