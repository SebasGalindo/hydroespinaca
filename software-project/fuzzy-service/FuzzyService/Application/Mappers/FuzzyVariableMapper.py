from typing import List, Dict, Any, Optional

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzySystemId, FuzzyTermId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums import MembershipFunctionType

from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
    FuzzyVariable as InfraFuzzyVariable,
    FuzzyTerm as InfraFuzzyTerm
)


class FuzzyVariableMapper:
    """Mapper específico para conversiones entre FuzzyVariable de dominio e infraestructura."""
    
    @staticmethod
    def to_infra(domain_variable: DomainFuzzyVariable) -> InfraFuzzyVariable:
        """Convierte una FuzzyVariable del dominio a InfraFuzzyVariable."""
        # Convertir términos
        infra_terms = []
        for domain_term in domain_variable.terms:
            infra_term = InfraFuzzyTerm(
                name=domain_term.name,
                membership_function=domain_term.membership_function.to_dict(),
                universe_range=(domain_variable.min_value, domain_variable.max_value)
            )
            infra_terms.append(infra_term)
        
        return InfraFuzzyVariable(
            name=domain_variable.name,
            universe_range=(domain_variable.min_value, domain_variable.max_value),
            terms=infra_terms,
            description=domain_variable.description
        )
    
    @staticmethod
    def to_domain(infra_variable: InfraFuzzyVariable, system_id: str, variable_id: str = None) -> DomainFuzzyVariable:
        """Convierte una InfraFuzzyVariable a FuzzyVariable del dominio."""
        # Convertir términos
        domain_terms = []
        for infra_term in infra_variable.terms:
            # Crear función de membresía desde el diccionario
            membership_function = MembershipFunction.from_dict(infra_term.membership_function)
            
            domain_term = DomainFuzzyTerm(
                id=FuzzyTermId.generate(),
                variable_id=FuzzyVariableId(variable_id) if variable_id else FuzzyVariableId.generate(),
                name=infra_term.name,
                membership_function=membership_function
            )
            domain_terms.append(domain_term)
        
        return DomainFuzzyVariable(
            id=FuzzyVariableId(variable_id) if variable_id else FuzzyVariableId.generate(),
            system_id=FuzzySystemId(system_id),
            name=infra_variable.name,
            description=getattr(infra_variable, 'description', ''),
            min_value=infra_variable.universe_range[0],
            max_value=infra_variable.universe_range[1],
            terms=domain_terms
        )
    
    @staticmethod
    def update_domain_with_infra_terms(domain_variable: DomainFuzzyVariable, infra_variable: InfraFuzzyVariable) -> DomainFuzzyVariable:
        """Actualiza una variable de dominio con términos de infraestructura."""
        # Convertir términos de infraestructura a dominio
        domain_terms = []
        for infra_term in infra_variable.terms:
            # Buscar si el término ya existe en el dominio
            existing_term = next(
                (term for term in domain_variable.terms if term.name == infra_term.name),
                None
            )
            
            if existing_term:
                # Mantener el término existente
                domain_terms.append(existing_term)
            else:
                # Crear nuevo término
                membership_function = MembershipFunction.from_dict(infra_term.membership_function)
                new_term = DomainFuzzyTerm(
                    id=FuzzyTermId.generate(),
                    variable_id=domain_variable.id,
                    name=infra_term.name,
                    membership_function=membership_function
                )
                domain_terms.append(new_term)
        
        # Crear nueva instancia de variable con términos actualizados
        return DomainFuzzyVariable(
            id=domain_variable.id,
            system_id=domain_variable.system_id,
            name=domain_variable.name,
            description=domain_variable.description,
            min_value=domain_variable.min_value,
            max_value=domain_variable.max_value,
            terms=domain_terms
        )