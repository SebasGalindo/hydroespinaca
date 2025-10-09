from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Command


class DeleteFuzzyVariableCommand(BaseModel, Command):
    """Comando para eliminar una variable difusa por Id."""

    id: str = Field(..., min_length=1)

    # Resultado de la operación (True si eliminada)
    _result: Optional[bool] = PrivateAttr(default=None)
