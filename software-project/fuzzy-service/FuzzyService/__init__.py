# FuzzyService Package
# Main package for fuzzy logic processing service

__version__ = "1.0.0"
__author__ = "HydroEspinaca Team"

# Import main components
from .Infrastructure.FuzzyEngine.ScikitFuzzyEngine import ScikitFuzzyEngine

__all__ = [
    'ScikitFuzzyEngine'
]