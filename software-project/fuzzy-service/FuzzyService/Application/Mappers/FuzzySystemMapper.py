from typing import List, Dict, Any
from datetime import datetime

from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem as DomainFuzzySystem
from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId

# Importar otros mappers
from .FuzzyVariableMapper import FuzzyVariableMapper
from .FuzzyRuleMapper import FuzzyRuleMapper


class FuzzySystemMapper:
    """Mapper para conversiones entre FuzzySystem de dominio y una representación infra serializable.
    Se eliminan dependencias de clases de infraestructura duplicadas; se usan dicts para configuración infra.
    """
    
    @staticmethod
    def to_infra_config(domain_system: DomainFuzzySystem) -> Dict[str, Any]:
        """Convierte un FuzzySystem del dominio a un dict de configuración infra serializable."""
        # Convertir variables y reglas usando mappers específicos
        infra_variables = [FuzzyVariableMapper.to_infra(v) for v in domain_system.variables]
        infra_rules = [FuzzyRuleMapper.to_infra(r) for r in domain_system.rules]
        
        return {
            "system_id": str(domain_system.id),
            "name": domain_system.name,
            "description": domain_system.description,
            "variables": infra_variables,
            "rules": infra_rules,
            "is_active": domain_system.is_active,
        }
    
    @staticmethod
    def to_domain(infra_config: Dict[str, Any], system_id: str | None = None) -> DomainFuzzySystem:
        """Convierte un dict de configuración infra a FuzzySystem del dominio."""
        final_system_id = system_id or infra_config.get("system_id")
        
        # Convertir variables
        domain_variables: List[DomainFuzzyVariable] = []
        for infra_variable in infra_config.get("variables", []):
            # FuzzyVariableMapper.to_domain acepta el tipo de infraestructura; mantenemos compatibilidad
            domain_variable = FuzzyVariableMapper.to_domain(
                infra_variable,
                final_system_id
            )
            domain_variables.append(domain_variable)
        
        # Convertir reglas
        domain_rules = [
            FuzzyRuleMapper.to_domain(infra_rule, final_system_id)
            for infra_rule in infra_config.get("rules", [])
        ]
        
        return DomainFuzzySystem(
            id=FuzzySystemId(final_system_id),
            name=infra_config.get("name", ""),
            description=infra_config.get("description"),
            variables=domain_variables,
            rules=domain_rules,
            is_active=infra_config.get("is_active", True),
            created_at=datetime.utcnow(),
            updated_at=datetime.utcnow(),
        )
    
    @staticmethod
    def update_domain_with_infra(domain_system: DomainFuzzySystem, infra_config: Dict[str, Any]) -> DomainFuzzySystem:
        """Actualiza un sistema de dominio con configuración de infraestructura (dict)."""
        # Actualizar variables: mantener existentes y agregar nuevas si aparecen en config
        updated_variables: List[DomainFuzzyVariable] = []
        for infra_variable in infra_config.get("variables", []):
            existing_variable = next(
                (var for var in domain_system.variables if getattr(infra_variable, "name", None) == var.name
                 or (isinstance(infra_variable, dict) and infra_variable.get("name") == var.name)),
                None,
            )
            if existing_variable:
                updated_variable = FuzzyVariableMapper.update_domain_with_infra_terms(
                    existing_variable,
                    infra_variable,
                )
                updated_variables.append(updated_variable)
            else:
                new_variable = FuzzyVariableMapper.to_domain(
                    infra_variable,
                    str(domain_system.id),
                )
                updated_variables.append(new_variable)
        
        # Actualizar reglas: mantener existentes y agregar nuevas si aparecen en config
        updated_rules = []
        for infra_rule in infra_config.get("rules", []):
            rule_id = None
            if isinstance(infra_rule, dict):
                rule_id = infra_rule.get("rule_id")
            else:
                rule_id = getattr(infra_rule, "rule_id", None)
            
            existing_rule = next(
                (rule for rule in domain_system.rules if str(rule.id) == str(rule_id)),
                None,
            )
            if existing_rule:
                updated_rules.append(existing_rule)
            else:
                new_rule = FuzzyRuleMapper.to_domain(infra_rule, str(domain_system.id))
                updated_rules.append(new_rule)
        
        return DomainFuzzySystem(
            id=domain_system.id,
            name=infra_config.get("name") or domain_system.name,
            description=infra_config.get("description") or domain_system.description,
            variables=updated_variables or domain_system.variables,
            rules=updated_rules or domain_system.rules,
            is_active=infra_config.get("is_active", domain_system.is_active),
            created_at=domain_system.created_at,
            updated_at=datetime.utcnow(),
        )