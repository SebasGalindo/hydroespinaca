"""Cliente para comunicación con el actuator-service.

Maneja la comunicación HTTP con el actuator-service usando API key
para autenticación y retry logic para robustez.
"""

import asyncio
import logging
from typing import Optional

import httpx
from application.dtos import CreateCommandDto
from domain.interfaces.actuator_client import IActuatorClient


class ActuatorServiceClient(IActuatorClient):
    """Cliente HTTP para el actuator-service.
    
    Envía comandos al actuator-service usando autenticación por API key.
    Incluye retry logic básico para manejar fallos temporales.
    """
    
    def __init__(self, base_url: str, api_key: str, timeout: float = 10.0):
        """
        Args:
            base_url: URL base del actuator-service (ej: http://actuator-service:8080)
            api_key: API key para autenticación
            timeout: Timeout en segundos para requests HTTP
        """
        self.base_url = base_url.rstrip('/')
        self.api_key = api_key
        self.timeout = timeout
        
        # Headers comunes para todas las requests
        self.headers = {
            "X-API-Key": self.api_key,
            "Content-Type": "application/json",
        }
        
        logging.info("ActuatorServiceClient inicializado: %s", self.base_url)
    
    async def send_command(self, command: CreateCommandDto) -> bool:
        """Envía un comando al actuator-service.
        
        Args:
            command: Comando a enviar
            
        Returns:
            True si el comando se envió exitosamente, False en caso contrario
        """
        url = f"{self.base_url}/api/commands"
        
        # Convertir el comando a dict para serialización JSON
        payload = command.model_dump(exclude_none=True)
        
        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                response = await client.post(
                    url,
                    json=payload,
                    headers=self.headers
                )
                
                if response.status_code == 201:
                    logging.info(
                        "Comando enviado exitosamente: %s -> %s",
                        command.ActuatorId,
                        command.Action
                    )
                    return True
                else:
                    logging.error(
                        "Error al enviar comando: HTTP %d - %s",
                        response.status_code,
                        response.text
                    )
                    return False
                    
        except httpx.TimeoutException:
            logging.error(
                "Timeout al enviar comando a %s (%.1fs)",
                url,
                self.timeout
            )
            return False
            
        except httpx.RequestError as e:
            logging.error(
                "Error de conexión al enviar comando: %s",
                str(e)
            )
            return False
            
        except Exception as e:
            logging.error(
                "Error inesperado al enviar comando: %s",
                str(e)
            )
            return False
    
    async def send_command_with_retry(self, command: CreateCommandDto, max_retries: int = 3) -> bool:
        """Envía un comando con retry logic.
        
        Args:
            command: Comando a enviar
            max_retries: Número máximo de reintentos
            
        Returns:
            True si el comando se envió exitosamente, False después de agotar reintentos
        """
        for attempt in range(max_retries + 1):
            if await self.send_command(command):
                return True
                
            if attempt < max_retries:
                wait_seconds = 2 ** attempt  # backoff exponencial
                logging.warning(
                    "Reintentando comando en %ds (intento %d/%d)",
                    wait_seconds,
                    attempt + 1,
                    max_retries
                )
                await asyncio.sleep(wait_seconds)
        
        logging.error(
            "Falló envío de comando después de %d intentos: %s -> %s",
            max_retries + 1,
            command.ActuatorId,
            command.Action
        )
        return False
    
    async def health_check(self) -> bool:
        """Verifica la conectividad con el actuator-service.
        
        Returns:
            True si el servicio responde correctamente
        """
        url = f"{self.base_url}/healthz"
        
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                response = await client.get(url, headers=self.headers)
                return response.status_code == 200
                
        except Exception as e:
            logging.warning("Health check falló: %s", str(e))
            return False