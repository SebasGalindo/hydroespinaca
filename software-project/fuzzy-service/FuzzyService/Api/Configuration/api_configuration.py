"""
API-specific configuration utilities.
- CORS setup
- OpenAPI/Swagger metadata
"""
from __future__ import annotations

import os
from typing import List

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware


def _parse_origins(env_value: str | None) -> List[str]:
    if not env_value:
        return ["*"]
    # Split by comma and strip whitespace
    return [o.strip() for o in env_value.split(",") if o.strip()]


def setup_cors(app: FastAPI) -> None:
    # Soportar tanto las variables nuevas como las existentes
    origins = _parse_origins(os.getenv("API_CORS_ORIGINS") or os.getenv("CORS_ORIGINS"))
    allow_credentials = (
        os.getenv("API_CORS_ALLOW_CREDENTIALS") or 
        os.getenv("CORS_ALLOW_CREDENTIALS", "false")
    ).lower() in {"1", "true", "yes", "y"}
    
    # Usar configuración específica o valores por defecto
    allow_methods_str = os.getenv("CORS_ALLOW_METHODS", "*")
    allow_methods = [m.strip() for m in allow_methods_str.split(",")] if allow_methods_str != "*" else ["*"]
    
    allow_headers_str = os.getenv("CORS_ALLOW_HEADERS", "*")
    allow_headers = [h.strip() for h in allow_headers_str.split(",")] if allow_headers_str != "*" else ["*"]

    app.add_middleware(
        CORSMiddleware,
        allow_origins=origins,
        allow_credentials=allow_credentials,
        allow_methods=allow_methods,
        allow_headers=allow_headers,
    )


def setup_docs(app: FastAPI) -> None:
    app.title = os.getenv("API_TITLE", "Fuzzy Service API")
    app.description = os.getenv(
        "API_DESCRIPTION",
        """
        ## Fuzzy Logic Controller Service
        
        Este servicio proporciona una API completa para la gestión de sistemas de lógica difusa, incluyendo:
        
        - **Sistemas Difusos**: Gestión de sistemas de control difuso
        - **Variables Difusas**: Definición de variables de entrada y salida
        - **Términos Lingüísticos**: Configuración de conjuntos difusos
        - **Reglas Difusas**: Creación y gestión de reglas de inferencia
        - **Rutinas Difusas**: Definición de secuencias de acciones
        
        ### Características principales:
        - CRUD completo para todas las entidades
        - Validaciones robustas de datos
        - Manejo de errores consistente
        - Integración con MongoDB
        - Documentación interactiva con Swagger
        """,
    )
    app.version = os.getenv("API_VERSION", "1.0.0")
    
    # Configurar tags para organizar los endpoints
    app.openapi_tags = [
        {
            "name": "health",
            "description": "Endpoints de salud y estado del servicio"
        },
        {
            "name": "fuzzy-systems",
            "description": "Gestión de sistemas de lógica difusa"
        },
        {
            "name": "fuzzy-variables",
            "description": "Gestión de variables difusas (entrada y salida)"
        },
        {
            "name": "fuzzy-terms",
            "description": "Gestión de términos lingüísticos y conjuntos difusos"
        },
        {
            "name": "fuzzy-rules",
            "description": "Gestión de reglas de inferencia difusa"
        },
        {
            "name": "fuzzy-routines",
            "description": "Gestión de rutinas y secuencias de acciones"
        },
        {
            "name": "Fuzzy Evaluations",
            "description": "Consulta de evaluaciones y estadísticas de sistemas difusos"
        }
    ]
    
    # Configurar información de contacto y licencia
    app.contact = {
        "name": "Fuzzy Service Team",
        "email": "support@fuzzyservice.com"
    }
    
    app.license_info = {
        "name": "MIT License",
        "url": "https://opensource.org/licenses/MIT"
    }


def configure_api(app: FastAPI) -> None:
    """Apply all API configurations to the given FastAPI app."""
    setup_docs(app)
    setup_cors(app)
