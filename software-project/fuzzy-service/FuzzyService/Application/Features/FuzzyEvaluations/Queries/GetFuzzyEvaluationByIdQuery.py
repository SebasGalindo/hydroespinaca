from __future__ import annotations

from typing import Optional

from pydantic import BaseModel, Field, field_validator

from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto


class GetFuzzyEvaluationByIdQuery(BaseModel):
    """Query para obtener una evaluación fuzzy específica por su ID."""
    
    evaluation_id: str = Field(..., description="ID de la evaluación fuzzy a obtener")
    
    @field_validator('evaluation_id')
    @classmethod
    def validate_evaluation_id(cls, v: str) -> str:
        """Valida que el ID de la evaluación no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID de la evaluación no puede estar vacío')
        return v.strip()


class GetFuzzyEvaluationByIdResponse(BaseModel):
    """Respuesta para la query GetFuzzyEvaluationById."""
    
    evaluation: Optional[FuzzyEvaluationDto] = Field(default=None, description="Evaluación fuzzy encontrada")
    found: bool = Field(default=False, description="Indica si la evaluación fue encontrada")
    
    @classmethod
    def create_found(cls, evaluation: FuzzyEvaluationDto) -> GetFuzzyEvaluationByIdResponse:
        """Crea una respuesta cuando la evaluación es encontrada."""
        return cls(evaluation=evaluation, found=True)
    
    @classmethod
    def create_not_found(cls) -> GetFuzzyEvaluationByIdResponse:
        """Crea una respuesta cuando la evaluación no es encontrada."""
        return cls(evaluation=None, found=False)
