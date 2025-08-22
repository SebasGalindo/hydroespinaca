"""MQTT client interface."""
from abc import ABC, abstractmethod
from typing import Callable, Optional
from application.dtos import ReadingBatch


class IMQTTClient(ABC):
    """Interface for MQTT client operations."""
    
    @abstractmethod
    async def connect(self) -> None:
        """Connect to MQTT broker."""
        pass
    
    @abstractmethod
    async def disconnect(self) -> None:
        """Disconnect from MQTT broker."""
        pass
    
    @abstractmethod
    async def subscribe_to_topics(self, topics: list[str]) -> None:
        """Subscribe to MQTT topics."""
        pass
    
    @abstractmethod
    def set_message_handler(self, handler: Callable[[ReadingBatch], None]) -> None:
        """Set the message handler for incoming readings."""
        pass
    
    @abstractmethod
    async def start_listening(self) -> None:
        """Start listening for messages."""
        pass
    
    @abstractmethod
    async def stop_listening(self) -> None:
        """Stop listening for messages."""
        pass