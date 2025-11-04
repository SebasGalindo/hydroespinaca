"""Seed data configuration for the Fuzzy System.
This module contains all the initial data needed to bootstrap the fuzzy system.
"""

from __future__ import annotations

import logging
import os
from typing import Dict, List, Any, Optional

from FuzzyService.Domain.ValueObjects.DomainId import (
    FuzzySystemId,
    FuzzyVariableId,
    FuzzyRuleId,
    FuzzyTermId
)
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType

# Import interfaces for DI access
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository

_logger = logging.getLogger(__name__)


# Variable names constants (Sonar: avoid duplicated string literals)
class VariableNames:
    """Centralized variable name constants for seed data."""
    AMBIENT_TEMPERATURE = "Ambient Temperature"
    WATER_TEMPERATURE = "Water Temperature"
    WATER_LEVEL = "Water Level"
    NIVEL_OPTIMO = "NivelÓptimo"


# Duration parameters constants (shared across all duration variables)
class DurationParameters:
    """Shared duration parameters for all actuators."""
    # Common duration ranges (in seconds)
    UNIVERSE_MIN = 0.0
    UNIVERSE_MAX = 480.0  # 8 minutes maximum

    # Duration terms - shared parameters for all duration variables
    CORTA = [180.0, 210.0, 270.0]      # Short duration: ~3-4 minutes
    MEDIA = [240.0, 300.0, 360.0]     # Medium duration: ~4-6 minutes
    LARGA = [330.0, 420.0, 480.0]  # Long duration: ~5-8 minutes

    # OFF term for duration variables
    OFF = [0.0, 0.0, 1.0]
    OFF_UNIVERSE_MIN = 0.0
    OFF_UNIVERSE_MAX = 100.0


class SeedDataConfig:
    """Configuration class for seed data with predefined IDs."""

    # Duration variable names (for reference)
    DURATION_VARIABLES = [
        "Duración de Ventilación",
        "Duración de Calefacción de Aire",
        "Duración de Luz",
        "Duración de Aireación",
        "Duración de Riego",
        "Duración de Calefacción de Agua",
        "Duración de Humidificación"
    ]

    @staticmethod
    def _create_duration_terms(variable_name: str) -> List[Dict[str, Any]]:
        """
        Create standardized duration terms for a given duration variable.

        All duration variables share the same term parameters defined in DurationParameters.
        This ensures consistency across all actuators.

        Args:
            variable_name: The name of the duration variable (e.g., "Duración de Ventilación")

        Returns:
            List of term configurations with OFF, duracionCorta, duracionMedia, duracionLarga
        """
        return [
            # OFF term for this duration variable
            {
                "variable_ref": variable_name,  # Reference by name, not ID
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": DurationParameters.OFF,
                    "universe_min": DurationParameters.OFF_UNIVERSE_MIN,
                    "universe_max": DurationParameters.OFF_UNIVERSE_MAX
                }
            },
            # Short duration
            {
                "variable_ref": variable_name,
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": DurationParameters.CORTA,
                    "universe_min": DurationParameters.UNIVERSE_MIN,
                    "universe_max": DurationParameters.UNIVERSE_MAX
                }
            },
            # Medium duration
            {
                "variable_ref": variable_name,
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": DurationParameters.MEDIA,
                    "universe_min": DurationParameters.UNIVERSE_MIN,
                    "universe_max": DurationParameters.UNIVERSE_MAX
                }
            },
            # Long duration
            {
                "variable_ref": variable_name,
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": DurationParameters.LARGA,
                    "universe_min": DurationParameters.UNIVERSE_MIN,
                    "universe_max": DurationParameters.UNIVERSE_MAX
                }
            }
        ]

    @staticmethod
    def _get_all_duration_terms() -> List[Dict[str, Any]]:
        """
        Generate all duration terms for all duration variables.

        This method replaces hundreds of lines of duplicated code with a single loop.
        All duration variables share the same term structure and parameters.
        """
        all_terms = []
        for duration_var_name in SeedDataConfig.DURATION_VARIABLES:
            all_terms.extend(SeedDataConfig._create_duration_terms(duration_var_name))
        return all_terms

    @staticmethod
    def get_system_config() -> Dict[str, Any]:
        """Get fuzzy system configuration."""
        return {
            "name": os.getenv("FUZZY_SEED_SYSTEM_NAME", "Hydroponic Control System"),
            "description": "Sistema de control difuso para invernadero hidropónico"
        }

    @staticmethod
    def get_variables_config() -> List[Dict[str, Any]]:
        """Get all variables configuration (input and output)."""
        return [
            # INPUT VARIABLES
            {
                "name": "Luminosity",
                "description": "Luminosidad medida por sensor BH1750 (lux)",
                "type": "input",
                "reference_code": "LUMINOSITY",
                "defuzzification_threshold": 50,
                "terms": []  # Se llenarán con IDs de términos creados
            },
            {
                "name": VariableNames.AMBIENT_TEMPERATURE,
                "description": "Temperatura ambiente del invernadero",
                "type": "input",
                "reference_code": "T_AMB",
                "terms": []
            },
            {
                "name": "Humidity",
                "description": "Humedad relativa del ambiente",
                "type": "input",
                "reference_code": "HUM",
                "terms": []
            },
            {
                "name": VariableNames.WATER_TEMPERATURE,
                "description": "Temperatura del agua del sistema",
                "type": "input",
                "reference_code": "T_WAT",
                "terms": []
            },
            {
                "name": VariableNames.WATER_LEVEL,
                "description": "Nivel de agua en el reservorio (seguridad crítica)",
                "type": "input",
                "reference_code": "WL",
                "terms": []
            },
            # OUTPUT VARIABLES
            {
                "name": "Potencia del Ventilador",
                "description": "Control de potencia del ventilador (PWM)",
                "type": "output",
                "actuator_type": "PWM",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,  # No aplica para PWM, pero se incluye por consistencia
                "reference_code": "Ventiladores",
                "terms": []
            },
            # VARIABLES DE CONTROL (DIGITAL)
            {
                "name": "Control Calefactor Aire",
                "description": "Control ON/OFF del calefactor de aire",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,  # OFF si <50, ON si >=50
                "reference_code": "termoventilador",
                "terms": []
            },
            {
                "name": "Control Calefactor Agua",
                "description": "Control ON/OFF del calefactor de agua",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "calefactor-agua",
                "terms": []
            },
            {
                "name": "Control Luz",
                "description": "Control ON/OFF de la luz de amplio espectro",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "luz-amplio-espectro",
                "terms": []
            },
            {
                "name": "Control Humidificador",
                "description": "Control ON/OFF del humidificador",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "humidificador-ultrasonico",
                "terms": []
            },
            {
                "name": "Control Bomba Aireación",
                "description": "Control ON/OFF de la bomba de aireación",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "piedra-difusora",
                "terms": []
            },
            {
                "name": "Control Bomba Riego",
                "description": "Control ON/OFF de la bomba de riego",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 100.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "bomba-agua",
                "terms": []
            },
            # VARIABLES DE DURACIÓN (DIGITAL)
            {
                "name": "Duración de Ventilación",
                "description": "Duración de activación del ventilador (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,  # 1 hora máximo
                "defuzzification_threshold": 50.0,
                "reference_code": "Ventiladores",
                "terms": []
            },
            {
                "name": "Duración de Calefacción de Aire",
                "description": "Duración de calefacción del aire (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "termoventilador",
                "terms": []
            },
            {
                "name": "Duración de Luz",
                "description": "Duración de luz artificial (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "luz-amplio-espectro",
                "terms": []
            },
            {
                "name": "Duración de Aireación",
                "description": "Duración de aireación del agua (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "piedra-difusora",
                "terms": []
            },
            {
                "name": "Duración de Riego",
                "description": "Duración del riego (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "bomba-agua",
                "terms": []
            },
            {
                "name": "Duración de Calefacción de Agua",
                "description": "Duración de calefacción del agua (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "calefactor-agua",
                "terms": []
            },
            {
                "name": "Duración de Humidificación",
                "description": "Duración de humidificación (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "universe_min": 0.0,
                "universe_max": 3600.0,
                "defuzzification_threshold": 50.0,
                "reference_code": "humidificador-ultrasonico",
                "terms": []
            }
        ]

    @staticmethod
    def get_terms_config() -> List[Dict[str, Any]]:
        """
        Get all fuzzy terms configuration with membership functions.

        IMPORTANT: This method returns term configurations with 'variable_ref' instead of
        'variable_id'. The actual variable IDs are assigned during the seeding process in
        _create_variables_and_terms(), which maps variable names to their MongoDB IDs.

        This approach ensures:
        1. No hardcoded MongoDB IDs in the configuration
        2. Terms can be created after variables exist
        3. Variable IDs can change between deployments
        """
        return [
            # TÉRMINOS PARA: Luminosity (BH1750 sensor)
            # Rangos basados en valores reales de lux (0-65535)
            # Umbrales: optimalMin=10000, optimalMax=12000
            {
                "variable_ref": "Luminosity",
                "label": "bajaLuminosidad",
                "membership_function": {
                    "function_type": "trapezoidal",
                    "parameters": [0.0, 0.0, 5000.0, 10000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {                "variable_ref": "Luminosity",
                "label": "luminosidadNormal",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [9000.0, 11000.0, 13000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {                "variable_ref": "Luminosity",
                "label": "altaLuminosidad",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [12000.0, 38767.5, 65535.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            # TÉRMINOS PARA: Ambient Temperature (68e05364d86d6edc39982871)
            {                "variable_ref": "Ambient Temperature",
                "label": "frioAmbiental",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [-10.0, 4.0, 18.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            {                "variable_ref": "Ambient Temperature",
                "label": "temperaturaOptima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [15.0, 20.0, 25.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            {                "variable_ref": "Ambient Temperature",
                "label": "calorAmbiental",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [25.0, 32.5, 40.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            # TÉRMINOS PARA: Humidity (68e05364d86d6edc39982872)
            {                "variable_ref": "Humidity",
                "label": "ambienteSeco",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 30.0, 70.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Humidity",
                "label": "humedadNormal",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [60, 75, 85],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # TÉRMINOS PARA: Water Temperature (68e05364d86d6edc39982873)
            {                "variable_ref": "Water Temperature",
                "label": "aguaFria",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [-10.0, 10.0, 17.5],  # Ajustado: peak en 10°C, cae a 0 en 17.5°C
                    "universe_min": -10.0,
                    "universe_max": 85.0
                }
            },
            {                "variable_ref": "Water Temperature",
                "label": "temperaturaAguaOptima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [17.0, 20.0, 23.0],  # Ajustado: empieza en 17°C, peak en 20°C
                    "universe_min": -10.0,
                    "universe_max": 85.0
                }
            },
            # TÉRMINOS PARA: Water Level (68e05364d86d6edc398828D8)
            {                "variable_ref": "Water Level",
                "label": VariableNames.NIVEL_OPTIMO,
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 3.0, 6.0],
                    "universe_min": 0.0,
                    "universe_max": 10.0
                }
            },
            {                "variable_ref": "Water Level",
                "label": "NivelCrítico",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [6.0, 8.5, 10.0],
                    "universe_min": 0.0,
                    "universe_max": 10.0
                }
            },
            # TÉRMINOS PARA: Potencia del Ventilador (68e05364d86d6edc39982875) - PWM
            # Términos PWM para control de potencia variable
            {                "variable_ref": "Potencia del Ventilador",
                "label": "potenciaBaja",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 22.5, 40.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Potencia del Ventilador",
                "label": "potenciaMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [30.0, 50.0, 70.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Potencia del Ventilador",
                "label": "potenciaAlta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [60.0, 75.0, 90.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Potencia del Ventilador",
                "label": "potenciaMaxima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [85.0, 92.5, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # TÉRMINOS PARA DURACIONES - GENERADOS DINÁMICAMENTE
            # Todos los actuadores comparten los mismos parámetros de duración
            # definidos en DurationParameters para garantizar consistencia
        ] + SeedDataConfig._get_all_duration_terms() + [
            # TÉRMINOS ON/OFF PARA VARIABLES DE CONTROL
            # Control Calefactor Aire (68e05364d86d6edc398828B0)
            {                "variable_ref": "Control Calefactor Aire",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Calefactor Aire",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Calefactor Agua (68e05364d86d6edc398828B1)
            {                "variable_ref": "Control Calefactor Agua",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Calefactor Agua",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Luz (68e05364d86d6edc398828B2)
            {                "variable_ref": "Control Luz",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Luz",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Humidificador (68e05364d86d6edc398828B3)
            {                "variable_ref": "Control Humidificador",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Humidificador",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Bomba Aireación (68e05364d86d6edc398828B4)
            {                "variable_ref": "Control Bomba Aireación",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Bomba Aireación",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Bomba Riego (68e05364d86d6edc398828B5)
            {                "variable_ref": "Control Bomba Riego",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {                "variable_ref": "Control Bomba Riego",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            }
        ]

   
    @staticmethod
    def get_rules_config() -> List[Dict[str, Any]]:
        """Get fuzzy rules configuration with predefined IDs."""
        return [
            {                "name": "Regla 1: Temperatura muy fria",
                "description": "Si temperatura ambiente es muy fría, encender calefactor de aire con duración media",
                "conditions": [
                    {
                        "variable_ref": "Ambient Temperature",  # Ambient Temperature
                        "variable_name": VariableNames.AMBIENT_TEMPERATURE,
                        "operator": "IS",
                        "value": "frioAmbiental"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Calefactor Aire",  # Control Calefactor Aire
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Calefacción de Aire",  # Duración de Calefacción de Aire
                        "terms": ["duracionMedia"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 2: Temperatura normal o mas alta",
                "description": "Si temperatura ambiente es normal u óptima, apagar calefactor de aire con duración corta",
                "conditions": [
                    {
                        "variable_ref": "Ambient Temperature",  # Ambient Temperature
                        "variable_name": VariableNames.AMBIENT_TEMPERATURE,
                        "operator": "IS",
                        "value": "temperaturaOptima"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Calefactor Aire",  # Control Calefactor Aire
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Calefacción de Aire",  # Duración de Calefacción de Aire
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 3A: Emergencia Térmica - Control de Aire",
                "description": "Si temperatura ambiente es muy alta, activar ventilador (no depende del nivel de agua)",
                "conditions": [
                    {
                        "variable_ref": "Ambient Temperature",  # Ambient Temperature
                        "variable_name": VariableNames.AMBIENT_TEMPERATURE,
                        "operator": "IS",
                        "value": "calorAmbiental"
                    }
                ],
                "connectors": [],
                "consequents": [
                    # Ventilador - Encender (Potencia Alta, Duración Media)
                    {
                        "variable_ref": "Potencia del Ventilador",  # Potencia del Ventilador (PWM)
                        "terms": ["potenciaAlta"],  # potenciaAlta
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Ventilación",  # Duración de Ventilación
                        "terms": ["duracionMedia"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 3B: Emergencia Térmica - Activación de Recirculación Segura",
                "description": "Si la temperatura ambiente es muy alta Y el nivel de agua es seguro, activar bombas para recirculación de emergencia",
                "conditions": [
                    {
                        "variable_ref": "Ambient Temperature",  # Ambient Temperature
                        "variable_name": VariableNames.AMBIENT_TEMPERATURE,
                        "operator": "IS",
                        "value": "calorAmbiental"
                    },
                    {
                        "variable_ref": "Water Level",  # Water Level
                        "variable_name": VariableNames.WATER_LEVEL,
                        "operator": "IS",
                        "value": VariableNames.NIVEL_OPTIMO
                    }
                ],
                "connectors": ["AND"],
                "consequents": [
                    # Bomba Aireación - Encender
                    {
                        "variable_ref": "Control Bomba Aireación",  # Control Bomba Aireación
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Aireación",  # Duración de Aireación
                        "terms": ["duracionLarga"],  # duracionLarga
                        "aggregation_method": "max"
                    },
                    # Bomba Riego - Encender
                    {
                        "variable_ref": "Control Bomba Riego",  # Control Bomba Riego
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Riego",  # Duración de Riego
                        "terms": ["duracionLarga"],  # duracionLarga
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 4: Humedad muy baja",
                "description": "Si humedad ambiente es muy baja, encender humidificador con duración media",
                "conditions": [
                    {
                        "variable_ref": "Humidity",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "ambienteSeco"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Humidificador",  # Control Humidificador
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Humidificación",  # Duración de Humidificación
                        "terms": ["duracionMedia"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 5: Humedad normal o mas alta",
                "description": "Si humedad ambiente es normal o alta, apagar humidificador con duración corta",
                "conditions": [
                    {
                        "variable_ref": "Humidity",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "humedadNormal"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Humidificador",  # Control Humidificador
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Humidificación",  # Duración de Humidificación
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 6: Temperatura de agua fria",
                "description": "Si temperatura del agua es muy fría Y el nivel de agua es seguro, encender calefactor de agua con duración media",
                "conditions": [
                    {
                        "variable_ref": "Water Temperature",  # Water Temperature
                        "variable_name": VariableNames.WATER_TEMPERATURE,
                        "operator": "IS",
                        "value": "aguaFria"
                    },
                    {
                        "variable_ref": "Water Level",  # Water Level
                        "variable_name": VariableNames.WATER_LEVEL,
                        "operator": "IS",
                        "value": VariableNames.NIVEL_OPTIMO
                    }
                ],
                "connectors": ["AND"],
                "consequents": [
                    {
                        "variable_ref": "Control Calefactor Agua",  # Control Calefactor Agua
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Calefacción de Agua",  # Duración de Calefacción de Agua
                        "terms": ["duracionMedia"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 7: Temperatura de agua normal o mas alta",
                "description": "Si temperatura del agua es normal u óptima, apagar calefactor de agua con duración corta",
                "conditions": [
                    {
                        "variable_ref": "Water Temperature",  # Water Temperature
                        "variable_name": VariableNames.WATER_TEMPERATURE,
                        "operator": "IS",
                        "value": "temperaturaAguaOptima"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Calefactor Agua",  # Control Calefactor Agua
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Calefacción de Agua",  # Duración de Calefacción de Agua
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 8: Nivel de luz bajo",
                "description": "Si la luminosidad (BH1750) es baja (< 10000 lux), encender luz artificial con duración media",
                "conditions": [
                    {
                        "variable_ref": "Luminosity",  # Luminosity
                        "variable_name": "Luminosity",
                        "operator": "IS",
                        "value": "bajaLuminosidad"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Luz",  # Control Luz
                        "terms": ["ON"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Luz",  # Duración de Luz
                        "terms": ["duracionMedia"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 9: Nivel de luz optimo",
                "description": "Si la luminosidad (BH1750) es normal/óptima, apagar luz artificial con duración corta",
                "conditions": [
                    {
                        "variable_ref": "Luminosity",  # Luminosity
                        "variable_name": "Luminosity",
                        "operator": "IS",
                        "value": "luminosidadNormal"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_ref": "Control Luz",  # Control Luz
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Luz",  # Duración de Luz
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {                "name": "Regla 10: Nivel Crítico - Bloqueo de Actuadores de Agua",
                "description": "Si el Nivel de Agua es Crítico, forzar el apagado de la Bomba de Agua, Calefactor de Agua, Bomba de Aireación y Humidificador para proteger el hardware",
                "conditions": [
                    {
                        "variable_ref": "Water Level",  # Water Level
                        "variable_name": VariableNames.WATER_LEVEL,
                        "operator": "IS",
                        "value": "NivelCrítico"
                    }
                ],
                "connectors": [],
                "consequents": [
                    # 1. Calefactor Agua = OFF
                    {
                        "variable_ref": "Control Calefactor Agua",  # Control Calefactor Agua
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Calefacción de Agua",  # Duración de Calefacción de Agua
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 2. Bomba Riego = OFF
                    {
                        "variable_ref": "Control Bomba Riego",  # Control Bomba Riego
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Riego",  # Duración de Riego
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 3. Bomba Aireación = OFF
                    {
                        "variable_ref": "Control Bomba Aireación",  # Control Bomba Aireación
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Aireación",  # Duración de Aireación
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 4. Humidificador = OFF
                    {
                        "variable_ref": "Control Humidificador",  # Control Humidificador
                        "terms": ["OFF"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_ref": "Duración de Humidificación",  # Duración de Humidificación
                        "terms": ["duracionCorta"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            }
        ]


async def seed_fuzzy_system_data(di) -> None:
    """
    Seed initial data for the fuzzy system using predefined IDs.

    Args:
        di: Dependency injection container
    """
    _logger.info("Starting fuzzy system data seeding with predefined IDs...")

    # Get repositories
    repo_system = di[IFuzzySystemRepository]
    repo_variable = di[IFuzzyVariableRepository]
    repo_term = di[IFuzzyTermRepository]
    repo_rule = di[IFuzzyRuleRepository]

    if not all([repo_system, repo_variable, repo_term, repo_rule]):
        _logger.warning("Some repositories not available; skipping seed data creation.")
        return

    # Create fuzzy system
    system = await _create_fuzzy_system(repo_system)
    if not system:
        return

    # Create variables and terms with predefined IDs
    variables_map, created_term_ids, created_variable_ids = await _create_variables_and_terms(repo_variable, repo_term)

    # Create rules (now uses direct consequents instead of routines)
    await _create_rules(repo_rule, system, variables_map, created_term_ids, created_variable_ids, repo_term)

    # Update system relationships
    await _update_system_relationships(repo_system, system, variables_map, repo_rule)

    _logger.info("Fuzzy system data seeding completed successfully.")


async def _create_fuzzy_system(repo_system):
    """Create the main fuzzy system."""
    from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
    from FuzzyService.Domain.Enums.EntityStatus import FuzzySystemStatus
    from FuzzyService.Domain.Enums.DefuzzificationMethod import DefuzzificationMethod
    from FuzzyService.Domain.ValueObjects.OperatorsConfig import OperatorsConfig

    system_name = "Hydroponic Control System"
    existing_system = await repo_system.get_by_name(system_name)  # type: ignore[attr-defined]

    if not existing_system:
        system = await repo_system.create(  # type: ignore[attr-defined]
            FuzzySystem(
                name=system_name,
                status=FuzzySystemStatus.ACTIVE,
                defuzzification_method=DefuzzificationMethod.CENTROID,
                operators=OperatorsConfig(),
                input_variable_ids=[],
                output_variable_ids=[],
                rule_ids=[]
            )
        )
        _logger.info("Seed: created fuzzy system '%s' (id=%s)", system_name, str(system.id))
        return system
    else:
        _logger.info("Seed: fuzzy system '%s' already exists (id=%s)", system_name, str(existing_system.id))
        return existing_system


async def _create_variables_and_terms(repo_variable, repo_term):
    """Create variables and their terms in correct order: variables first, then terms.

    Flow:
    1. Create all variables (without terms in the list)
    2. Create all terms (now variables exist)
    3. Update variables with term references
    """
    from FuzzyService.Domain.Entities.fuzzy_variable import FuzzyVariable
    from FuzzyService.Domain.Entities.fuzzy_term import FuzzyTerm

    variables_map = {}
    created_variable_ids = {}  # Map: variable_name -> actual_mongodb_id
    created_term_ids = {}  # Map: composite_key (variable_name:term_label) -> actual_mongodb_id

    # STEP 1: Create all variables first (with empty terms list)
    _logger.info("STEP 1: Creating fuzzy variables (without terms)...")
    for var_config in SeedDataConfig.get_variables_config():
        var_name = var_config["name"]

        # Check if variable already exists by name
        existing_var = await repo_variable.get_by_name(var_name)  # type: ignore[attr-defined]

        if not existing_var:
            # Create variable WITHOUT terms initially
            variable = FuzzyVariable(
                name=var_config["name"],
                description=var_config.get("description", ""),
                variable_type=var_config["type"],
                actuator_type=var_config.get("actuator_type"),  # Solo para outputs
                universe_min=var_config.get("universe_min"),
                universe_max=var_config.get("universe_max"),
                defuzzification_threshold=var_config.get("defuzzification_threshold", 50.0),
                reference_code=var_config.get("reference_code"),
                terms=[]  # Empty initially
            )

            created_var = await repo_variable.create(variable)  # type: ignore[attr-defined]
            variables_map[var_name] = created_var

            # Map variable name to actual MongoDB ID
            created_variable_ids[var_name] = str(created_var.id)

            _logger.info(
                "Seed: created variable '%s' (reference_code=%s, actual_id=%s)",
                var_config["name"],
                var_config.get("reference_code", "N/A"),
                str(created_var.id)
            )
        else:
            # Variable exists, but update fields if they're missing or different
            updated = False

            # Update actuator_type if missing or different
            if existing_var.variable_type == "output":
                new_actuator_type = var_config.get("actuator_type")
                if existing_var.actuator_type != new_actuator_type:
                    existing_var.actuator_type = new_actuator_type
                    updated = True

                # Update universe_min if missing or different
                new_universe_min = var_config.get("universe_min")
                if existing_var.universe_min != new_universe_min:
                    existing_var.universe_min = new_universe_min
                    updated = True

                # Update universe_max if missing or different
                new_universe_max = var_config.get("universe_max")
                if existing_var.universe_max != new_universe_max:
                    existing_var.universe_max = new_universe_max
                    updated = True

                # Update defuzzification_threshold if different
                new_threshold = var_config.get("defuzzification_threshold", 50.0)
                if existing_var.defuzzification_threshold != new_threshold:
                    existing_var.defuzzification_threshold = new_threshold
                    updated = True

            # Save updates if any field changed
            if updated:
                existing_var = await repo_variable.update(existing_var)  # type: ignore[attr-defined]
                _logger.info(
                    "Seed: updated existing variable '%s' with new fields (id=%s)",
                    var_name,
                    str(existing_var.id)
                )
            else:
                _logger.info(
                    "Seed: variable '%s' already exists and is up-to-date (id=%s)",
                    var_name,
                    str(existing_var.id)
                )

            variables_map[var_name] = existing_var

            # Map variable name to actual MongoDB ID
            created_variable_ids[var_name] = str(existing_var.id)

    # STEP 2: Create all terms using actual variable IDs
    _logger.info("STEP 2: Creating fuzzy terms (using variable references)...")
    terms_by_variable = {}  # Map: variable_id -> list of term_ids

    for term_config in SeedDataConfig.get_terms_config():
        # Use variable_ref (name) instead of hardcoded variable_id
        variable_name = term_config.get("variable_ref")

        if not variable_name:
            _logger.warning(
                f"Skipping term '{term_config.get('label', 'UNKNOWN')}': missing 'variable_ref'"
            )
            continue

        # Get the variable from variables_map by name
        variable = variables_map.get(variable_name)
        if not variable:
            _logger.warning(
                f"Skipping term '{term_config['label']}': variable '{variable_name}' not found in variables_map"
            )
            continue

        variable_id = FuzzyVariableId(str(variable.id))
        term_label = term_config["label"]

        # Check if term already exists for this variable
        existing_term = await repo_term.get_by_label(variable_id, term_label)  # type: ignore[attr-defined]

        if not existing_term:
            # Create membership function
            mf_config = term_config["membership_function"]
            membership_function = MembershipFunction(
                function_type=MembershipFunctionType(mf_config["function_type"]),
                parameters=mf_config["parameters"],
                universe_min=mf_config["universe_min"],
                universe_max=mf_config["universe_max"]
            )

            # Create term with actual variable ID
            term = FuzzyTerm(
                label=term_config["label"],
                variable_id=variable_id,
                membership_function=membership_function
            )

            created_term = await repo_term.create(term)  # type: ignore[attr-defined]
            actual_term_id = str(created_term.id)

            # Track terms by variable using a composite key: variable_name + term_label
            # This allows rules to reference terms by variable name + term label
            term_key = f"{variable_name}:{term_label}"
            created_term_ids[term_key] = actual_term_id

            # Also track by variable ID for backward compatibility
            var_id_str = str(variable.id)
            if var_id_str not in terms_by_variable:
                terms_by_variable[var_id_str] = []
            terms_by_variable[var_id_str].append(FuzzyTermId(actual_term_id))

            _logger.info(
                "Seed: created term '%s' for variable '%s' (actual_id=%s)",
                term_config["label"],
                variable_name,
                actual_term_id
            )
        else:
            actual_term_id = str(existing_term.id)

            # Save mapping for existing terms
            term_key = f"{variable_name}:{term_label}"
            created_term_ids[term_key] = actual_term_id

            # Track existing terms
            var_id_str = str(variable.id)
            if var_id_str not in terms_by_variable:
                terms_by_variable[var_id_str] = []
            terms_by_variable[var_id_str].append(FuzzyTermId(actual_term_id))

            _logger.info(
                "Seed: term '%s' already exists for variable '%s' (actual_id=%s)",
                term_config["label"],
                variable_name,
                actual_term_id
            )

    # STEP 3: Update variables with term references
    _logger.info("STEP 3: Updating variables with term references...")
    for var_name, variable in variables_map.items():
        var_id_str = str(variable.id)
        term_ids = terms_by_variable.get(var_id_str, [])

        if term_ids:
            # Update variable with term IDs
            variable.terms = term_ids
            await repo_variable.update(variable)  # type: ignore[attr-defined]

            _logger.info(
                "Seed: updated variable '%s' with %d terms",
                var_name,
                len(term_ids)
            )

    return variables_map, created_term_ids, created_variable_ids


async def _create_consequents_from_config(rule_config, created_term_ids, created_variable_ids, repo_term):
    """
    Create RuleConsequent objects directly from rule configuration.

    This replaces the old routine-based approach with direct Mamdani consequents.
    The configuration now specifies consequents directly instead of routing through
    intermediate routines.

    Expected config format:
    {
        "consequents": [
            {
                "variable_ref": "Control Calefactor Aire",  # Predefined variable output ID
                "terms": ["ON"],  # List of predefined term IDs for this variable
                "aggregation_method": "max"
            }
        ]
    }

    Args:
        rule_config: Configuration dict for the rule
        created_term_ids: Map of composite keys (variable_name:term_label) to actual MongoDB IDs
        created_variable_ids: Map of variable names to actual MongoDB IDs
        repo_term: Repository to fetch terms
    """
    from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
    from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId

    # Get consequents from config
    consequents_config = rule_config.get("consequents", [])

    if consequents_config:
        consequents = []
        for cons_config in consequents_config:
            # Get variable name from variable_ref
            variable_name = cons_config.get("variable_ref")

            if not variable_name:
                _logger.warning(
                    f"Skipping consequent: missing 'variable_ref'. Config: {cons_config}"
                )
                continue

            # Get term labels from config
            # Rules now use term labels (e.g., ["ON", "duracionMedia"]) instead of hardcoded IDs
            term_labels = cons_config.get("terms", [])

            # Map term labels to actual MongoDB IDs using composite key: "variable_name:term_label"
            actual_term_ids = []
            for term_label in term_labels:
                # Create composite key
                term_key = f"{variable_name}:{term_label}"
                actual_id = created_term_ids.get(term_key)

                if actual_id:
                    actual_term_ids.append(FuzzyTermId(actual_id))
                    _logger.debug(f"Mapped term: '{term_key}' -> {actual_id}")
                else:
                    _logger.warning(
                        f"Term mapping not found for '{term_key}'. "
                        f"Variable: '{variable_name}', Label: '{term_label}'. "
                        f"Available term keys: {[k for k in created_term_ids.keys() if ':' in k][:5]}..."
                    )

            if not actual_term_ids:
                _logger.warning(
                    f"No actual term IDs found for consequent. "
                    f"Variable: '{variable_name}', Term labels: {term_labels}"
                )
                continue

            # Map variable name to actual MongoDB ID
            actual_variable_id = created_variable_ids.get(variable_name)
            if actual_variable_id:
                variable_id = FuzzyVariableId(actual_variable_id)
                _logger.debug(
                    f"Mapped variable: name='{variable_name}' -> id={actual_variable_id}"
                )
            else:
                # Variable not found in mapping
                _logger.error(
                    f"Variable '{variable_name}' not found in created_variable_ids mapping. "
                    f"Available variables: {list(created_variable_ids.keys())}. "
                    f"Skipping this consequent."
                )
                continue

            consequent = RuleConsequent(
                variable_id=variable_id,
                terms=actual_term_ids,
                aggregation_method=cons_config.get("aggregation_method", "max")
            )
            consequents.append(consequent)

        return consequents

    # Si no hay consecuentes definidos, retornar lista vacía
    return []


async def _create_rules(repo_rule, system, variables_map, created_term_ids, created_variable_ids, repo_term):
    """Create fuzzy rules with direct Mamdani consequents."""
    from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule
    from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent

    _logger.info(f"Creating rules with {len(created_term_ids)} term ID mappings and {len(created_variable_ids)} variable ID mappings available")

    for rule_config in SeedDataConfig.get_rules_config():
        rule_name = rule_config["name"]

        existing_rule = await repo_rule.get_by_name(FuzzySystemId(str(system.id)), rule_name)  # type: ignore[attr-defined]

        if not existing_rule:
            # Build conditions using variable_name from config
            conditions = []
            for condition_config in rule_config["conditions"]:
                variable_name = condition_config["variable_name"]
                variable = variables_map.get(variable_name)

                if not variable:
                    _logger.warning(
                        f"Skipping condition in rule '{rule_name}': variable '{variable_name}' not found"
                    )
                    continue

                condition = {
                    "variableId": FuzzyVariableId(str(variable.id)),
                    "operator": condition_config["operator"],
                    "value": condition_config["value"]
                }
                conditions.append(condition)

            # Build consequents directly from config (Mamdani model)
            _logger.debug(f"Creating consequents for rule '{rule_name}'...")
            consequents = await _create_consequents_from_config(
                rule_config, created_term_ids, created_variable_ids, repo_term
            )

            if not consequents:
                _logger.warning(
                    f"Skipping rule '{rule_name}': could not create consequents. "
                    f"Rule config consequents: {rule_config.get('consequents', [])}"
                )
                continue

            _logger.info(f"Created {len(consequents)} consequents for rule '{rule_name}'")

            if conditions:
                rule = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name,
                        system_id=FuzzySystemId(str(system.id)),
                        description=rule_config.get("description", ""),
                        conditions=conditions,
                        connectors=rule_config["connectors"],
                        consequents=consequents,
                    )
                )
                _logger.info(
                    "Seed: created rule '%s' with %d conditions and %d consequents (id=%s)",
                    rule_name,
                    len(conditions),
                    len(consequents),
                    str(rule.id)
                )
            else:
                _logger.warning(f"Rule '{rule_name}' has no valid conditions, skipping")
        else:
            _logger.info(
                "Seed: rule '%s' already exists (id=%s)",
                rule_name,
                str(existing_rule.id)
            )


async def _update_system_relationships(repo_system, system, variables_map, repo_rule):
    """Update system with created variables and rules."""
    # Get all variables for this system
    all_variables = list(variables_map.values())

    # Separate variables by type
    input_variables = [FuzzyVariableId(str(var.id)) for var in all_variables if var.variable_type == "input"]
    output_variables = [FuzzyVariableId(str(var.id)) for var in all_variables if var.variable_type == "output"]

    # Get all rules for this system
    all_rules = await repo_rule.get_by_system_id(FuzzySystemId(str(system.id)))  # type: ignore[attr-defined]

    # Update system
    system.input_variable_ids = input_variables
    system.output_variable_ids = output_variables
    system.rule_ids = [rule.id for rule in all_rules] if all_rules else []

    await repo_system.update(system)  # type: ignore[attr-defined]
    _logger.info(
        "Seed: updated system relationships - %d input vars, %d output vars, %d rules",
        len(input_variables), len(output_variables), len(all_rules)
    )
