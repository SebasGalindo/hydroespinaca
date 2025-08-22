"""Actuator client interface."""
from abc import ABC, abstractmethod
from application.dtos import CreateCommandDto


class IActuatorClient(ABC):
    """Interface for actuator service client operations."""
    
    @abstractmethod
    async def send_command(self, command: CreateCommandDto) -> bool:
        """Send command to actuator service.
        
        Args:
            command: The command to send
            
        Returns:
            True if command was sent successfully, False otherwise
        """
        pass
    
    @abstractmethod
    async def health_check(self) -> bool:
        """Check if actuator service is healthy.
        
        Returns:
            True if service is healthy, False otherwise
        """
        pass