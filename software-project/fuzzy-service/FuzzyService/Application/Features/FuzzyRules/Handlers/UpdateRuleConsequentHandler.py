from __future__ import annotations

from kink import di
from medyator import CommandHandler

from FuzzyService.Application.Features.FuzzyRules.Commands.UpdateRuleConsequentCommand import UpdateRuleConsequentCommand
from FuzzyService.Application.Features.FuzzyRules.DTOs.FuzzyRuleDto import FuzzyRuleDto
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRuleId, FuzzyVariableId, FuzzyTermId
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError


class UpdateRuleConsequentHandler(CommandHandler[UpdateRuleConsequentCommand]):
    """Handler para actualizar los consecuentes Mamdani de una regla difusa existente."""

    async def __call__(self, request: UpdateRuleConsequentCommand) -> None:
        # 1. Obtener repositorios
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]

        # 2. Verificar que la regla existe
        rule_id = FuzzyRuleId(request.rule_id)
        existing_rule = await rule_repo.get_by_id(rule_id)
        if not existing_rule:
            raise EntityNotFoundError(f"Regla con ID {request.rule_id} no encontrada")

        # 3. NOTA: El campo 'consequent' (rutina) fue eliminado.
        # Este handler ahora trabaja con 'consequents' (array de RuleConsequent)
        if hasattr(request, 'consequent') and request.consequent:
            raise BusinessRuleViolationError(
                "El campo 'consequent' (rutina) ya no está soportado. "
                "Use 'consequents' (array) con el nuevo modelo Mamdani."
            )

        # 4. Validar y crear nuevos consecuentes
        if not hasattr(request, 'consequents') or not request.consequents:
            raise BusinessRuleViolationError(
                "Debe proporcionar 'consequents' (array de RuleConsequent)"
            )

        new_consequents = []
        for cons_data in request.consequents:
            # Validar que los términos existen
            term_ids = []
            for term_id_str in cons_data.get('terms', []):
                term_id = FuzzyTermId(term_id_str)
                term = await term_repo.get_by_id(term_id)
                if not term:
                    raise EntityNotFoundError(f"Término con ID {term_id_str} no encontrado")
                term_ids.append(term_id)

            # Crear RuleConsequent
            consequent = RuleConsequent(
                variable_id=FuzzyVariableId(cons_data['variable_id']),
                terms=term_ids,
                aggregation_method=cons_data.get('aggregation_method', 'max')
            )
            new_consequents.append(consequent)

        # 5. Reemplazar consecuentes
        existing_rule.consequents = new_consequents

        # 6. Guardar la regla actualizada
        updated_rule = await rule_repo.update(existing_rule)

        # 7. Asignar resultado al comando
        request._result = FuzzyRuleDto.from_entity(updated_rule)