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


class SeedDataConfig:
    """Configuration class for seed data with predefined IDs."""

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
                "name": "Ambient Temperature",
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
                "name": "Water Temperature",
                "description": "Temperatura del agua del sistema",
                "type": "input",
                "reference_code": "T_WAT",
                "terms": []
            },
            {
                "name": "Water Level",
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
        """Get all fuzzy terms configuration with membership functions."""
        return [
            # TÉRMINOS PARA: Luminosity (BH1750 sensor) (68e05364d86d6edc39982870)
            # Rangos basados en valores reales de lux (0-65535)
            # Umbrales: optimalMin=10000, optimalMax=12000
            {
                "_id": "68e05364d86d6edc39982880",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "bajaLuminosidad",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 5000.0, 10000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982881",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "luminosidadNormal",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [9000.0, 11000.0, 13000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982882",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "altaLuminosidad",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [12000.0, 38767.5, 65535.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            # TÉRMINOS PARA: Ambient Temperature (68e05364d86d6edc39982871)
            {
                "_id": "68e05364d86d6edc39982883",
                "variable_id": "68e05364d86d6edc39982871",
                "label": "frioAmbiental",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [-10.0, 4.0, 18.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982884",
                "variable_id": "68e05364d86d6edc39982871",
                "label": "temperaturaOptima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [15.0, 20.0, 25.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982885",
                "variable_id": "68e05364d86d6edc39982871",
                "label": "calorAmbiental",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [22.0, 31.0, 40.0],
                    "universe_min": -10.0,
                    "universe_max": 40.0
                }
            },
            # TÉRMINOS PARA: Humidity (68e05364d86d6edc39982872)
            {
                "_id": "68e05364d86d6edc39982886",
                "variable_id": "68e05364d86d6edc39982872",
                "label": "ambienteSeco",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 30.0, 60.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982887",
                "variable_id": "68e05364d86d6edc39982872",
                "label": "humedadNormal",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [55.0, 66.5, 78.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982888",
                "variable_id": "68e05364d86d6edc39982872",
                "label": "ambienteHumido",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [70.0, 85.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # TÉRMINOS PARA: Water Temperature (68e05364d86d6edc39982873)
            {
                "_id": "68e05364d86d6edc39982889",
                "variable_id": "68e05364d86d6edc39982873",
                "label": "aguaFria",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [-10.0, 10.0, 17.5],  # Ajustado: peak en 10°C, cae a 0 en 17.5°C
                    "universe_min": -10.0,
                    "universe_max": 85.0
                }
            },
            {
                "_id": "68e05364d86d6edc3998288a",
                "variable_id": "68e05364d86d6edc39982873",
                "label": "temperaturaAguaOptima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [17.0, 20.0, 23.0],  # Ajustado: empieza en 17°C, peak en 20°C
                    "universe_min": -10.0,
                    "universe_max": 85.0
                }
            },
            {
                "_id": "68e05364d86d6edc3998288b",
                "variable_id": "68e05364d86d6edc39982873",
                "label": "aguaCaliente",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [21.0, 53.0, 85.0],
                    "universe_min": -10.0,
                    "universe_max": 85.0
                }
            },
            # TÉRMINOS PARA: Water Level (68e05364d86d6edc398828D8)
            {
                "_id": "68e05364d86d6edc398828D9",
                "variable_id": "68e05364d86d6edc398828D8",
                "label": "NivelÓptimo",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 3.0, 6.0],
                    "universe_min": 0.0,
                    "universe_max": 10.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DA",
                "variable_id": "68e05364d86d6edc398828D8",
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
            {
                "_id": "68e05364d86d6edc39982890",
                "variable_id": "68e05364d86d6edc39982875",
                "label": "potenciaBaja",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 22.5, 40.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982891",
                "variable_id": "68e05364d86d6edc39982875",
                "label": "potenciaMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [30.0, 50.0, 70.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982892",
                "variable_id": "68e05364d86d6edc39982875",
                "label": "potenciaAlta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [60.0, 75.0, 90.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982893",
                "variable_id": "68e05364d86d6edc39982875",
                "label": "potenciaMaxima",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [85.0, 92.5, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # TÉRMINOS COMPARTIDOS PARA: Duraciones (variables de salida)
            # Estos términos se usan en múltiples variables de duración
            {
                "_id": "68e05364d86d6edc39982894",
                "variable_id": "68e05364d86d6edc39982876",  # Duración de Ventilación
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982895",
                "variable_id": "68e05364d86d6edc39982876",  # Duración de Ventilación
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982896",
                "variable_id": "68e05364d86d6edc39982876",  # Duración de Ventilación
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # TÉRMINOS OFF ESPECÍFICOS POR VARIABLE DE SALIDA DIGITAL
            # Término OFF para Duración de Ventilación (68e05364d86d6edc39982876)
            {
                "_id": "68e05364d86d6edc398828A0",
                "variable_id": "68e05364d86d6edc39982876",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Término OFF para Duración de Calefacción de Aire (68e05364d86d6edc39982877)
            {
                "_id": "68e05364d86d6edc398828A1",
                "variable_id": "68e05364d86d6edc39982877",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Término OFF para Duración de Luz (68e05364d86d6edc39982878)
            {
                "_id": "68e05364d86d6edc398828A2",
                "variable_id": "68e05364d86d6edc39982878",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Término OFF para Duración de Aireación (68e05364d86d6edc39982879)
            {
                "_id": "68e05364d86d6edc398828A3",
                "variable_id": "68e05364d86d6edc39982879",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Término OFF para Duración de Riego (68e05364d86d6edc3998287a)
            {
                "_id": "68e05364d86d6edc398828A4",
                "variable_id": "68e05364d86d6edc3998287a",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Término OFF para Duración de Calefacción de Agua (68e05364d86d6edc3998287b)
            {
                "_id": "68e05364d86d6edc398828A5",
                "variable_id": "68e05364d86d6edc3998287b",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # TÉRMINOS DE DURACIÓN PARA VARIABLES DIGITAL
            # Duración de Calefacción de Aire (68e05364d86d6edc39982877)
            {
                "_id": "68e05364d86d6edc398828D0",
                "variable_id": "68e05364d86d6edc39982877",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D1",
                "variable_id": "68e05364d86d6edc39982877",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D2",
                "variable_id": "68e05364d86d6edc39982877",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # Duración de Luz (68e05364d86d6edc39982878)
            {
                "_id": "68e05364d86d6edc398828D3",
                "variable_id": "68e05364d86d6edc39982878",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D4",
                "variable_id": "68e05364d86d6edc39982878",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D5",
                "variable_id": "68e05364d86d6edc39982878",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # Duración de Aireación (68e05364d86d6edc39982879)
            {
                "_id": "68e05364d86d6edc398828D6",
                "variable_id": "68e05364d86d6edc39982879",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D7",
                "variable_id": "68e05364d86d6edc39982879",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828D8",
                "variable_id": "68e05364d86d6edc39982879",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # Duración de Riego (68e05364d86d6edc3998287a)
            {
                "_id": "68e05364d86d6edc398828D9",
                "variable_id": "68e05364d86d6edc3998287a",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DA",
                "variable_id": "68e05364d86d6edc3998287a",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DB",
                "variable_id": "68e05364d86d6edc3998287a",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # Duración de Calefacción de Agua (68e05364d86d6edc3998287b)
            {
                "_id": "68e05364d86d6edc398828DC",
                "variable_id": "68e05364d86d6edc3998287b",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DD",
                "variable_id": "68e05364d86d6edc3998287b",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DE",
                "variable_id": "68e05364d86d6edc3998287b",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            # TÉRMINOS ON/OFF PARA VARIABLES DE CONTROL
            # Control Calefactor Aire (68e05364d86d6edc398828B0)
            {
                "_id": "68e05364d86d6edc398828C0",
                "variable_id": "68e05364d86d6edc398828B0",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828C1",
                "variable_id": "68e05364d86d6edc398828B0",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Calefactor Agua (68e05364d86d6edc398828B1)
            {
                "_id": "68e05364d86d6edc398828C2",
                "variable_id": "68e05364d86d6edc398828B1",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828C3",
                "variable_id": "68e05364d86d6edc398828B1",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Luz (68e05364d86d6edc398828B2)
            {
                "_id": "68e05364d86d6edc398828C4",
                "variable_id": "68e05364d86d6edc398828B2",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828C5",
                "variable_id": "68e05364d86d6edc398828B2",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Humidificador (68e05364d86d6edc398828B3)
            {
                "_id": "68e05364d86d6edc398828C6",
                "variable_id": "68e05364d86d6edc398828B3",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828C7",
                "variable_id": "68e05364d86d6edc398828B3",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Bomba Aireación (68e05364d86d6edc398828B4)
            {
                "_id": "68e05364d86d6edc398828C8",
                "variable_id": "68e05364d86d6edc398828B4",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828C9",
                "variable_id": "68e05364d86d6edc398828B4",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Control Bomba Riego (68e05364d86d6edc398828B5)
            {
                "_id": "68e05364d86d6edc398828CA",
                "variable_id": "68e05364d86d6edc398828B5",
                "label": "OFF",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 0.0, 1.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828CB",
                "variable_id": "68e05364d86d6edc398828B5",
                "label": "ON",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [99.0, 100.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            # Duración de Humidificación (68e05364d86d6edc3998287c)
            {
                "_id": "68e05364d86d6edc398828DE",
                "variable_id": "68e05364d86d6edc3998287c",
                "label": "duracionCorta",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [5.0, 62.5, 120.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828DF",
                "variable_id": "68e05364d86d6edc3998287c",
                "label": "duracionMedia",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [90.0, 195.0, 300.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            },
            {
                "_id": "68e05364d86d6edc398828E0",
                "variable_id": "68e05364d86d6edc3998287c",
                "label": "duracionLarga",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [240.0, 1920.0, 3600.0],
                    "universe_min": 0.0,
                    "universe_max": 3600.0
                }
            }
        ]

   
    @staticmethod
    def get_rules_config() -> List[Dict[str, Any]]:
        """Get fuzzy rules configuration with predefined IDs."""
        return [
            {
                "_id": "68e05365d86d6edc39982910",
                "name": "Regla 1: Temperatura muy fria",
                "description": "Si temperatura ambiente es muy fría, encender calefactor de aire con duración media",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "frioAmbiental"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B0",  # Control Calefactor Aire
                        "terms": ["68e05364d86d6edc398828C1"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982877",  # Duración de Calefacción de Aire
                        "terms": ["68e05364d86d6edc398828D1"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982911",
                "name": "Regla 2: Temperatura normal o mas alta",
                "description": "Si temperatura ambiente es normal u óptima, apagar calefactor de aire con duración corta",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "temperaturaOptima"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B0",  # Control Calefactor Aire
                        "terms": ["68e05364d86d6edc398828C0"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982877",  # Duración de Calefacción de Aire
                        "terms": ["68e05364d86d6edc398828D0"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982912",
                "name": "Regla 3A: Emergencia Térmica - Control de Aire",
                "description": "Si temperatura ambiente es muy alta, activar ventilador (no depende del nivel de agua)",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "calorAmbiental"
                    }
                ],
                "connectors": [],
                "consequents": [
                    # Ventilador - Encender (Potencia Alta, Duración Media)
                    {
                        "variable_id": "68e05364d86d6edc39982875",  # Potencia del Ventilador (PWM)
                        "terms": ["68e05364d86d6edc39982892"],  # potenciaAlta
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982876",  # Duración de Ventilación
                        "terms": ["68e05364d86d6edc39982895"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc3998291B",
                "name": "Regla 3B: Emergencia Térmica - Activación de Recirculación Segura",
                "description": "Si la temperatura ambiente es muy alta Y el nivel de agua es seguro, activar bombas para recirculación de emergencia",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "calorAmbiental"
                    },
                    {
                        "variable_id": "68e05364d86d6edc398828D8",  # Water Level
                        "variable_name": "Water Level",
                        "operator": "IS",
                        "value": "NivelÓptimo"
                    }
                ],
                "connectors": ["AND"],
                "consequents": [
                    # Bomba Aireación - Encender
                    {
                        "variable_id": "68e05364d86d6edc398828B4",  # Control Bomba Aireación
                        "terms": ["68e05364d86d6edc398828C9"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982879",  # Duración de Aireación
                        "terms": ["68e05364d86d6edc39982896"],  # duracionLarga
                        "aggregation_method": "max"
                    },
                    # Bomba Riego - Encender
                    {
                        "variable_id": "68e05364d86d6edc398828B5",  # Control Bomba Riego
                        "terms": ["68e05364d86d6edc398828CB"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287a",  # Duración de Riego
                        "terms": ["68e05364d86d6edc39982896"],  # duracionLarga
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982913",
                "name": "Regla 4: Humedad muy baja",
                "description": "Si humedad ambiente es muy baja, encender humidificador con duración media",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982872",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "ambienteSeco"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B3",  # Control Humidificador
                        "terms": ["68e05364d86d6edc398828C7"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287c",  # Duración de Humidificación
                        "terms": ["68e05364d86d6edc398828DF"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982914",
                "name": "Regla 5: Humedad normal o mas alta",
                "description": "Si humedad ambiente es normal o alta, apagar humidificador con duración corta",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982872",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "humedadNormal"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B3",  # Control Humidificador
                        "terms": ["68e05364d86d6edc398828C6"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287c",  # Duración de Humidificación
                        "terms": ["68e05364d86d6edc398828DE"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982915",
                "name": "Regla 6: Temperatura de agua fria",
                "description": "Si temperatura del agua es muy fría Y el nivel de agua es seguro, encender calefactor de agua con duración media",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "aguaFria"
                    },
                    {
                        "variable_id": "68e05364d86d6edc398828D8",  # Water Level
                        "variable_name": "Water Level",
                        "operator": "IS",
                        "value": "NivelÓptimo"
                    }
                ],
                "connectors": ["AND"],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B1",  # Control Calefactor Agua
                        "terms": ["68e05364d86d6edc398828C3"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287b",  # Duración de Calefacción de Agua
                        "terms": ["68e05364d86d6edc398828DD"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982916",
                "name": "Regla 7: Temperatura de agua normal o mas alta",
                "description": "Si temperatura del agua es normal u óptima, apagar calefactor de agua con duración corta",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "temperaturaAguaOptima"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B1",  # Control Calefactor Agua
                        "terms": ["68e05364d86d6edc398828C2"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287b",  # Duración de Calefacción de Agua
                        "terms": ["68e05364d86d6edc398828DC"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982917",
                "name": "Regla 8: Agua demasiado caliente",
                "description": "Si agua está demasiado caliente, apagar calefactor de agua con duración corta",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "aguaCaliente"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B1",  # Control Calefactor Agua
                        "terms": ["68e05364d86d6edc398828C2"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287b",  # Duración de Calefacción de Agua
                        "terms": ["68e05364d86d6edc398828DC"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982918",
                "name": "Regla 9: Nivel de luz bajo",
                "description": "Si la luminosidad (BH1750) es baja (< 10000 lux), encender luz artificial con duración media",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982870",  # Luminosity
                        "variable_name": "Luminosity",
                        "operator": "IS",
                        "value": "bajaLuminosidad"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B2",  # Control Luz
                        "terms": ["68e05364d86d6edc398828C5"],  # ON
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982878",  # Duración de Luz
                        "terms": ["68e05364d86d6edc398828D4"],  # duracionMedia
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982919",
                "name": "Regla 10: Nivel de luz optimo",
                "description": "Si la luminosidad (BH1750) es normal/óptima (10000-13000 lux), apagar luz artificial con duración corta",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982870",  # Luminosity
                        "variable_name": "Luminosity",
                        "operator": "IS",
                        "value": "luminosidadNormal"
                    }
                ],
                "connectors": [],
                "consequents": [
                    {
                        "variable_id": "68e05364d86d6edc398828B2",  # Control Luz
                        "terms": ["68e05364d86d6edc398828C4"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982878",  # Duración de Luz
                        "terms": ["68e05364d86d6edc398828D3"],  # duracionCorta
                        "aggregation_method": "max"
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc3998291A",
                "name": "Regla 11: Nivel Crítico - Bloqueo de Actuadores de Agua",
                "description": "Si el Nivel de Agua es Crítico, forzar el apagado de la Bomba de Agua, Calefactor de Agua, Bomba de Aireación y Humidificador para proteger el hardware",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc398828D8",  # Water Level
                        "variable_name": "Water Level",
                        "operator": "IS",
                        "value": "NivelCrítico"
                    }
                ],
                "connectors": [],
                "consequents": [
                    # 1. Calefactor Agua = OFF
                    {
                        "variable_id": "68e05364d86d6edc398828B1",  # Control Calefactor Agua
                        "terms": ["68e05364d86d6edc398828C2"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287b",  # Duración de Calefacción de Agua
                        "terms": ["68e05364d86d6edc398828DC"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 2. Bomba Riego = OFF
                    {
                        "variable_id": "68e05364d86d6edc398828B5",  # Control Bomba Riego
                        "terms": ["68e05364d86d6edc398828CA"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287a",  # Duración de Riego
                        "terms": ["68e05364d86d6edc39982897"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 3. Bomba Aireación = OFF
                    {
                        "variable_id": "68e05364d86d6edc398828B4",  # Control Bomba Aireación
                        "terms": ["68e05364d86d6edc398828C8"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982879",  # Duración de Aireación
                        "terms": ["68e05364d86d6edc39982897"],  # duracionCorta
                        "aggregation_method": "max"
                    },
                    # 4. Humidificador = OFF
                    {
                        "variable_id": "68e05364d86d6edc398828B3",  # Control Humidificador
                        "terms": ["68e05364d86d6edc398828C6"],  # OFF
                        "aggregation_method": "max"
                    },
                    {
                        "variable_id": "68e05364d86d6edc3998287c",  # Duración de Humidificación
                        "terms": ["68e05364d86d6edc398828DE"],  # duracionCorta
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

    # Mapping of variable names to old predefined IDs (for backward compatibility with terms config)
    VARIABLE_NAME_TO_OLD_ID = {
        "Luminosity": "68e05364d86d6edc39982870",
        "Ambient Temperature": "68e05364d86d6edc39982871",
        "Humidity": "68e05364d86d6edc39982872",
        "Water Temperature": "68e05364d86d6edc39982873",
        "Water Level": "68e05364d86d6edc398828D8",
        "Potencia del Ventilador": "68e05364d86d6edc39982875",
        "Control Calefactor Aire": "68e05364d86d6edc398828B0",
        "Control Calefactor Agua": "68e05364d86d6edc398828B1",
        "Control Luz": "68e05364d86d6edc398828B2",
        "Control Humidificador": "68e05364d86d6edc398828B3",
        "Control Bomba Aireación": "68e05364d86d6edc398828B4",
        "Control Bomba Riego": "68e05364d86d6edc398828B5",
        "Duración de Ventilación": "68e05364d86d6edc39982876",
        "Duración de Calefacción de Aire": "68e05364d86d6edc39982877",
        "Duración de Luz": "68e05364d86d6edc39982878",
        "Duración de Aireación": "68e05364d86d6edc39982879",
        "Duración de Riego": "68e05364d86d6edc3998287a",
        "Duración de Calefacción de Agua": "68e05364d86d6edc3998287b",
        "Duración de Humidificación": "68e05364d86d6edc3998287c",
    }

    variables_map = {}
    created_variable_ids = {}  # Map: predefined_id -> actual_created_id
    created_term_ids = {}  # Map: predefined_term_id -> actual_created_term_id

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

            # Map old predefined ID to actual MongoDB ID for backward compatibility
            old_id = VARIABLE_NAME_TO_OLD_ID.get(var_name)
            if old_id:
                created_variable_ids[old_id] = str(created_var.id)

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

            # Map old predefined ID to actual MongoDB ID for backward compatibility
            old_id = VARIABLE_NAME_TO_OLD_ID.get(var_name)
            if old_id:
                created_variable_ids[old_id] = str(existing_var.id)

    # STEP 2: Create all terms using actual variable IDs
    _logger.info("STEP 2: Creating fuzzy terms (using actual variable IDs)...")
    terms_by_variable = {}  # Map: variable_id -> list of term_ids

    for term_config in SeedDataConfig.get_terms_config():
        predefined_term_id = term_config["_id"]
        predefined_var_id = term_config["variable_id"]
        actual_var_id = created_variable_ids.get(predefined_var_id)

        if not actual_var_id:
            _logger.warning(
                f"Skipping term '{term_config['label']}': variable {predefined_var_id} not found"
            )
            continue

        variable_id = FuzzyVariableId(actual_var_id)
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

            # Save mapping: predefined_term_id -> actual_term_id
            created_term_ids[predefined_term_id] = actual_term_id

            # Track terms by variable
            if actual_var_id not in terms_by_variable:
                terms_by_variable[actual_var_id] = []
            terms_by_variable[actual_var_id].append(FuzzyTermId(actual_term_id))

            _logger.info(
                "Seed: created term '%s' for variable %s (predefined_id=%s, actual_id=%s)",
                term_config["label"],
                actual_var_id,
                predefined_term_id,
                actual_term_id
            )
        else:
            actual_term_id = str(existing_term.id)

            # Save mapping for existing terms
            created_term_ids[predefined_term_id] = actual_term_id

            # Track existing terms
            if actual_var_id not in terms_by_variable:
                terms_by_variable[actual_var_id] = []
            terms_by_variable[actual_var_id].append(FuzzyTermId(actual_term_id))

            _logger.info(
                "Seed: term '%s' already exists (predefined_id=%s, actual_id=%s)",
                term_config["label"],
                predefined_term_id,
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
                "variable_id": "68e05364d86d6edc398828B0",  # Predefined variable output ID
                "terms": ["68e05364d86d6edc398828C1"],  # List of predefined term IDs for this variable
                "aggregation_method": "max"
            }
        ]
    }

    Args:
        rule_config: Configuration dict for the rule
        created_term_ids: Map of predefined term IDs to actual MongoDB IDs
        created_variable_ids: Map of predefined variable IDs to actual MongoDB IDs
        repo_term: Repository to fetch terms
    """
    from FuzzyService.Domain.Entities.rule_consequent import RuleConsequent
    from FuzzyService.Domain.ValueObjects.DomainId import FuzzyTermId, FuzzyVariableId

    # Get consequents from config
    consequents_config = rule_config.get("consequents", [])

    if consequents_config:
        consequents = []
        for cons_config in consequents_config:
            # New format: variable_id + terms (explicit)
            predefined_variable_id = cons_config.get("variable_id")
            predefined_term_ids = cons_config.get("terms", [])

            # Fallback to old format if new format not found
            if not predefined_variable_id and "term_ids" in cons_config:
                predefined_term_ids = cons_config.get("term_ids", [])

            # Map predefined term IDs to actual IDs
            actual_term_ids = []
            for predefined_id in predefined_term_ids:
                actual_id = created_term_ids.get(predefined_id)
                if actual_id:
                    actual_term_ids.append(FuzzyTermId(actual_id))
                else:
                    _logger.warning(
                        f"Term ID mapping not found: predefined_id={predefined_id}. "
                        f"Available mappings: {len(created_term_ids)} terms"
                    )

            if not actual_term_ids:
                _logger.warning(
                    f"No actual term IDs found for consequent. "
                    f"Predefined IDs: {predefined_term_ids}"
                )
                continue

            # Determine variable_id - CRITICAL FIX: Map predefined ID to actual MongoDB ID
            if predefined_variable_id:
                # Map predefined variable_id to actual MongoDB ID
                actual_variable_id = created_variable_ids.get(predefined_variable_id)
                if actual_variable_id:
                    variable_id = FuzzyVariableId(actual_variable_id)
                    _logger.debug(
                        f"Mapped variable: predefined={predefined_variable_id} -> actual={actual_variable_id}"
                    )
                else:
                    # If no mapping found, log error and skip this consequent
                    _logger.error(
                        f"Variable ID mapping not found: predefined_id={predefined_variable_id}. "
                        f"Available variable mappings: {len(created_variable_ids)} variables. "
                        f"Skipping this consequent."
                    )
                    continue
            else:
                # Fallback: Get variable_id from first term (old format)
                first_term = await repo_term.get_by_id(actual_term_ids[0])  # type: ignore[attr-defined]
                if not first_term:
                    _logger.warning(
                        f"First term not found in repository: term_id={actual_term_ids[0]}"
                    )
                    continue
                variable_id = first_term.variable_id

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
