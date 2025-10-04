from typing import List, Dict, Any, Optional

from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable as DomainFuzzyVariable
from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm as DomainFuzzyTerm
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyVariableId, FuzzySystemId, FuzzyTermId
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction


class FuzzyVariableMapper:
    """Mapper específico para conversiones entre FuzzyVariable de dominio y una representación infra serializable (dict).
    Se eliminan dependencias de clases de infraestructura duplicadas.
    """
    
    @staticmethod
    def to_infra(domain_variable: DomainFuzzyVariable) -> Dict[str, Any]:
        """Convierte una FuzzyVariable del dominio a un dict serializable para infraestructura.

        Nota: El cálculo del universo se realiza posteriormente en el proceso de evaluación fuzzy
        cuando se tienen acceso a los términos completos, ya que domain_variable.terms solo
        contiene IDs de términos, no los objetos completos.
        """
        return {
            "name": domain_variable.name,
            "terms": [str(term_id) for term_id in domain_variable.terms],  # Solo IDs
            "description": domain_variable.description,
            "variable_type": domain_variable.variable_type,
            "reference_id": domain_variable.reference_id,
        }
    
    @staticmethod
    def _get_attr(obj: Any, key: str, default: Any = None) -> Any:
        """Obtiene un atributo compatible con dict u objeto."""
        if isinstance(obj, dict):
            return obj.get(key, default)
        return getattr(obj, key, default)

    @staticmethod
    def to_domain(infra_variable: Any, system_id: str, variable_id: Optional[str] = None) -> DomainFuzzyVariable:
        """Convierte una variable infra (dict u objeto con atributos) a FuzzyVariable del dominio.

        Nota: Los términos se manejan por separado ya que requieren acceso a objetos completos
        que no están disponibles en esta conversión básica.
        """
        name = FuzzyVariableMapper._get_attr(infra_variable, "name", "")
        description = FuzzyVariableMapper._get_attr(infra_variable, "description", "")
        variable_type = FuzzyVariableMapper._get_attr(infra_variable, "variable_type", "input")
        reference_id = FuzzyVariableMapper._get_attr(infra_variable, "reference_id", None)
        terms = FuzzyVariableMapper._get_attr(infra_variable, "terms", [])

        # Convertir términos de strings a FuzzyTermId si es necesario
        term_ids = []
        for term in terms:
            if isinstance(term, str):
                term_ids.append(FuzzyTermId(term))
            else:
                # Si es un objeto complejo, extraer solo el ID
                term_id = FuzzyVariableMapper._get_attr(term, "id", None)
                if term_id:
                    term_ids.append(FuzzyTermId(term_id))

        return DomainFuzzyVariable(
            id=FuzzyVariableId(variable_id) if variable_id else FuzzyVariableId.generate(),
            name=name,
            description=description or "",
            variable_type=variable_type,
            reference_id=reference_id,
            terms=term_ids,
        )
    
    @staticmethod
    def update_domain_with_infra_terms(domain_variable: DomainFuzzyVariable, infra_variable: Any) -> DomainFuzzyVariable:
        """Actualiza una variable de dominio con información provenientes de infraestructura.

        Nota: Solo actualiza campos básicos. Los términos se manejan por separado ya que
        requieren acceso a objetos completos que no están disponibles aquí.
        """
        # Actualizar campos básicos desde infraestructura
        name = FuzzyVariableMapper._get_attr(infra_variable, "name", domain_variable.name)
        description = FuzzyVariableMapper._get_attr(infra_variable, "description", domain_variable.description)
        variable_type = FuzzyVariableMapper._get_attr(infra_variable, "variable_type", domain_variable.variable_type)
        reference_id = FuzzyVariableMapper._get_attr(infra_variable, "reference_id", domain_variable.reference_id)
        terms = FuzzyVariableMapper._get_attr(infra_variable, "terms", [])

        # Convertir términos a IDs si es necesario
        term_ids = []
        for term in terms:
            if isinstance(term, str):
                term_ids.append(FuzzyTermId(term))
            else:
                # Si es un objeto complejo, extraer solo el ID
                term_id = FuzzyVariableMapper._get_attr(term, "id", None)
                if term_id:
                    term_ids.append(FuzzyTermId(term_id))

        # Usar términos actualizados si se proporcionaron, sino mantener los existentes
        final_terms = term_ids if term_ids else domain_variable.terms

        # Crear nueva instancia de variable con información actualizada
        return DomainFuzzyVariable(
            id=domain_variable.id,
            name=name,
            description=description,
            variable_type=variable_type,
            reference_id=reference_id,
            terms=final_terms,
        )