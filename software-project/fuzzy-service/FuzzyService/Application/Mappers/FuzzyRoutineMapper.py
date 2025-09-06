from typing import List, Dict, Any, Optional
from datetime import datetime, timezone

from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
from FuzzyService.Domain.ValueObjects.DomainId import FuzzyRoutineId


class FuzzyRoutineMapper:
    """Mapper específico para conversiones entre FuzzyRoutine de dominio e infraestructura."""
    
    @staticmethod
    def to_infra(domain_routine: FuzzyRoutine) -> Dict[str, Any]:
        """Convierte una FuzzyRoutine del dominio a formato de infraestructura."""
        return {
            "routineId": {"$oid": str(domain_routine.id)} if domain_routine.id else None,
            "routine_name": domain_routine.routine_name,
            "steps": [step.to_dict() for step in domain_routine.steps],
            "createdAt": domain_routine.created_at.isoformat() if domain_routine.created_at else None
        }
    
    @staticmethod
    def to_domain(infra_routine: Dict[str, Any]) -> FuzzyRoutine:
        """Convierte un diccionario de infraestructura a FuzzyRoutine del dominio."""
        return FuzzyRoutine.from_dict(infra_routine)