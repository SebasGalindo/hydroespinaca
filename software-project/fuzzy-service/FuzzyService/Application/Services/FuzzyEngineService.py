from __future__ import annotations

from typing import List, Optional, Dict, Any
from datetime import datetime, timezone
import logging

from FuzzyService.Domain.Interfaces.IFuzzyEngine import IFuzzyEngine
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository
from FuzzyService.Domain.Interfaces.IFuzzyEvaluationRepository import IFuzzyEvaluationRepository

from FuzzyService.Domain.Entities.fuzzy_evaluation import FuzzyEvaluation, InputValue, OutputValue
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Enums.DefuzzificationMethod import DefuzzificationMethod
from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
from FuzzyService.Domain.Errors.DomainErrors import EntityNotFoundError, ValidationError

# Infrastructure imports
from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
    ScikitFuzzyEngine,
    FuzzyEvaluationRequest,
    FuzzyEvaluationResponse
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration
)

# Mappers específicos
from FuzzyService.Application.Mappers import (
    FuzzyRuleMapper,
    FuzzyEvaluationMapper
)


class FuzzyEngineService(IFuzzyEngine):
    """Servicio de aplicación que implementa IFuzzyEngine y actúa como adaptador
    entre el dominio y la infraestructura del motor fuzzy.
    
    Este servicio respeta clean architecture:
    - Implementa la interfaz del dominio (IFuzzyEngine)
    - Usa repositorios del dominio para cargar datos
    - Convierte entre entidades del dominio y DTOs de infraestructura
    - Orquesta el motor fuzzy de infraestructura
    """
    
    def __init__(self,
                 scikit_engine: ScikitFuzzyEngine,
                 system_repo: IFuzzySystemRepository,
                 variable_repo: IFuzzyVariableRepository,
                 term_repo: IFuzzyTermRepository,
                 rule_repo: IFuzzyRuleRepository,
                 routine_repo: IFuzzyRoutineRepository,
                 evaluation_repo: IFuzzyEvaluationRepository):
        self.scikit_engine = scikit_engine
        self.system_repo = system_repo
        self.variable_repo = variable_repo
        self.term_repo = term_repo
        self.rule_repo = rule_repo
        self.routine_repo = routine_repo
        self.evaluation_repo = evaluation_repo
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
    
    async def evaluate(self, 
                      system_id: FuzzySystemId, 
                      inputs: List[InputValue], *,
                      at: Optional[datetime] = None,
                      defuzz_method: Optional[DefuzzificationMethod] = None) -> FuzzyEvaluation:
        """Evalúa el sistema fuzzy para las entradas proporcionadas.
        
        Args:
            system_id: Identificador del sistema fuzzy objetivo
            inputs: Lista de entradas crisp con identificadores de sensores
            at: Tiempo de evaluación opcional (por defecto ahora UTC)
            defuzz_method: Método de defuzzificación opcional
        
        Returns:
            Una entidad FuzzyEvaluation del dominio capturando el resultado de la evaluación
        
        Raises:
            EntityNotFoundError: Si el sistema no existe
            ValidationError: Si las entradas son inválidas o incompletas para el sistema
        """
        try:
            self.logger.info(f"Iniciando evaluación fuzzy para sistema {system_id}")
            
            # 1. Cargar configuración del sistema desde repositorios
            system = await self.system_repo.get_by_id(system_id)
            if system is None:
                raise EntityNotFoundError(f"Sistema fuzzy {system_id} no encontrado")
            
            if system.status != FuzzySystemStatus.IN_USE:
                raise ValidationError(f"Sistema fuzzy {system_id} no está activo (status: {system.status})")
            
            # 2. Cargar componentes del sistema
            variables = await self.variable_repo.get_by_system_id(system_id)
            if not variables:
                raise ValidationError(f"No se encontraron variables para el sistema {system_id}")
            
            # Obtener IDs de variables para cargar términos
            variable_ids = [var.id for var in variables if var.id is not None]
            terms = await self.term_repo.get_by_variable_ids(variable_ids)
            
            rules = await self.rule_repo.get_by_system_id(system_id)
            if not rules:
                raise ValidationError(f"No se encontraron reglas para el sistema {system_id}")
            
            # 3. Convertir entidades del dominio a estructuras del motor
            engine_request = await self._convert_to_engine_request(
                system, variables, terms, rules, inputs, at, defuzz_method
            )
            
            # 4. Ejecutar motor fuzzy de infraestructura
            self.logger.debug(f"Ejecutando motor fuzzy para request {engine_request.request_id}")
            engine_response = await self.scikit_engine.evaluate_fuzzy_logic(engine_request)
            
            # 5. Convertir respuesta a entidad del dominio
            domain_evaluation = await self._convert_to_domain_evaluation(
                engine_response, system_id, inputs, at
            )
            
            # 6. Persistir evaluación
            await self.evaluation_repo.save(domain_evaluation)
            
            self.logger.info(
                f"Evaluación fuzzy completada para sistema {system_id} "
                f"en {engine_response.processing_time_ms:.2f}ms"
            )
            
            return domain_evaluation
            
        except Exception as e:
            self.logger.error(f"Error en evaluación fuzzy para sistema {system_id}: {str(e)}")
            raise
    
    async def supported_defuzz_methods(self) -> List[DefuzzificationMethod]:
        """Retorna la lista de métodos de defuzzificación soportados por la implementación del motor."""
        # Mapear métodos de infraestructura a enums del dominio
        from FuzzyService.Infrastructure.FuzzyEngine.FuzzyEngineConfiguration import DefuzzificationMethod as InfraDefuzzMethod
        
        infra_methods = list(InfraDefuzzMethod)
        domain_methods = []
        
        for infra_method in infra_methods:
            try:
                domain_method = DefuzzificationMethod(infra_method.value)
                domain_methods.append(domain_method)
            except ValueError:
                # Método de infraestructura no soportado en dominio
                continue
        
        return domain_methods
    
    async def warm_up(self) -> None:
        """Prepara caches internos o precomputaciones si el motor lo soporta."""
        self.logger.info("Iniciando warm-up del motor fuzzy")
        
        # El ScikitFuzzyEngine no tiene método warm_up explícito,
        # pero podemos inicializar componentes si es necesario
        try:
            # Verificar que todos los componentes estén inicializados
            if hasattr(self.scikit_engine, '_initialize_components'):
                self.scikit_engine._initialize_components()
            
            self.logger.info("Warm-up del motor fuzzy completado")
        except Exception as e:
            self.logger.warning(f"Error durante warm-up del motor fuzzy: {str(e)}")
    
    async def _convert_to_engine_request(self,
                                        system,
                                        variables,
                                        terms,
                                        rules,
                                        inputs: List[InputValue],
                                        at: Optional[datetime],
                                        defuzz_method: Optional[DefuzzificationMethod]) -> FuzzyEvaluationRequest:
        """Convierte entidades del dominio a FuzzyEvaluationRequest de infraestructura.
        
        Utiliza mappers específicos para cada entidad.
        """
        
        # Convertir inputs a dict sensor_id -> value
        sensor_data = {input_val.sensor_id: input_val.value for input_val in inputs}
        
        # Construir membership_functions dict usando términos
        membership_functions = {}
        for variable in variables:
            if variable.name not in membership_functions:
                membership_functions[variable.name] = {}
            
            # Encontrar términos para esta variable
            variable_terms = [t for t in terms if t.variable_id == variable.id]
            for term in variable_terms:
                if term.mf and term.mf.type and term.mf.params:
                    membership_functions[variable.name][term.label] = {
                        "type": term.mf.type.value if hasattr(term.mf.type, 'value') else str(term.mf.type),
                        "params": term.mf.params
                    }
        
        # Usar mapper específico para convertir reglas
        infra_rules = [FuzzyRuleMapper.to_infra(rule) for rule in rules]
        
        return FuzzyEvaluationRequest(
            request_id=f"eval_{system.id}_{int((at or datetime.now(timezone.utc)).timestamp())}",
            system_id=str(system.id),
            sensor_data=sensor_data,
            timestamp=at or datetime.now(timezone.utc),
            rules=infra_rules,
            membership_functions=membership_functions
        )
    
    async def _convert_to_domain_evaluation(self,
                                           engine_response: FuzzyEvaluationResponse,
                                           system_id: FuzzySystemId,
                                           inputs: List[InputValue],
                                           at: Optional[datetime]) -> FuzzyEvaluation:
        """Convierte FuzzyEvaluationResponse de infraestructura a FuzzyEvaluation del dominio.
        
        Utiliza mappers específicos para cada entidad.
        """
        
        # Usar mapper específico para la conversión
        return FuzzyEvaluationMapper.from_rule_evaluation_result(
            engine_response, system_id, inputs
        )