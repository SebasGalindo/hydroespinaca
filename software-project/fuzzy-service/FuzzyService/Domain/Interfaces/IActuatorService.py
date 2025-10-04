"""Domain interface (port) for interacting with the external Actuator Service.

This remains technology-agnostic and expresses the contract needed by the domain/application.
"""

from abc import ABC, abstractmethod
from typing import List, Dict, Any, Optional


class IActuatorService(ABC):
    """Port for sending routines/commands to the Actuator Service."""

    @abstractmethod
    async def send_routines(self, routines_payload: List[Dict[str, Any]]) -> bool:
        """Sends a pre-built routines payload to the Actuator Service.

        The application should provide the already-defuzzified payload that the external service expects.

        Args:
            routines_payload: List of routines with steps to execute on actuators

        Returns:
            True if the Actuator Service accepted the routines, False otherwise
        """
        pass

    @abstractmethod
    async def is_available(self) -> bool:
        """Checks whether the Actuator Service is reachable and healthy."""
        pass

    @abstractmethod
    async def validate_output_exists(self, output_id: str) -> bool:
        """Valida que un output existe en actuator-service mediante GET /api/outputs/{id}.

        Args:
            output_id: ID del output a validar

        Returns:
            True si el output existe (HTTP 200), False en caso contrario (HTTP 404)
        """
        pass

    @abstractmethod
    async def get_output_details(self, output_id: str) -> Optional[Dict[str, Any]]:
        """Obtiene los detalles de un output desde actuator-service mediante GET /api/outputs/{id}.

        Args:
            output_id: ID del output

        Returns:
            Diccionario con los detalles del output o None si no existe (HTTP 404)
        """
        pass
