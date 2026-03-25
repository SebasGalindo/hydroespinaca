from __future__ import annotations

from typing import Optional

from medyator import Command
from pydantic import Field, ConfigDict, field_validator, PrivateAttr
from pydantic import BaseModel

from FuzzyService.Application.Features.FuzzyTerms.DTOs.FuzzyTermDto import FuzzyTermDto, MembershipFunctionDto


class UpdateFuzzyTermCommand(BaseModel, Command):
    """Command para actualizar parcialmente un término difuso."""

    model_config = ConfigDict(validate_assignment=True, extra="forbid")

    id: Optional[str] = Field(default=None, description="Id del término a actualizar (set from path)")
    label: Optional[str] = Field(default=None, min_length=1, max_length=30)
    membership_function: Optional[MembershipFunctionDto] = Field(default=None)

    _result: Optional[FuzzyTermDto] = PrivateAttr(default=None)

    @field_validator("id")
    @classmethod
    def _validate_id(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return v
        v = v.strip()
        if not v:
            raise ValueError("id es requerido")
        return v

    @field_validator("label")
    @classmethod
    def _validate_label(cls, v: Optional[str]) -> Optional[str]:
        if v is None:
            return v
        v = v.strip()
        if not v:
            raise ValueError("label no puede ser vacío si se proporciona")
        if len(v) > 30:
            raise ValueError("label no puede exceder 30 caracteres")
        return v
