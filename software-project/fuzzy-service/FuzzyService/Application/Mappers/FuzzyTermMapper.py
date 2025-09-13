from typing import List, Dict, Any, Optional

from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction


class FuzzyTermMapper:
    """Mapper específico para conversiones entre FuzzyTerm de dominio y una representación infra serializable (dict)."""
    
    @staticmethod
    def to_infra(domain_term: DomainFuzzyTerm, universe_range: tuple) -> Dict[str, Any]:
        """Convierte un FuzzyTerm del dominio a dict serializable.
        Requiere universe_range explícito para evitar rangos por defecto implícitos.
        """
        return {
            "name": domain_term.name,
            "membership_function": domain_term.membership_function.to_dict(),
            "universe_range": universe_range,
        }
    
    @staticmethod
    def _get_attr(obj: Any, key: str, default: Any = None) -> Any:
        if isinstance(obj, dict):
            return obj.get(key, default)
        return getattr(obj, key, default)
    
    @staticmethod
    def to_domain(infra_term: Any, variable_id: str, term_id: Optional[str] = None) -> DomainFuzzyTerm:
        """Convierte un término infra (dict u objeto) a FuzzyTerm del dominio."""
        name = FuzzyTermMapper._get_attr(infra_term, "name", "")
        mf_dict = FuzzyTermMapper._get_attr(infra_term, "membership_function", {})
        membership_function = MembershipFunction.from_dict(mf_dict)
        
        return DomainFuzzyTerm(
            id=FuzzyTermId(term_id) if term_id else FuzzyTermId.generate(),
            variable_id=FuzzyVariableId(variable_id),
            name=name,
            membership_function=membership_function,
        )
    
    @staticmethod
    def to_infra_list(domain_terms: List[DomainFuzzyTerm], universe_range: tuple) -> List[Dict[str, Any]]:
        """Convierte una lista de FuzzyTerm del dominio a lista de dicts infra.
        Requiere universe_range explícito para todos los términos.
        """
        return [
            FuzzyTermMapper.to_infra(term, universe_range)
            for term in domain_terms
        ]
    
    @staticmethod
    def to_domain_list(infra_terms: List[Any], variable_id: str) -> List[DomainFuzzyTerm]:
        """Convierte una lista de términos infra (dicts/objetos) a lista de FuzzyTerm del dominio."""
        return [
            FuzzyTermMapper.to_domain(term, variable_id)
            for term in infra_terms
        ]
    
    @staticmethod
    def update_domain_term(domain_term: DomainFuzzyTerm, infra_term: Any) -> DomainFuzzyTerm:
        """Actualiza un término de dominio con datos de infraestructura (dict/objeto)."""
        mf_dict = FuzzyTermMapper._get_attr(infra_term, "membership_function", {})
        membership_function = MembershipFunction.from_dict(mf_dict)
        
        return DomainFuzzyTerm(
            id=domain_term.id,
            variable_id=domain_term.variable_id,
            name=FuzzyTermMapper._get_attr(infra_term, "name", domain_term.name),
            membership_function=membership_function,
        )