from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto


class ActivateFuzzySystemCommand(BaseModel, Command):
    """Comando para activar un sistema difuso de forma exclusiva.
    
    Desactiva todos los sistemas activos y activa el sistema indicado.
    Valida que el sistema tenga variables de entrada, salida y reglas.
    """

    # ID se toma del path en el controlador
    id: Optional[str] = Field(default=None, min_length=1)

    # Campo result para almacenar el DTO resultante
    _result: Optional[FuzzySystemDto] = PrivateAttr(default=None)
