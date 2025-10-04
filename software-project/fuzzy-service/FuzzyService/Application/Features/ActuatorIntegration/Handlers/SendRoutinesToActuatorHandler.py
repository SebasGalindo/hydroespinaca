from __future__ import annotations

from typing import List, Dict, Any

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendRoutinesToActuatorCommand import (
    SendRoutinesToActuatorCommand,
)
from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService
from FuzzyService.Domain.Errors.DomainErrors import ValidationError


class SendRoutinesToActuatorHandler(CommandHandler[SendRoutinesToActuatorCommand]):
    """Handler para enviar rutinas defuzzificadas al actuator-service.
    
    Este handler construye directamente el payload HTTP a partir del comando,
    evitando duplicar modelos con un DTO adicional.
    """

    async def __call__(self, request: SendRoutinesToActuatorCommand) -> None:
        """Procesa el comando de envío de rutinas al actuator.
        
        Args:
            request: Comando con el payload de rutinas a enviar
            
        Raises:
            ValidationError: Si hay errores de validación o comunicación
        """
        # Obtener servicio desde el contenedor de dependencias
        actuator_service: IActuatorService = di[IActuatorService]

        # Verificar que el actuator service esté disponible
        is_available = await actuator_service.is_available()
        if not is_available:
            raise ValidationError("El servicio de actuadores no está disponible")

        # Enviar las rutinas al actuator service
        try:
            # Convertir comando a payload (ya viene en formato correcto desde ScikitFuzzyEngine)
            payload: List[Dict[str, Any]] = []

            for routine in request.routines:
                routine_dict = {
                    "routineId": routine.routineId,
                    "steps": []
                }

                for step in routine.steps:
                    step_dict = {
                        "outputVariable": step.outputVariable,
                        "duration": int(step.duration)
                    }

                    # Agregar power O dutyCycle (mutuamente exclusivos)
                    if step.power is not None:
                        step_dict["power"] = step.power
                    if step.dutyCycle is not None:
                        step_dict["dutyCycle"] = step.dutyCycle

                    routine_dict["steps"].append(step_dict)

                payload.append(routine_dict)

            if not payload:
                raise ValidationError("El payload de rutinas está vacío")

            success = await actuator_service.send_routines(payload)

            if not success:
                raise ValidationError("El servicio de actuadores rechazó las rutinas")

            # Establecer el resultado del comando
            request._result = True

        except Exception as e:
            error_msg = f"Error al enviar rutinas al actuator: {str(e)}"
            raise ValidationError(error_msg) from e