"""Application Services Module

Contains domain service implementations that orchestrate between
domain entities and infrastructure components.

Note: Avoid importing submodules here. Import explicitly where needed, e.g.:
from FuzzyService.Application.Services.NullFuzzyEngine import NullFuzzyEngine
"""

from __future__ import annotations

# Do not re-export concrete services to prevent unintended imports
__all__: list[str] = []

# Exponer solo servicios que no arrastran dependencias de infraestructura pesada
from .NullFuzzyEngine import NullFuzzyEngine

__all__ = [
    "NullFuzzyEngine",
]