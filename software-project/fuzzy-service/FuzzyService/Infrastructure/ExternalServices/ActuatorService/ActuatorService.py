from __future__ import annotations

import logging
from typing import List, Dict, Any, Optional
import httpx
import asyncio

from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService

_logger = logging.getLogger(__name__)


class ActuatorService(IActuatorService):
    """Implementación concreta del servicio de actuadores.
    
    Envía rutinas al actuator-service usando HTTP.
    """
    
    def __init__(
        self,
        base_url: str = "http://localhost:5002",
        timeout: float = 30.0,
        *,
        send_endpoint: str = "/api/routines/execute",
        health_endpoint: str = "/api/health",
        api_key: Optional[str] = None,
        auth_header: str = "X-API-Key",
        max_retries: int = 3,
        retry_delay_ms: int = 1000,
    ):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.endpoint = send_endpoint if send_endpoint.startswith("/") else f"/{send_endpoint}"
        self.health_endpoint = health_endpoint if health_endpoint.startswith("/") else f"/{health_endpoint}"
        self.api_key = api_key
        self.auth_header = auth_header
        self.max_retries = max(1, int(max_retries))
        self.retry_delay_ms = max(0, int(retry_delay_ms))
    
    async def send_routines(self, routines_payload: List[Dict[str, Any]]) -> bool:
        """Envía el payload de rutinas al actuator-service.
        
        Args:
            routines_payload: Lista de rutinas ya defuzzificadas con sus pasos
            
        Returns:
            True si el servicio aceptó las rutinas, False en caso contrario
        """
        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers[self.auth_header] = self.api_key
        
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        response = await client.post(
                            f"{self.base_url}{self.endpoint}",
                            json=routines_payload,
                            headers=headers,
                        )
                        if response.status_code == 200:
                            _logger.info(
                                "Rutinas enviadas exitosamente al actuator-service: %s rutinas (intento %s/%s)",
                                len(routines_payload), attempt, self.max_retries,
                            )
                            return True
                        else:
                            _logger.warning(
                                "Error al enviar rutinas (HTTP %s): %s (intento %s/%s)",
                                response.status_code, response.text, attempt, self.max_retries,
                            )
                    except Exception as e:
                        _logger.warning(
                            "Excepción al enviar rutinas al actuator-service: %s (intento %s/%s)",
                            str(e), attempt, self.max_retries,
                        )
                    
                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)
        except Exception as e:
            _logger.error("Error de cliente HTTP para actuator-service: %s", str(e))
            return False
        
        _logger.error("No fue posible enviar rutinas después de %s reintentos.", self.max_retries)
        return False
    
    async def is_available(self) -> bool:
        """Verifica si el actuator-service está disponible."""
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                url = f"{self.base_url}{self.health_endpoint}"
                for attempt in range(1, self.max_retries + 1):
                    try:
                        response = await client.get(url)
                        if response.status_code == 200:
                            return True
                    except Exception:
                        pass
                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)
                return False
        except Exception:
            return False