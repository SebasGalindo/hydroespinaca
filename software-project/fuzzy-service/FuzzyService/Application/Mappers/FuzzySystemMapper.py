from typing import List, Dict, Any, Optional
from datetime import datetime

from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem as DomainFuzzySystem
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule as DomainFuzzyRule
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId, FuzzyVariableId, FuzzyRuleId

from FuzzyService.Infrastructure.FuzzyEngine.ScikitFuzzyEngine import (
    FuzzySystemConfig
)
from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
    FuzzyVariable as InfraFuzzyVariable
)
from FuzzyService.Infrastructure.FuzzyEngine.RuleEvaluationEngine import (
    InfraFuzzyRule
)

# Importar otros mappers
from .FuzzyVariableMapper import FuzzyVariableMapper
from .FuzzyRuleMapper import FuzzyRuleMapper


class FuzzySystemMapper:
    """Mapper específico para conversiones entre FuzzySystem de dominio e infraestructura."""
    
    @staticmethod
    def to_infra_config(domain_system: DomainFuzzySystem) -> FuzzySystemConfig:
        """Convierte un FuzzySystem del dominio a FuzzySystemConfig de infraestructura."""
        # Convertir variables
        infra_variables = []
        for domain_variable in domain_system.variables:
            infra_variable = FuzzyVariableMapper.to_infra(domain_variable)
            infra_variables.append(infra_variable)
        
        # Convertir reglas
        infra_rules = []
        for domain_rule in domain_system.rules:
            infra_rule = FuzzyRuleMapper.to_infra(domain_rule)
            infra_rules.append(infra_rule)
        
        return FuzzySystemConfig(
            system_id=str(domain_system.id),
            name=domain_system.name,
            description=domain_system.description,
            variables=infra_variables,
            rules=infra_rules,
            is_active=domain_system.is_active
        )
    
    @staticmethod
    def to_domain(infra_config: FuzzySystemConfig, system_id: str = None) -> DomainFuzzySystem:
        """Convierte un FuzzySystemConfig de infraestructura a FuzzySystem del dominio."""
        # Usar el system_id proporcionado o el del config
        final_system_id = system_id or infra_config.system_id
        
        # Convertir variables
        domain_variables = []
        for infra_variable in infra_config.variables:
            domain_variable = FuzzyVariableMapper.to_domain(
                infra_variable, 
                final_system_id
            )
            domain_variables.append(domain_variable)
        
        # Convertir reglas
        domain_rules = []
        for infra_rule in infra_config.rules:
            domain_rule = FuzzyRuleMapper.to_domain(
                infra_rule, 
                final_system_id
            )
            domain_rules.append(domain_rule)
        
        return DomainFuzzySystem(
            id=FuzzySystemId(final_system_id),
            name=infra_config.name,
            description=infra_config.description,
            variables=domain_variables,
            rules=domain_rules,
            is_active=getattr(infra_config, 'is_active', True),
            created_at=datetime.utcnow(),
            updated_at=datetime.utcnow()
        )
    
    @staticmethod
    def update_domain_with_infra(domain_system: DomainFuzzySystem, infra_config: FuzzySystemConfig) -> DomainFuzzySystem:
        """Actualiza un sistema de dominio con configuración de infraestructura."""
        # Actualizar variables
        updated_variables = []
        for infra_variable in infra_config.variables:
            # Buscar variable existente en el dominio
            existing_variable = next(
                (var for var in domain_system.variables if var.name == infra_variable.name),
                None
            )
            
            if existing_variable:
                # Actualizar variable existente con términos de infraestructura
                updated_variable = FuzzyVariableMapper.update_domain_with_infra_terms(
                    existing_variable, 
                    infra_variable
                )
                updated_variables.append(updated_variable)
            else:
                # Crear nueva variable
                new_variable = FuzzyVariableMapper.to_domain(
                    infra_variable, 
                    str(domain_system.id)
                )
                updated_variables.append(new_variable)
        
        # Actualizar reglas
        updated_rules = []
        for infra_rule in infra_config.rules:
            # Buscar regla existente en el dominio
            existing_rule = next(
                (rule for rule in domain_system.rules if str(rule.id) == infra_rule.rule_id),
                None
            )
            
            if existing_rule:
                # Mantener regla existente (podríamos actualizarla si es necesario)
                updated_rules.append(existing_rule)
            else:
                # Crear nueva regla
                new_rule = FuzzyRuleMapper.to_domain(
                    infra_rule, 
                    str(domain_system.id)
                )
                updated_rules.append(new_rule)
        
        # Crear sistema actualizado
        return DomainFuzzySystem(
            id=domain_system.id,
            name=infra_config.name or domain_system.name,
            description=infra_config.description or domain_system.description,
            variables=updated_variables,
            rules=updated_rules,
            is_active=getattr(infra_config, 'is_active', domain_system.is_active),
            created_at=domain_system.created_at,
            updated_at=datetime.utcnow()
        )