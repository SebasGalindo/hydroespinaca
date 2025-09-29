from __future__ import annotations

from typing import Optional

from medyator import Command
from pydantic import BaseModel, Field, PrivateAttr, ConfigDict, field_validator


class DeleteFuzzyTermCommand(BaseModel, Command):
    """Command para eliminar un término difuso por id."""

    model_config = ConfigDict(validate_assignment=True, extra="forbid")

    id: str = Field(..., min_length=1, description="Id del término")
    _result: Optional[bool] = PrivateAttr(default=None)

    @field_validator("id")
    @classmethod
    def _validate_id(cls, v: str) -> str:
        v = (v or "").strip()
        if not v:
            raise ValueError("id es requerido")
        return v
