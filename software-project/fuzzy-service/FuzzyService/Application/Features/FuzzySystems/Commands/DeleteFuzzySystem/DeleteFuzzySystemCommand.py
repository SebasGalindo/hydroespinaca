from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Command


class DeleteFuzzySystemCommand(BaseModel, Command):
    """Comando para eliminar un sistema difuso por su ID."""

    id: str = Field(..., min_length=1)
    
    # Campo result para almacenar el resultado booleano
    _result: Optional[bool] = PrivateAttr(default=None)