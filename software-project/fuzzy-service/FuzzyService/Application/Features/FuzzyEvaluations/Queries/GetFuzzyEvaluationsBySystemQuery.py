from __future__ import annotations

from typing import List, Optional
from datetime import datetime

from pydantic import BaseModel, Field, field_validator

from FuzzyService.Application.Features.FuzzyEvaluations.DTOs.FuzzyEvaluationDto import FuzzyEvaluationDto


class GetFuzzyEvaluationsBySystemQuery(BaseModel):
    """Query para obtener evaluaciones fuzzy de un sistema específico."""
    
    system_id: str = Field(..., description="ID del sistema fuzzy")
    
    # Filtros de fecha
    start_date: Optional[datetime] = Field(default=None, description="Fecha de inicio para filtrar evaluaciones")
    end_date: Optional[datetime] = Field(default=None, description="Fecha de fin para filtrar evaluaciones")
    
    # Paginación
    page: int = Field(default=1, ge=1, description="Número de página (inicia en 1)")
    page_size: int = Field(default=20, ge=1, le=100, description="Tamaño de página (máximo 100)")
    
    # Ordenamiento
    sort_order: str = Field(default="desc", description="Orden de clasificación por timestamp (asc/desc)")
    
    @field_validator('system_id')
    @classmethod
    def validate_system_id(cls, v: str) -> str:
        """Valida que el ID del sistema no esté vacío."""
        if not v or not v.strip():
            raise ValueError('El ID del sistema no puede estar vacío')
        return v.strip()
    
    @field_validator('end_date')
    @classmethod
    def validate_date_range(cls, v: Optional[datetime], info) -> Optional[datetime]:
        """Valida que la fecha de fin sea posterior a la fecha de inicio."""
        if v is not None and 'start_date' in info.data and info.data['start_date'] is not None:
            if v <= info.data['start_date']:
                raise ValueError('La fecha de fin debe ser posterior a la fecha de inicio')
        return v
    
    @field_validator('sort_order')
    @classmethod
    def validate_sort_order(cls, v: str) -> str:
        """Valida que el orden de clasificación sea válido."""
        if v.lower() not in ['asc', 'desc']:
            raise ValueError('El orden de clasificación debe ser "asc" o "desc"')
        return v.lower()
    
    @property
    def skip(self) -> int:
        """Calcula el número de registros a omitir para la paginación."""
        return (self.page - 1) * self.page_size
    
    @property
    def limit(self) -> int:
        """Retorna el límite de registros por página."""
        return self.page_size


class GetFuzzyEvaluationsBySystemResponse(BaseModel):
    """Respuesta para la query GetFuzzyEvaluationsBySystem."""
    
    evaluations: List[FuzzyEvaluationDto] = Field(default_factory=list, description="Lista de evaluaciones del sistema")
    system_id: str = Field(..., description="ID del sistema consultado")
    total_count: int = Field(default=0, description="Número total de evaluaciones del sistema")
    page: int = Field(default=1, description="Página actual")
    page_size: int = Field(default=20, description="Tamaño de página")
    total_pages: int = Field(default=0, description="Número total de páginas")
    has_next: bool = Field(default=False, description="Indica si hay más páginas")
    has_previous: bool = Field(default=False, description="Indica si hay páginas anteriores")
    
    @classmethod
    def create(
        cls,
        evaluations: List[FuzzyEvaluationDto],
        system_id: str,
        total_count: int,
        page: int,
        page_size: int
    ) -> GetFuzzyEvaluationsBySystemResponse:
        """Crea una respuesta con metadatos de paginación calculados."""
        total_pages = (total_count + page_size - 1) // page_size if total_count > 0 else 0
        
        return cls(
            evaluations=evaluations,
            system_id=system_id,
            total_count=total_count,
            page=page,
            page_size=page_size,
            total_pages=total_pages,
            has_next=page < total_pages,
            has_previous=page > 1
        )