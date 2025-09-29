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
        # Normalizar claves de steps: aceptar tanto step_id como stepId
        normalized = dict(infra_routine)
        steps = normalized.get("steps")
        if isinstance(steps, list):
            norm_steps: List[Dict[str, Any]] = []
            for s in steps:
                if isinstance(s, dict):
                    s2 = dict(s)
                    if "step_id" in s2 and "stepId" not in s2:
                        s2["stepId"] = s2.pop("step_id")
                    norm_steps.append(s2)
                else:
                    norm_steps.append(s)
            normalized["steps"] = norm_steps
        # Mapear nombre snake_case a camelCase esperado por from_dict
        if "routine_name" in normalized and "routineName" not in normalized:
            normalized["routineName"] = normalized.get("routine_name")
        return FuzzyRoutine.from_dict(normalized)