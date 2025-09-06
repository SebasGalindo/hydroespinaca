from __future__ import annotations

from typing import List, Optional

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto


class CreateFuzzyVariableCommand(BaseModel, Command):
    """Comando para crear una nueva variable difusa."""

    # Datos requeridos
    name: str = Field(..., min_length=1, max_length=100)
    variable_type: str = Field(..., description="'input' o 'output'")

    # Datos opcionales
    description: Optional[str] = Field(default=None, max_length=500)
    device_id: Optional[str] = Field(default=None)
    terms: List[str] = Field(default_factory=list, description="IDs de términos asociados")

    # Resultado (excluido del modelo de entrada/salida)
    _result: Optional[FuzzyVariableDto] = PrivateAttr(default=None)

    @field_validator("name")
    @classmethod
    def validate_name(cls, v: str) -> str:
        stripped = (v or "").strip()
        if not stripped:
            raise ValueError("El nombre no puede estar vacío")
        return stripped

    @field_validator("variable_type")
    @classmethod
    def validate_variable_type(cls, v: str) -> str:
        if v is None:
            raise ValueError("variable_type es requerido")
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
