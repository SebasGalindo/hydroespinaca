from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, PrivateAttr
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto


class CloneFuzzySystemCommand(BaseModel, Command):
    """Comando para clonar (deep copy) un sistema difuso completo.
    
    Duplica el sistema con todas sus variables, términos y reglas.
    El sistema clonado se crea con status DRAFT y un nuevo nombre.
    Todas las referencias internas (IDs) se actualizan a los nuevos IDs generados.
    """

    # ID del sistema a clonar (se toma del path en el controlador)
    id: Optional[str] = Field(default=None, min_length=1)

    # Nombre personalizado para el clon (opcional, default "Copia de {original}")
    name: Optional[str] = Field(default=None, max_length=100)

    # Campo result para almacenar el DTO resultante
    _result: Optional[FuzzySystemDto] = PrivateAttr(default=None)
