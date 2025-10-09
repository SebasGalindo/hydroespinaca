from __future__ import annotations

from typing import Optional, List

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Domain.Enums import FuzzySystemStatus, DefuzzificationMethod
from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig


class UpdateFuzzySystemCommand(BaseModel, Command):
    """Comando para actualizar un sistema difuso existente."""

    id: Optional[str] = Field(default=None, min_length=1)
    name: Optional[str] = Field(default=None, min_length=1, max_length=100)
    status: Optional[FuzzySystemStatus] = Field(default=None)
    defuzzification_method: Optional[DefuzzificationMethod] = Field(default=None)
    operators: Optional[OperatorsConfig] = Field(default=None)
    input_variable_ids: Optional[List[str]] = Field(default=None)
    output_variable_ids: Optional[List[str]] = Field(default=None)
    
    # Campo result para almacenar el DTO resultante
    _result: Optional[FuzzySystemDto] = PrivateAttr(default=None)

    @field_validator("name")
    @classmethod
    def validate_name(cls, v: Optional[str]) -> Optional[str]:
        """Valida que el nombre no esté vacío después de strip si se proporciona."""
        if v is None:
            return v
        stripped = v.strip()
        if not stripped:
            raise ValueError("El nombre no puede estar vacío")
        return stripped