from __future__ import annotations

from typing import Optional, List, Dict, Any

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Query


class SimulateInput(BaseModel):
    """Entrada de simulación: reference_code del sensor + valor."""
    reference_code: str = Field(..., min_length=1, description="Código de referencia del sensor (ej: T_AMB, H_REL)")
    value: float = Field(..., description="Valor numérico a simular")


class SimulateFuzzySystemQuery(BaseModel, Query):
    """Query para simular una evaluación fuzzy con inputs arbitrarios.
    
    Ejecuta el motor fuzzy sin persistir la evaluación ni enviar comandos
    a los actuadores. Útil para experimentar con diferentes valores de entrada.
    """

    # ID del sistema a simular (se asigna desde el path en el controller)
    id: Optional[str] = Field(default=None)

    # Inputs de simulación
    inputs: List[SimulateInput] = Field(
        ..., min_length=1, description="Lista de entradas con reference_code y value"
    )

    # Campo result para almacenar el resultado de la simulación
    _result: Optional[Dict[str, Any]] = PrivateAttr(default=None)

    @field_validator("inputs")
    @classmethod
    def _validate_unique_inputs(cls, v: List[SimulateInput]) -> List[SimulateInput]:
        codes = [inp.reference_code for inp in v]
        if len(codes) != len(set(codes)):
            raise ValueError("No puede haber reference_codes duplicados en los inputs")
        return v
