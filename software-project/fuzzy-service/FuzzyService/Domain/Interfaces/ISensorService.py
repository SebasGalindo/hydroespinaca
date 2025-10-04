"""Interfaz para el servicio de sensor-service."""

from abc import ABC, abstractmethod
from typing import Optional, Dict, Any


class ISensorService(ABC):
    """Interfaz para validar referencias a variables en sensor-service."""

    @abstractmethod
    async def validate_variable_exists(self, variable_id: str) -> bool:
        """Valida que una variable existe en sensor-service mediante GET /api/variables/{id}.

        Args:
            variable_id: ID de la variable a validar

        Returns:
            True si la variable existe (HTTP 200), False en caso contrario (HTTP 404)
        """
        pass

    @abstractmethod
    async def get_variable_details(self, variable_id: str) -> Optional[Dict[str, Any]]:
        """Obtiene los detalles de una variable desde sensor-service mediante GET /api/variables/{id}.

        Args:
            variable_id: ID de la variable

        Returns:
            Diccionario con los detalles de la variable o None si no existe (HTTP 404)
        """
        pass

    @abstractmethod
    async def is_available(self) -> bool:
        """Verifica si el sensor-service está disponible.

        Returns:
            True si el servicio está disponible, False en caso contrario
        """
        pass
