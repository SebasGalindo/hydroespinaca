from typing import List, Dict, Any, Optional

from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums import MembershipFunctionType

from FuzzyService.Infrastructure.FuzzyEngine.FuzzificationEngine import (
    FuzzyTerm as InfraFuzzyTerm
)


class FuzzyTermMapper:
    """Mapper específico para conversiones entre FuzzyTerm de dominio e infraestructura."""
    
    @staticmethod
    def to_infra(domain_term: DomainFuzzyTerm, universe_range: tuple = None) -> InfraFuzzyTerm:
        """Convierte un FuzzyTerm del dominio a InfraFuzzyTerm."""
        return InfraFuzzyTerm(
            name=domain_term.name,
            membership_function=domain_term.membership_function.to_dict(),
            universe_range=universe_range or (0, 100)  # Rango por defecto si no se proporciona
        )
    
    @staticmethod
    def to_domain(infra_term: InfraFuzzyTerm, variable_id: str, term_id: str = None) -> DomainFuzzyTerm:
        """Convierte un InfraFuzzyTerm a FuzzyTerm del dominio."""
        # Crear función de membresía desde el diccionario
        membership_function = MembershipFunction.from_dict(infra_term.membership_function)
        
        return DomainFuzzyTerm(
            id=FuzzyTermId(term_id) if term_id else FuzzyTermId.generate(),
            variable_id=FuzzyVariableId(variable_id),
            name=infra_term.name,
            membership_function=membership_function
        )
    
    @staticmethod
    def to_infra_list(domain_terms: List[DomainFuzzyTerm], universe_range: tuple = None) -> List[InfraFuzzyTerm]:
        """Convierte una lista de FuzzyTerm del dominio a lista de InfraFuzzyTerm."""
        return [
            FuzzyTermMapper.to_infra(term, universe_range)
            for term in domain_terms
        ]
    
    @staticmethod
    def to_domain_list(infra_terms: List[InfraFuzzyTerm], variable_id: str) -> List[DomainFuzzyTerm]:
        """Convierte una lista de InfraFuzzyTerm a lista de FuzzyTerm del dominio."""
        return [
            FuzzyTermMapper.to_domain(term, variable_id)
            for term in infra_terms
        ]
    
    @staticmethod
    def update_domain_term(domain_term: DomainFuzzyTerm, infra_term: InfraFuzzyTerm) -> DomainFuzzyTerm:
        """Actualiza un término de dominio con datos de infraestructura."""
        # Crear nueva función de membresía desde infraestructura
        membership_function = MembershipFunction.from_dict(infra_term.membership_function)
        
        return DomainFuzzyTerm(
            id=domain_term.id,
            variable_id=domain_term.variable_id,
            name=infra_term.name,
            membership_function=membership_function
        )