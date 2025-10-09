from __future__ import annotations

from typing import Iterable, List, Optional, Sequence

from FuzzyService.Domain.Errors.DomainErrors import ValidationError


# Mensajes de error estandarizados
ERR_EMPTY = "El valor no puede estar vacío"
ERR_MAX_LEN = "El valor excede la longitud máxima permitida"
ERR_MIN_LEN = "El valor no cumple con la longitud mínima requerida"
ERR_INVALID_CHOICE = "Valor inválido, debe ser uno de: {choices}"
ERR_INVALID_ID = "Identificador inválido"
ERR_INVALID_PAGINATION = "Parámetros de paginación inválidos"


def require_str(value: str, *, field: str, min_len: int = 1, max_len: Optional[int] = None) -> str:
    """Valida que un string requerido no esté vacío y cumpla longitudes.

    - Aplica strip() para evitar espacios en blanco.
    - Lanza ValidationError con mensajes estandarizados.
    """
    if value is None:
        raise ValidationError(f"{field}: {ERR_EMPTY}")
    v = value.strip()
    if len(v) < min_len:
        raise ValidationError(f"{field}: {ERR_MIN_LEN}")
    if max_len is not None and len(v) > max_len:
        raise ValidationError(f"{field}: {ERR_MAX_LEN}")
    return v


def optional_str(value: Optional[str], *, field: str, max_len: Optional[int] = None) -> Optional[str]:
    """Valida un string opcional solo si viene informado."""
    if value is None:
        return None
    v = value.strip()
    if max_len is not None and len(v) > max_len:
        raise ValidationError(f"{field}: {ERR_MAX_LEN}")
    return v


def ensure_in_set(value: str, *, field: str, allowed: Sequence[str]) -> str:
    """Valida que value esté en el conjunto permitido (case-sensitive)."""
    if value not in allowed:
        raise ValidationError(f"{field}: {ERR_INVALID_CHOICE.format(choices=allowed)}")
    return value


def require_id(value: Optional[str], *, field: str) -> str:
    """Valida que un ID requerido esté presente y con formato básico (string no vacío)."""
    if value is None:
        raise ValidationError(f"{field}: {ERR_INVALID_ID}")
    v = value.strip()
    if not v:
        raise ValidationError(f"{field}: {ERR_INVALID_ID}")
    return v


def ensure_ids_list(values: Optional[Iterable[str]], *, field: str) -> List[str]:
    """Valida colección de IDs (strings no vacíos). Devuelve lista normalizada."""
    if values is None:
        return []
    result: List[str] = []
    for idx, raw in enumerate(values):
        if raw is None or not str(raw).strip():
            raise ValidationError(f"{field}[{idx}]: {ERR_INVALID_ID}")
        result.append(str(raw).strip())
    return result


def validate_pagination(skip: Optional[int] = 0, limit: Optional[int] = 50, *, max_limit: int = 200) -> tuple[int, int]:
    """Valida parámetros de paginación y devuelve (skip, limit) saneados."""
    try:
        s = int(skip or 0)
        l = int(limit or 50)
    except Exception as e:
        raise ValidationError(ERR_INVALID_PAGINATION) from e

    if s < 0 or l <= 0 or l > max_limit:
        raise ValidationError(ERR_INVALID_PAGINATION)
    return s, l
