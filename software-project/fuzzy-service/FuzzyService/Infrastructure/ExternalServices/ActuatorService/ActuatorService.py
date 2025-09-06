from __future__ import annotations

import logging
from typing import List, Dict, Any
import httpx

from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService

_logger = logging.getLogger(__name__)


class ActuatorService(IActuatorService):
    """Implementación concreta del servicio de actuadores.
    
    Envía rutinas al actuator-service usando HTTP.
    """
    
    def __init__(self, base_url: str = "http://localhost:5002", timeout: float = 30.0):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.endpoint = "/api/routines/execute"
    
    async def send_routines(self, routines_payload: List[Dict[str, Any]]) -> bool:
        """Envía el payload de rutinas al actuator-service.
        
        Args:
            routines_payload: Lista de rutinas ya defuzzificadas con sus pasos
            
        Returns:
            True si el servicio aceptó las rutinas, False en caso contrario
        """
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    f"{self.base_url}{self.endpoint}",
                    json=routines_payload,
                    headers={"Content-Type": "application/json"}
                )
                
                if response.status_code == 200:
                    _logger.info(
                        f"Rutinas enviadas exitosamente al actuator-service: {len(routines_payload)} rutinas"
                    )
                    return True
                else:
                    _logger.error(
                        f"Error al enviar rutinas: HTTP {response.status_code} - {response.text}"
                    )
                    return False
                    
        except Exception as e:
            _logger.error(f"Error de comunicación con actuator-service: {str(e)}")
            return False
    
    async def is_available(self) -> bool:
        """Verifica si el actuator-service está disponible."""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(f"{self.base_url}/health")
                return response.status_code == 200
        except Exception:
            return False