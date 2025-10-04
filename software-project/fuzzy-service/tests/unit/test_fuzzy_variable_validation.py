"""Tests unitarios para validación de reference_id en FuzzyVariable."""

import pytest
from unittest.mock import AsyncMock, MagicMock
from kink import di

from FuzzyService.Application.Features.FuzzyVariables.Commands.CreateFuzzyVariableCommand import (
    CreateFuzzyVariableCommand,
)
from FuzzyService.Application.Features.FuzzyVariables.Commands.UpdateFuzzyVariableCommand import (
    UpdateFuzzyVariableCommand,
)
from FuzzyService.Application.Features.FuzzyVariables.Handlers.CreateFuzzyVariableHandler import (
    CreateFuzzyVariableHandler,
)
from FuzzyService.Application.Features.FuzzyVariables.Handlers.UpdateFuzzyVariableHandler import (
    UpdateFuzzyVariableHandler,
)
from FuzzyService.Domain.Errors.DomainErrors import InvalidReferenceException, EntityNotFoundError
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.ISensorService import ISensorService
from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService


@pytest.fixture
def mock_system_repo():
    """Mock del repositorio de sistemas fuzzy."""
    repo = AsyncMock(spec=IFuzzySystemRepository)
    # Simular que el sistema existe
    mock_system = MagicMock()
    mock_system.id = "system_123"
    mock_system.add_input_variable = MagicMock()
    mock_system.add_output_variable = MagicMock()
    repo.get_by_id = AsyncMock(return_value=mock_system)
    repo.update = AsyncMock(return_value=mock_system)
    return repo


@pytest.fixture
def mock_variable_repo():
    """Mock del repositorio de variables fuzzy."""
    repo = AsyncMock(spec=IFuzzyVariableRepository)
    mock_variable = MagicMock()
    mock_variable.id = "var_123"
    mock_variable.reference_id = "old_ref_123"
    mock_variable.variable_type = "input"
    repo.create = AsyncMock(return_value=mock_variable)
    repo.get_by_id = AsyncMock(return_value=mock_variable)
    repo.update = AsyncMock(return_value=mock_variable)
    return repo


@pytest.fixture
def mock_sensor_service():
    """Mock del servicio de sensor-service."""
    service = AsyncMock(spec=ISensorService)
    service.validate_variable_exists = AsyncMock(return_value=True)
    return service


@pytest.fixture
def mock_actuator_service():
    """Mock del servicio de actuator-service."""
    service = AsyncMock(spec=IActuatorService)
    service.validate_output_exists = AsyncMock(return_value=True)
    return service


@pytest.fixture(autouse=True)
def setup_di(mock_system_repo, mock_variable_repo, mock_sensor_service, mock_actuator_service):
    """Configura el DI container con mocks."""
    di[IFuzzySystemRepository] = mock_system_repo
    di[IFuzzyVariableRepository] = mock_variable_repo
    di[ISensorService] = mock_sensor_service
    di[IActuatorService] = mock_actuator_service
    yield
    # Cleanup
    di.clear_cache()


class TestCreateFuzzyVariableValidation:
    """Tests para validación en CreateFuzzyVariableHandler."""

    @pytest.mark.asyncio
    async def test_create_input_variable_valid_reference(self, mock_sensor_service):
        """Debe crear variable input cuando reference_id existe en sensor-service."""
        # Arrange
        command = CreateFuzzyVariableCommand(
            name="Temperature",
            variable_type="input",
            system_id="system_123",
            reference_id="sensor_var_456",
            description="Test input variable",
        )
        handler = CreateFuzzyVariableHandler()

        # Act
        await handler(command)

        # Assert
        mock_sensor_service.validate_variable_exists.assert_called_once_with("sensor_var_456")
        assert command._result is not None

    @pytest.mark.asyncio
    async def test_create_input_variable_invalid_reference(self, mock_sensor_service):
        """Debe lanzar InvalidReferenceException cuando reference_id no existe en sensor-service."""
        # Arrange
        mock_sensor_service.validate_variable_exists = AsyncMock(return_value=False)
        command = CreateFuzzyVariableCommand(
            name="Temperature",
            variable_type="input",
            system_id="system_123",
            reference_id="invalid_sensor_var",
        )
        handler = CreateFuzzyVariableHandler()

        # Act & Assert
        with pytest.raises(InvalidReferenceException) as exc_info:
            await handler(command)

        assert exc_info.value.reference_id == "invalid_sensor_var"
        assert exc_info.value.service == "sensor-service /api/variables/{id}"
        assert exc_info.value.variable_type == "input"

    @pytest.mark.asyncio
    async def test_create_output_variable_valid_reference(self, mock_actuator_service):
        """Debe crear variable output cuando reference_id existe en actuator-service."""
        # Arrange
        command = CreateFuzzyVariableCommand(
            name="Pump Power",
            variable_type="output",
            system_id="system_123",
            reference_id="output_789",
            description="Test output variable",
        )
        handler = CreateFuzzyVariableHandler()

        # Act
        await handler(command)

        # Assert
        mock_actuator_service.validate_output_exists.assert_called_once_with("output_789")
        assert command._result is not None

    @pytest.mark.asyncio
    async def test_create_output_variable_invalid_reference(self, mock_actuator_service):
        """Debe lanzar InvalidReferenceException cuando reference_id no existe en actuator-service."""
        # Arrange
        mock_actuator_service.validate_output_exists = AsyncMock(return_value=False)
        command = CreateFuzzyVariableCommand(
            name="Pump Power",
            variable_type="output",
            system_id="system_123",
            reference_id="invalid_output",
        )
        handler = CreateFuzzyVariableHandler()

        # Act & Assert
        with pytest.raises(InvalidReferenceException) as exc_info:
            await handler(command)

        assert exc_info.value.reference_id == "invalid_output"
        assert exc_info.value.service == "actuator-service /api/outputs/{id}"
        assert exc_info.value.variable_type == "output"


class TestUpdateFuzzyVariableValidation:
    """Tests para validación en UpdateFuzzyVariableHandler."""

    @pytest.mark.asyncio
    async def test_update_input_variable_valid_reference(self, mock_sensor_service):
        """Debe actualizar variable input cuando nuevo reference_id existe en sensor-service."""
        # Arrange
        command = UpdateFuzzyVariableCommand(
            id="var_123",
            reference_id="new_sensor_var_999",
        )
        handler = UpdateFuzzyVariableHandler()

        # Act
        await handler(command)

        # Assert
        mock_sensor_service.validate_variable_exists.assert_called_once_with("new_sensor_var_999")

    @pytest.mark.asyncio
    async def test_update_input_variable_invalid_reference(self, mock_sensor_service):
        """Debe lanzar InvalidReferenceException cuando nuevo reference_id no existe."""
        # Arrange
        mock_sensor_service.validate_variable_exists = AsyncMock(return_value=False)
        command = UpdateFuzzyVariableCommand(
            id="var_123",
            reference_id="invalid_new_ref",
        )
        handler = UpdateFuzzyVariableHandler()

        # Act & Assert
        with pytest.raises(InvalidReferenceException) as exc_info:
            await handler(command)

        assert exc_info.value.reference_id == "invalid_new_ref"

    @pytest.mark.asyncio
    async def test_update_variable_type_change_validates_new_service(
        self, mock_variable_repo, mock_actuator_service
    ):
        """Debe validar contra actuator-service cuando se cambia type de input a output."""
        # Arrange
        mock_var = MagicMock()
        mock_var.id = "var_123"
        mock_var.reference_id = "current_ref_123"
        mock_var.variable_type = "input"
        mock_variable_repo.get_by_id = AsyncMock(return_value=mock_var)

        command = UpdateFuzzyVariableCommand(
            id="var_123",
            variable_type="output",  # Cambio de input a output
        )
        handler = UpdateFuzzyVariableHandler()

        # Act
        await handler(command)

        # Assert
        # Debe validar contra actuator-service porque el nuevo tipo es output
        mock_actuator_service.validate_output_exists.assert_called_once_with("current_ref_123")

    @pytest.mark.asyncio
    async def test_update_no_reference_change_no_validation(
        self, mock_sensor_service, mock_actuator_service
    ):
        """No debe validar reference_id si no cambia."""
        # Arrange
        command = UpdateFuzzyVariableCommand(
            id="var_123",
            name="New Name",  # Solo cambia el nombre
        )
        handler = UpdateFuzzyVariableHandler()

        # Act
        await handler(command)

        # Assert
        # No debe llamar a ningún servicio de validación
        mock_sensor_service.validate_variable_exists.assert_not_called()
        mock_actuator_service.validate_output_exists.assert_not_called()


class TestRoutinePayloadSerialization:
    """Tests para verificar que rutinas usan outputVariableId."""

    def test_step_payload_uses_output_variable_id(self):
        """Debe usar outputVariableId en el payload de rutinas."""
        from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import (
            StepPayload,
        )

        # Act
        step = StepPayload(
            outputVariableId="650f83b079309f3ba4f238",
            power=60,
            duration=45,
        )

        # Assert
        assert step.outputVariableId == "650f83b079309f3ba4f238"
        assert step.power == 60
        assert step.duration == 45

    def test_step_payload_validates_empty_output_variable_id(self):
        """Debe rechazar outputVariableId vacío."""
        from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import (
            StepPayload,
        )

        # Act & Assert
        with pytest.raises(ValueError, match="outputVariableId no puede estar vacío"):
            StepPayload(
                outputVariableId="",
                power=60,
                duration=45,
            )
