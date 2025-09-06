from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto


class CreateFuzzySystemCommand(BaseModel, Command):
    """Comando para crear un nuevo sistema difuso."""

    name: str = Field(..., min_length=1, max_length=100)
    isActive: bool = Field(default=False)
    
    # Campo result para almacenar el DTO resultante
    _result: Optional[FuzzySystemDto] = PrivateAttr(default=None)

    @field_validator("name")
    @classmethod
    def validate_name(cls, v: str) -> str:
        """Valida que el nombre no esté vacío después de strip."""
        stripped = v.strip()
        if not stripped:
            raise ValueError("El nombre no puede estar vacío")
        return stripped