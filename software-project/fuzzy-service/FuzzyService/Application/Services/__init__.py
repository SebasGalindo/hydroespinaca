"""Application Services Module

Contains domain services implementations that orchestrate between
domain entities and infrastructure components.
"""

from .FuzzyEngineService import FuzzyEngineService

__all__ = [
    "FuzzyEngineService"
]