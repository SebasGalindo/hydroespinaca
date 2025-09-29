from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Domain.Enums import FuzzySystemStatus


class UpdateFuzzySystemStatusCommand(BaseModel, Command):
    """Comando para actualizar únicamente el estado de un sistema difuso."""

    # ID se toma del path en el controlador; en el body no es requerido
    id: Optional[str] = Field(default=None, min_length=1)
    status: FuzzySystemStatus
    
    # Campo result para almacenar el DTO resultante
    _result: Optional[bool] = PrivateAttr(default=None)