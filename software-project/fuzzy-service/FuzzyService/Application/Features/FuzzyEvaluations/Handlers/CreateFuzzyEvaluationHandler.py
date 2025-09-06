from __future__ import annotations

from datetime import datetime, timezone

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyEvaluations.Commands.CreateFuzzyEvaluationCommand import CreateFuzzyEvaluationCommand
from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, RuleActivation
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyEvaluationId
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, ValidationError


class CreateFuzzyEvaluationHandler(CommandHandler[CreateFuzzyEvaluationCommand]):
    """Handler para crear una nueva evaluación fuzzy.
    
    Este handler valida que el sistema fuzzy exista, crea la entidad
    de evaluación y la persiste en el repositorio.
    """

    async def __call__(self, request: CreateFuzzyEvaluationCommand) -> None:
        """Procesa el comando de creación de evaluación fuzzy.
        
        Args:
            request: Comando con los datos de la evaluación a crear
            
        Raises:
            EntityNotFoundError: Si el sistema fuzzy no existe
            ValidationError: Si los datos de entrada son inválidos
        """
        # Obtener repositorios desde el contenedor de dependencias
        evaluation_repo: IFuzzyEvaluationRepository = di[IFuzzyEvaluationRepository]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        
        # Validar que el sistema fuzzy existe
        system_id = FuzzySystemId(request.system_id)
        system = await system_repo.get_by_id(system_id)
        if not system:
            raise EntityNotFoundError(f"Sistema fuzzy con ID '{request.system_id}' no encontrado")
        
        # Crear la entidad de evaluación fuzzy
        evaluation = FuzzyEvaluation(
            id=FuzzyEvaluationId.generate(),  # Generar nuevo ID
            system_id=system_id,
            timestamp=request.timestamp or datetime.now(timezone.utc)
        )
        
        # Agregar valores de entrada
        for input_dto in request.inputs:
            input_value = InputValue(
                sensor_id=input_dto.sensor_id,
                value=input_dto.value
            )
            evaluation.add_input(input_dto.sensor_id, input_dto.value)
        
        # Agregar reglas activadas
        for rule_dto in request.activated_rules:
            # Convertir DTO a entidad
            rule_activation = rule_dto.to_entity()
            evaluation.add_rule_activation(rule_activation)
        
        # Validar la evaluación antes de persistir
        if not evaluation.inputs:
            raise ValidationError("La evaluación debe tener al menos un valor de entrada")
        
        if not evaluation.activated_rules:
            raise ValidationError("La evaluación debe tener al menos una regla activada")
        
        # Persistir la evaluación en el repositorio
        created_evaluation = await evaluation_repo.create(evaluation)
        if not created_evaluation:
            raise ValidationError("Error al crear la evaluación fuzzy")
        
        # Convertir la entidad creada a DTO para el resultado
        result_dto = FuzzyEvaluationDto.from_entity(created_evaluation)
        request._result = result_dto