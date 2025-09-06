from __future__ import annotations

from typing import Iterable, List, TypeVar, Callable

from FuzzyService.Application.Features.FuzzySystems.DTOs.FuzzySystemDto import FuzzySystemDto
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem

TEntity = TypeVar("TEntity")
TDto = TypeVar("TDto")


def map_entity_to_dto(entity: TEntity, mapper: Callable[[TEntity], TDto]) -> TDto:
    """Mapea una entidad de dominio a su DTO usando una función mapper."""
    return mapper(entity)


def map_dto_to_entity(dto: TDto, mapper: Callable[[TDto], TEntity]) -> TEntity:
    """Mapea un DTO a su entidad de dominio usando una función mapper."""
    return mapper(dto)


def map_list(items: Iterable, mapper: Callable) -> List:
    """Mapea una colección aplicando el mapper a cada elemento."""
    return [mapper(i) for i in items]


# ----------------------------- Mappers concretos -----------------------------

def fuzzy_system_to_dto(entity: FuzzySystem) -> FuzzySystemDto:
    return FuzzySystemDto.from_entity(entity)


def fuzzy_system_from_dto(dto: FuzzySystemDto) -> FuzzySystem:
    return dto.to_entity()
