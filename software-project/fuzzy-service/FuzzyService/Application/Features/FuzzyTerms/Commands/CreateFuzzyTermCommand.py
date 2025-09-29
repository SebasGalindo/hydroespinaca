from __future__ import annotations

from typing import Optional

from medyator import Command
from pydantic import BaseModel, Field, PrivateAttr, ConfigDict, field_validator

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto, MembershipFunctionDto



class CreateFuzzyTermCommand(BaseModel, Command):
    """Command para crear un término difuso."""

    model_config = ConfigDict(validate_assignment=True, extra="forbid")

    variable_id: str = Field(..., min_length=1, description="ID de la variable a la que pertenece el término")
    label: str = Field(..., min_length=1, max_length=30, description="Etiqueta lingüística del término")
    membership_function: MembershipFunctionDto = Field(..., description="Función de membresía")

    _result: Optional[FuzzyTermDto] = PrivateAttr(default=None)

    @field_validator("variable_id")
    @classmethod
    def _validate_var_id(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("variable_id es requerido")
        return v

    @field_validator("label")
    @classmethod
    def _validate_label(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("label es requerido")
        if len(v) > 30:
            raise ValueError("label no puede exceder 30 caracteres")
        return v
