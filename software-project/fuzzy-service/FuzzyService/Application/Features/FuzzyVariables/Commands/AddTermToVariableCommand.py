from __future__ import annotations

from typing import Optional

from medyator import Command
from pydantic import BaseModel, Field, PrivateAttr, ConfigDict, field_validator

from FuzzyService.Application.Features.FuzzyVariables.DTOs.FuzzyVariableDto import FuzzyVariableDto


class AddTermToVariableCommand(BaseModel, Command):
    """Command para agregar un término a una variable difusa."""

    model_config = ConfigDict(validate_assignment=True, extra="forbid")

    variable_id: str = Field(..., min_length=1, description="ID de la variable")
    term_id: str = Field(..., min_length=1, description="ID del término a agregar")

    _result: Optional[FuzzyVariableDto] = PrivateAttr(default=None)

    @field_validator("variable_id")
    @classmethod
    def _validate_variable_id(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("variable_id es requerido")
        return v

    @field_validator("term_id")
    @classmethod
    def _validate_term_id(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("term_id es requerido")
        return v