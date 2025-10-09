"""Domain interface (port) for MQTT communications used by the fuzzy service.

This is kept abstract in the domain layer to allow different infrastructure implementations.
"""

from abc import ABC, abstractmethod
from typing import Any, Dict


class IMqttService(ABC):
    """Port for publishing/subscribing messages via MQTT."""

    @abstractmethod
    async def publish(self, topic: str, payload: Dict[str, Any], qos: int = 0, retain: bool = False) -> None:
        """Publishes a message to a MQTT topic.
        
        Args:
            topic: Topic to publish to
            payload: JSON-like payload
            qos: Quality of service level
            retain: Retain flag
        """
        pass

    @abstractmethod
    async def subscribe(self, topic: str, qos: int = 0) -> None:
        """Subscribes to a MQTT topic with the desired QoS."""
        pass

    @abstractmethod
    async def unsubscribe(self, topic: str) -> None:
        """Unsubscribes from a MQTT topic."""
        pass

    @abstractmethod
    async def is_connected(self) -> bool:
        """Checks whether the MQTT client is currently connected."""
        pass
