from __future__ import annotations

from typing import Optional, List, Dict, Any

from pydantic import BaseModel, Field, PrivateAttr, field_validator
from medyator import Command

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto


class ImportFuzzySystemCommand(BaseModel, Command):
    """Comando para importar un sistema difuso desde un JSON exportado.
    
    Crea un sistema nuevo con todas sus variables, términos y reglas
    desde el formato portátil generado por el export.
    El sistema importado se crea con status DRAFT.
    """

    version: str = Field(default="1.0")
    exported_at: Optional[str] = None

    system: Dict[str, Any] = Field(
        ..., description="Datos del sistema: name, defuzzification_method, operators"
    )
    variables: List[Dict[str, Any]] = Field(
        default_factory=list, description="Lista de variables con sus propiedades"
    )
    terms: List[Dict[str, Any]] = Field(
        default_factory=list, description="Lista de términos con variable_ref (índice)"
    )
    rules: List[Dict[str, Any]] = Field(
        default_factory=list, description="Lista de reglas con variable_ref y term_refs"
    )

    # Campo result para almacenar el DTO resultante
    _result: Optional[FuzzySystemDto] = PrivateAttr(default=None)

    @field_validator("system")
    @classmethod
    def _validate_system(cls, v: Dict[str, Any]) -> Dict[str, Any]:
        if "name" not in v or not v["name"]:
            raise ValueError("El campo 'system.name' es requerido")
        return v
