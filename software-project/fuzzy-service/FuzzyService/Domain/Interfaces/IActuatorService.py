"""Domain interface (port) for interacting with the external Actuator Service.

This remains technology-agnostic and expresses the contract needed by the domain/application.
"""

from abc import ABC, abstractmethod
from typing import List

from ..Entities.fuzzy_routine import FuzzyRoutine


class IActuatorService(ABC):
    """Port for sending routines/commands to the Actuator Service."""

    @abstractmethod
    async def send_routines(self, routines: List[FuzzyRoutine]) -> bool:
        """Sends a list of domain routines to the Actuator Service.
        
        The translation from domain routines to the external service payload/format is a responsibility
        of the infrastructure adapter, typically orchestrated by the application layer.
        
        Args:
            routines: List of domain routines to execute on actuators
        
        Returns:
            True if the Actuator Service accepted the routines, False otherwise
        """
        pass

    @abstractmethod
    async def is_available(self) -> bool:
        """Checks whether the Actuator Service is reachable and healthy."""
        pass
