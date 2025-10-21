from __future__ import annotations

from typing import Optional, List

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto


class UpdateFuzzyVariableCommand(BaseModel, Command):
    """Comando para actualizar una variable difusa existente."""

    id: str = Field(..., min_length=1)

    # Campos actualizables (opcionales)
    name: Optional[str] = Field(default=None, min_length=1, max_length=100)
    description: Optional[str] = Field(default=None, max_length=500)
    variable_type: Optional[str] = Field(default=None, description="'input' o 'output'")
    reference_code: Optional[str] = Field(default=None, min_length=1, description="Código estable que mapea directamente al código del actuador")
    actuator_type: Optional[str] = Field(default=None, description="Tipo de actuador: 'PWM' o 'DIGITAL'")
    defuzzification_threshold: Optional[float] = Field(default=None, ge=0, le=100, description="Umbral para actuadores DIGITAL")
    universe_min: Optional[float] = Field(default=None, description="Valor mínimo del universo de discurso")
    universe_max: Optional[float] = Field(default=None, description="Valor máximo del universo de discurso")
    terms: Optional[List[str]] = Field(default=None, description="IDs de términos asociados; si se pasa, reemplaza el listado")

    # Resultado
    _result: Optional[FuzzyVariableDto] = PrivateAttr(default=None)

    @field_validator("name")
    @classmethod
    def validate_name(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return None
        stripped = v.strip()
        if not stripped:
            raise ValueError("El nombre no puede estar vacío si se proporciona")
        return stripped

    @field_validator("variable_type")
    @classmethod
    def validate_variable_type(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return None
        v_norm = str(v).strip().lower()
        if v_norm not in {"input", "output"}:
            raise ValueError("variable_type inválido. Debe ser 'input' o 'output'")
        return v_norm

    @field_validator("description")
    @classmethod
    def validate_description(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return None
        if len(v) > 500:
            raise ValueError("La descripción no puede exceder 500 caracteres")
        return v
