from __future__ import annotations

import logging
from datetime import datetime, timezone
from typing import Any, Dict, List

from medyator import QueryHandler
from kink import di

from FuzzyService.Application.Features.FuzzySystems.Queries.SimulateFuzzySystem.SimulateFuzzySystemQuery import SimulateFuzzySystemQuery
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, BusinessRuleViolationError

_logger = logging.getLogger(__name__)


class SimulateFuzzySystemHandler(QueryHandler[SimulateFuzzySystemQuery, Dict[str, Any]]):
    """Handler de la query SimulateFuzzySystemQuery.
    
    Ejecuta una evaluación fuzzy completa SIN persistir el resultado
    ni enviar comandos a actuadores. Retorna toda la información
    de la simulación para inspección del usuario.
    """

    async def __call__(self, request: SimulateFuzzySystemQuery) -> Dict[str, Any]:  # type: ignore[override]
        system_repo: IFuzzySystemRepository = di[IFuzzySystemRepository]
        variable_repo: IFuzzyVariableRepository = di[IFuzzyVariableRepository]
        term_repo: IFuzzyTermRepository = di[IFuzzyTermRepository]
        rule_repo: IFuzzyRuleRepository = di[IFuzzyRuleRepository]
        fuzzy_engine: IFuzzyEngine = di[IFuzzyEngine]

        system_id = FuzzySystemId(request.id)

        # 1. Obtener el sistema
        system = await system_repo.get_by_id(system_id)
        if system is None:
            raise EntityNotFoundError(f"Sistema difuso con id '{request.id}' no encontrado")

        # 2. Validar que el sistema tiene la estructura mínima
        if not system.input_variable_ids:
            raise BusinessRuleViolationError("El sistema no tiene variables de entrada configuradas")
        if not system.output_variable_ids:
            raise BusinessRuleViolationError("El sistema no tiene variables de salida configuradas")
        if not system.rule_ids:
            raise BusinessRuleViolationError("El sistema no tiene reglas configuradas")

        # 3. Cargar todas las variables
        input_variables = []
        for var_id in system.input_variable_ids:
            var = await variable_repo.get_by_id(var_id)
            if var:
                input_variables.append(var)

        output_variables = []
        for var_id in system.output_variable_ids:
            var = await variable_repo.get_by_id(var_id)
            if var:
                output_variables.append(var)

        all_variables = input_variables + output_variables

        # 4. Cargar todos los términos
        all_terms = []
        for var in all_variables:
            terms = await term_repo.get_by_variable_id(var.id)
            if terms:
                all_terms.extend(terms)

        # 5. Cargar todas las reglas
        rules = await rule_repo.get_by_system_id(system_id)

        # 6. Construir sensor_readings desde los inputs de simulación
        # Mapear reference_code → valor (como lo espera el engine)
        sensor_readings: Dict[str, float] = {}
        for sim_input in request.inputs:
            sensor_readings[sim_input.reference_code] = sim_input.value

        # Validar que los reference_codes correspondan a variables de entrada
        available_ref_codes = {
            var.reference_code for var in input_variables if var.reference_code
        }
        provided_ref_codes = set(sensor_readings.keys())
        unknown_codes = provided_ref_codes - available_ref_codes
        if unknown_codes:
            _logger.warning(
                "Reference codes no reconocidos en simulación: %s (disponibles: %s)",
                unknown_codes, available_ref_codes,
            )

        missing_codes = available_ref_codes - provided_ref_codes
        if missing_codes:
            _logger.warning(
                "Variables de entrada sin valor en simulación: %s",
                missing_codes,
            )

        # 7. Ejecutar la evaluación fuzzy (SIN persistir)
        _logger.info(
            "Simulando sistema '%s' con inputs: %s",
            system.name, sensor_readings,
        )

        try:
            result = await fuzzy_engine.complete_fuzzy_evaluation(
                system=system,
                variables=all_variables,
                terms=all_terms,
                rules=rules,
                sensor_readings=sensor_readings,
                is_simulation=True,
            )
        except Exception as e:
            _logger.error("Error al simular evaluación fuzzy: %s", str(e), exc_info=True)
            raise BusinessRuleViolationError(f"Error en la simulación: {str(e)}")

        # 8. Construir respuesta de simulación
        rule_evaluation = result.get("rule_evaluation", {})
        activated_rules_data = rule_evaluation.get("activated_rules", [])
        defuzzification_results = result.get("defuzzification_results", {})

        # Formatear reglas activadas
        formatted_rules = []
        for rule_data in activated_rules_data:
            formatted_rules.append({
                "rule_id": rule_data.get("rule_id"),
                "rule_name": rule_data.get("rule_name", ""),
                "firing_strength": rule_data.get("firing_strength", 0.0),
                "output_values": rule_data.get("output_values", []),
            })

        # Formatear outputs finales
        final_outputs = []
        for var in output_variables:
            ref_code = var.reference_code or str(var.id)
            # Buscar en defuzzification_results
            crisp_value = defuzzification_results.get(ref_code)
            if crisp_value is not None:
                final_outputs.append({
                    "variable_name": var.name,
                    "reference_code": ref_code,
                    "crisp_value": crisp_value,
                    "actuator_type": var.actuator_type,
                })

        # Formatear inputs usados
        formatted_inputs = []
        for sim_input in request.inputs:
            # Buscar nombre de la variable
            var_name = sim_input.reference_code
            for var in input_variables:
                if var.reference_code == sim_input.reference_code:
                    var_name = var.name
                    break
            formatted_inputs.append({
                "reference_code": sim_input.reference_code,
                "variable_name": var_name,
                "value": sim_input.value,
            })

        simulation_response = {
            "system_id": str(system.id),
            "system_name": system.name,
            "inputs": formatted_inputs,
            "activated_rules": formatted_rules,
            "final_outputs": final_outputs,
            "simulated_at": datetime.now(timezone.utc).isoformat(),
        }

        _logger.info(
            "Simulación completada: %d reglas activadas, %d outputs generados",
            len(formatted_rules), len(final_outputs),
        )

        return simulation_response
