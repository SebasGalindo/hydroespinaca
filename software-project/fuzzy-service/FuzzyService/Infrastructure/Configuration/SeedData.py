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
    FuzzyTermId,
    FuzzyRoutineId
)
from FuzzyService.Domain.ValueObjects.MembershipFunction import MembershipFunction
from FuzzyService.Domain.Enums.MembershipFunctionType import MembershipFunctionType

# Import interfaces for DI access
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository
from FuzzyService.Domain.Interfaces.IFuzzyVariableRepository import IFuzzyVariableRepository
from FuzzyService.Domain.Interfaces.IFuzzyTermRepository import IFuzzyTermRepository
from FuzzyService.Domain.Interfaces.IFuzzyRuleRepository import IFuzzyRuleRepository
from FuzzyService.Domain.Interfaces.IFuzzyRoutineRepository import IFuzzyRoutineRepository

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
                "_id": "68e05364d86d6edc39982870",
                "name": "Luminosity Index",
                "description": "Índice de luminosidad ambiental",
                "type": "input",
                "reference_id": "688970837f02137645d58395",
                "terms": []  # Se llenarán con IDs de términos creados
            },
            {
                "_id": "68e05364d86d6edc39982871",
                "name": "Ambient Temperature",
                "description": "Temperatura ambiente del invernadero",
                "type": "input",
                "reference_id": "688970ab7f02137645d58398",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982872",
                "name": "Humidity",
                "description": "Humedad relativa del ambiente",
                "type": "input",
                "reference_id": "688970af7f02137645d58399",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982873",
                "name": "Water Temperature",
                "description": "Temperatura del agua del sistema",
                "type": "input",
                "reference_id": "68bb4d8cbdcb66fc5738f9af",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982874",
                "name": "Luminosity Clear",
                "description": "Luminosidad clara (lux)",
                "type": "input",
                "reference_id": "68d6dde25b8956ed967d6a8d",
                "terms": []
            },
            # OUTPUT VARIABLES
            {
                "_id": "68e05364d86d6edc39982875",
                "name": "Potencia del Ventilador",
                "description": "Control de potencia del ventilador (PWM)",
                "type": "output",
                "actuator_type": "PWM",
                "reference_id": "68e04314d86d6edc39982869",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982876",
                "name": "Duración de Ventilación",
                "description": "Duración de activación del ventilador (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04314d86d6edc3998286a",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982877",
                "name": "Duración de Calefacción de Aire",
                "description": "Duración de calefacción del aire (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04314d86d6edc3998286b",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982878",
                "name": "Duración de Luz",
                "description": "Duración de luz artificial (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04314d86d6edc3998286c",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc39982879",
                "name": "Duración de Aireación",
                "description": "Duración de aireación del agua (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04315d86d6edc3998286d",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc3998287a",
                "name": "Duración de Riego",
                "description": "Duración del riego (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04315d86d6edc3998286e",
                "terms": []
            },
            {
                "_id": "68e05364d86d6edc3998287b",
                "name": "Duración de Calefacción de Agua",
                "description": "Duración de calefacción del agua (segundos)",
                "type": "output",
                "actuator_type": "DIGITAL",
                "reference_id": "68e04315d86d6edc3998286f",
                "terms": []
            }
        ]

    @staticmethod
    def get_terms_config() -> List[Dict[str, Any]]:
        """Get all fuzzy terms configuration with membership functions."""
        return [
            # TÉRMINOS PARA: Luminosity Index (68e05364d86d6edc39982870)
            {
                "_id": "68e05364d86d6edc39982880",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "bajaLuminosidad",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 22.5, 45.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982881",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "luminosidadNormal",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [35.0, 57.5, 80.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            },
            {
                "_id": "68e05364d86d6edc39982882",
                "variable_id": "68e05364d86d6edc39982870",
                "label": "altaLuminosidad",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [70.0, 85.0, 100.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
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
                    "parameters": [-10.0, 4.0, 18.0],
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
                    "parameters": [15.0, 19.5, 24.0],
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
            # TÉRMINOS PARA: Luminosity Clear (68e05364d86d6edc39982874)
            {
                "_id": "68e05364d86d6edc3998288c",
                "variable_id": "68e05364d86d6edc39982874",
                "label": "pocaLuz",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [0.0, 1500.0, 3000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {
                "_id": "68e05364d86d6edc3998288d",
                "variable_id": "68e05364d86d6edc39982874",
                "label": "luzAdecuada",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [2000.0, 3500.0, 5000.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
                }
            },
            {
                "_id": "68e05364d86d6edc3998288e",
                "variable_id": "68e05364d86d6edc39982874",
                "label": "muchaLuz",
                "membership_function": {
                    "function_type": "triangular",
                    "parameters": [4000.0, 34767.5, 65535.0],
                    "universe_min": 0.0,
                    "universe_max": 65535.0
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
                    "parameters": [0.0, 0.0, 0.0],
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
                    "parameters": [0.0, 0.0, 0.0],
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
                    "parameters": [0.0, 0.0, 0.0],
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
                    "parameters": [0.0, 0.0, 0.0],
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
                    "parameters": [0.0, 0.0, 0.0],
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
                    "parameters": [0.0, 0.0, 0.0],
                    "universe_min": 0.0,
                    "universe_max": 100.0
                }
            }
        ]

    @staticmethod
    def get_routines_config() -> List[Dict[str, Any]]:
        """Get fuzzy routines configuration with predefined IDs."""
        return [
            {
                "_id": "68e05365d86d6edc39982900",
                "name": "Encender Calefactor Aire",
                "description": "Activar calefactor de aire",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Activar calefactor de aire (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A1",  # OFF específico de Calefacción Aire
                        "duration_term_id": "68e05364d86d6edc39982895"  # duracionMedia
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982901",
                "name": "Apagar Calefactor Aire",
                "description": "Desactivar calefactor de aire",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Desactivar calefactor de aire (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A1",  # OFF específico de Calefacción Aire
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982902",
                "name": "Encender Ventilador",
                "description": "Activar ventilador a potencia alta",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Activar ventilador a potencia alta (PWM)",
                        "power_term_id": "68e05364d86d6edc39982892",  # potenciaAlta (75%)
                        "duration_term_id": "68e05364d86d6edc39982895"  # duracionMedia
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982903",
                "name": "Apagar Ventilador",
                "description": "Desactivar ventilador",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Desactivar ventilador (PWM a 0%)",
                        "power_term_id": "68e05364d86d6edc39982890",  # potenciaBaja (5-40%)
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982904",
                "name": "Encender Calefactor Agua",
                "description": "Activar calefactor de agua",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Activar calefactor de agua (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A5",  # OFF específico de Calefacción Agua
                        "duration_term_id": "68e05364d86d6edc39982895"  # duracionMedia
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982905",
                "name": "Apagar Calefactor Agua",
                "description": "Desactivar calefactor de agua",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Desactivar calefactor de agua (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A5",  # OFF específico de Calefacción Agua
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982906",
                "name": "Encender Humidificador",
                "description": "Activar humidificador",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Activar humidificador (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A4",  # OFF específico de Riego (Humidificador)
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982907",
                "name": "Apagar Humidificador",
                "description": "Desactivar humidificador",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Desactivar humidificador (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A4",  # OFF específico de Riego (Humidificador)
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982908",
                "name": "Emergencia Termica",
                "description": "Protocolo de emergencia térmica - apagar calefactores y activar enfriamiento",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Forzar apagado de calefactor de aire (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A1",  # OFF específico de Calefacción Aire
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    },
                    {
                        "stepId": 1,
                        "condition": "Encender ventilador (PWM)",
                        "power_term_id": "68e05364d86d6edc39982892",  # potenciaAlta (~75%)
                        "duration_term_id": "68e05364d86d6edc39982895"  # duracionMedia
                    },
                    {
                        "stepId": 2,
                        "condition": "Encender bomba de aire (PWM)",
                        "power_term_id": "68e05364d86d6edc39982893",  # potenciaMaxima (~92%)
                        "duration_term_id": "68e05364d86d6edc39982896"  # duracionLarga
                    },
                    {
                        "stepId": 3,
                        "condition": "Encender bomba de agua (PWM)",
                        "power_term_id": "68e05364d86d6edc39982893",  # potenciaMaxima (~92%)
                        "duration_term_id": "68e05364d86d6edc39982896"  # duracionLarga
                    },
                    {
                        "stepId": 4,
                        "condition": "Apagar calefactor de agua (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc39982897",  # OFF (0)
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc39982909",
                "name": "Apagar Actuadores de Agua",
                "description": "Apagar todos los actuadores relacionados con el agua",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Apagar calefactor de agua (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A5",  # OFF específico de Calefacción Agua
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    },
                    {
                        "stepId": 1,
                        "condition": "Apagar bomba de agua (PWM)",
                        "power_term_id": "68e05364d86d6edc398828A3",  # OFF específico de Aireación
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc3998290a",
                "name": "Encender Luz",
                "description": "Encender luz de amplio espectro",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Encender luz de amplio espectro (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A2",  # OFF específico de Luz
                        "duration_term_id": "68e05364d86d6edc39982896"  # duracionLarga
                    }
                ]
            },
            {
                "_id": "68e05365d86d6edc3998290b",
                "name": "Apagar Luz",
                "description": "Apagar luz de amplio espectro",
                "steps": [
                    {
                        "stepId": 0,
                        "condition": "Apagar luz de amplio espectro (DIGITAL)",
                        "power_term_id": "68e05364d86d6edc398828A2",  # OFF específico de Luz
                        "duration_term_id": "68e05364d86d6edc39982894"  # duracionCorta
                    }
                ]
            }
        ]

    @staticmethod
    def get_rules_config() -> List[Dict[str, Any]]:
        """Get fuzzy rules configuration with predefined IDs."""
        return [
            {
                "_id": "68e05365d86d6edc39982910",
                "name": "Regla 1: Temperatura muy fria",
                "description": "Si temperatura ambiente es muy fría, encender calefactor de aire",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "frioAmbiental"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982900",  # Encender Calefactor Aire
                "consequent_routine_name": "Encender Calefactor Aire"
            },
            {
                "_id": "68e05365d86d6edc39982911",
                "name": "Regla 2: Temperatura normal o mas alta",
                "description": "Si temperatura ambiente es normal u óptima, apagar calefactor de aire",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "temperaturaOptima"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982901",  # Apagar Calefactor Aire
                "consequent_routine_name": "Apagar Calefactor Aire"
            },
            {
                "_id": "68e05365d86d6edc39982912",
                "name": "Regla 3: Temperatura de emergencia",
                "description": "Si temperatura ambiente es muy alta, activar protocolo de emergencia térmica",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982871",  # Ambient Temperature
                        "variable_name": "Ambient Temperature",
                        "operator": "IS",
                        "value": "calorAmbiental"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982908",  # Emergencia Termica
                "consequent_routine_name": "Emergencia Termica"
            },
            {
                "_id": "68e05365d86d6edc39982913",
                "name": "Regla 4: Humedad muy baja",
                "description": "Si humedad ambiente es muy baja, encender humidificador",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982872",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "ambienteSeco"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982906",  # Encender Humidificador
                "consequent_routine_name": "Encender Humidificador"
            },
            {
                "_id": "68e05365d86d6edc39982914",
                "name": "Regla 5: Humedad normal o mas alta",
                "description": "Si humedad ambiente es normal o alta, apagar humidificador",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982872",  # Humidity
                        "variable_name": "Humidity",
                        "operator": "IS",
                        "value": "humedadNormal"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982907",  # Apagar Humidificador
                "consequent_routine_name": "Apagar Humidificador"
            },
            {
                "_id": "68e05365d86d6edc39982915",
                "name": "Regla 6: Temperatura de agua fria",
                "description": "Si temperatura del agua es muy fría, encender calefactor de agua",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "aguaFria"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982904",  # Encender Calefactor Agua
                "consequent_routine_name": "Encender Calefactor Agua"
            },
            {
                "_id": "68e05365d86d6edc39982916",
                "name": "Regla 7: Temperatura de agua normal o mas alta",
                "description": "Si temperatura del agua es normal u óptima, apagar calefactor de agua",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "temperaturaAguaOptima"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982905",  # Apagar Calefactor Agua
                "consequent_routine_name": "Apagar Calefactor Agua"
            },
            {
                "_id": "68e05365d86d6edc39982917",
                "name": "Regla 8: Agua demasiado caliente",
                "description": "Si agua está demasiado caliente, apagar calefactor de agua",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982873",  # Water Temperature
                        "variable_name": "Water Temperature",
                        "operator": "IS",
                        "value": "aguaCaliente"
                    }
                ],
                "connectors": [],
                "consequent_routine_id": "68e05365d86d6edc39982905",  # Apagar Calefactor Agua
                "consequent_routine_name": "Apagar Calefactor Agua"
            },
            {
                "_id": "68e05365d86d6edc39982918",
                "name": "Regla 9: Nivel de luz bajo",
                "description": "Si tanto el índice de luminosidad como la luz clara son bajas, encender luz artificial",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982870",  # Luminosity Index
                        "variable_name": "Luminosity Index",
                        "operator": "IS",
                        "value": "bajaLuminosidad"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982874",  # Luminosity Clear
                        "variable_name": "Luminosity Clear",
                        "operator": "IS",
                        "value": "pocaLuz"
                    }
                ],
                "connectors": ["AND"],
                "consequent_routine_id": "68e05365d86d6edc3998290a",  # Encender Luz
                "consequent_routine_name": "Encender Luz"
            },
            {
                "_id": "68e05365d86d6edc39982919",
                "name": "Regla 10: Nivel de luz optimo",
                "description": "Si tanto el índice de luminosidad como la luz clara son óptimas, apagar luz artificial",
                "conditions": [
                    {
                        "variable_id": "68e05364d86d6edc39982870",  # Luminosity Index
                        "variable_name": "Luminosity Index",
                        "operator": "IS",
                        "value": "luminosidadNormal"
                    },
                    {
                        "variable_id": "68e05364d86d6edc39982874",  # Luminosity Clear
                        "variable_name": "Luminosity Clear",
                        "operator": "IS",
                        "value": "luzAdecuada"
                    }
                ],
                "connectors": ["AND"],
                "consequent_routine_id": "68e05365d86d6edc3998290b",  # Apagar Luz
                "consequent_routine_name": "Apagar Luz"
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
    repo_routine = di[IFuzzyRoutineRepository]
    repo_rule = di[IFuzzyRuleRepository]

    if not all([repo_system, repo_variable, repo_term, repo_routine, repo_rule]):
        _logger.warning("Some repositories not available; skipping seed data creation.")
        return

    # Create fuzzy system
    system = await _create_fuzzy_system(repo_system)
    if not system:
        return

    # Create variables and terms with predefined IDs
    variables_map, created_term_ids = await _create_variables_and_terms(repo_variable, repo_term)

    # Create routines
    routines_map = await _create_routines(repo_routine, created_term_ids)

    # Create rules
    await _create_rules(repo_rule, system, variables_map, routines_map)

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
                reference_id=var_config["reference_id"],
                terms=[]  # Empty initially
            )

            created_var = await repo_variable.create(variable)  # type: ignore[attr-defined]
            variables_map[var_name] = created_var
            created_variable_ids[var_config["_id"]] = str(created_var.id)

            _logger.info(
                "Seed: created variable '%s' (predefined_id=%s, actual_id=%s)",
                var_config["name"],
                var_config["_id"],
                str(created_var.id)
            )
        else:
            variables_map[var_name] = existing_var
            created_variable_ids[var_config["_id"]] = str(existing_var.id)
            _logger.info(
                "Seed: variable '%s' already exists (id=%s)",
                var_name,
                str(existing_var.id)
            )

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

    return variables_map, created_term_ids


async def _create_routines(repo_routine, created_term_ids):
    """Create fuzzy routines using dynamic term IDs."""
    from FuzzyService.Domain.Entities.fuzzy_routine import FuzzyRoutine, RoutineStep
    from FuzzyService.Domain.ValueObjects.DomainId import DomainId

    routines_map = {}
    created_routine_ids = {}  # Map: predefined_routine_id -> actual_routine_id

    for routine_config in SeedDataConfig.get_routines_config():
        routine_name = routine_config["name"]
        predefined_routine_id = routine_config["_id"]

        existing_routine = await repo_routine.get_by_name(routine_name)  # type: ignore[attr-defined]

        if not existing_routine:
            # Create routine steps
            steps = []
            for step_config in routine_config["steps"]:
                predefined_power_term = step_config["power_term_id"]
                predefined_duration_term = step_config["duration_term_id"]

                # Get actual term IDs from mapping
                actual_power_term_id = created_term_ids.get(predefined_power_term)
                actual_duration_term_id = created_term_ids.get(predefined_duration_term)

                if not actual_power_term_id or not actual_duration_term_id:
                    _logger.warning(
                        f"Skipping step {step_config['stepId']} in routine '{routine_name}': "
                        f"power_term={predefined_power_term} or duration_term={predefined_duration_term} not found"
                    )
                    continue

                step = RoutineStep(
                    step_id=step_config["stepId"],
                    condition=step_config["condition"],
                    power_term_id=DomainId(actual_power_term_id),
                    duration_term_id=DomainId(actual_duration_term_id)
                )
                steps.append(step)

            if steps:
                routine = await repo_routine.create(  # type: ignore[attr-defined]
                    FuzzyRoutine(
                        routine_name=routine_name,
                        steps=steps
                    )
                )
                actual_routine_id = str(routine.id)
                created_routine_ids[predefined_routine_id] = actual_routine_id
                routines_map[routine_name] = routine

                _logger.info(
                    "Seed: created routine '%s' with %d steps (predefined_id=%s, actual_id=%s)",
                    routine_name,
                    len(steps),
                    predefined_routine_id,
                    actual_routine_id
                )
            else:
                _logger.warning(f"Routine '{routine_name}' has no valid steps, skipping")
        else:
            actual_routine_id = str(existing_routine.id)
            created_routine_ids[predefined_routine_id] = actual_routine_id
            routines_map[routine_name] = existing_routine

            _logger.info(
                "Seed: routine '%s' already exists (predefined_id=%s, actual_id=%s)",
                routine_name,
                predefined_routine_id,
                actual_routine_id
            )

    return routines_map


async def _create_rules(repo_rule, system, variables_map, routines_map):
    """Create fuzzy rules using dynamic variable and routine IDs."""
    from FuzzyService.Domain.Entities.fuzzy_rule import FuzzyRule

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

            # Get consequent routine by name
            routine_name = rule_config["consequent_routine_name"]
            routine = routines_map.get(routine_name)

            if not routine:
                _logger.warning(
                    f"Skipping rule '{rule_name}': routine '{routine_name}' not found"
                )
                continue

            if conditions:
                rule = await repo_rule.create(  # type: ignore[attr-defined]
                    FuzzyRule(
                        name=rule_name,
                        system_id=FuzzySystemId(str(system.id)),
                        description=rule_config.get("description", ""),
                        conditions=conditions,
                        connectors=rule_config["connectors"],
                        consequent=FuzzyRoutineId(str(routine.id)),
                    )
                )
                _logger.info(
                    "Seed: created rule '%s' with %d conditions (id=%s)",
                    rule_name,
                    len(conditions),
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
