"""Implementación concreta del servicio de sensor-service."""

from __future__ import annotations

import logging
from typing import Optional, Dict, Any
import httpx
import asyncio

from FuzzyService.Domain.Interfaces.ISensorService import ISensorService
from FuzzyService.Infrastructure.Authentication.jwt_auth import get_auth_service

_logger = logging.getLogger(__name__)


class SensorService(ISensorService):
    """Implementación concreta del servicio de sensor-service.

    Valida referencias a variables en sensor-service usando HTTP.
    """

    def __init__(
        self,
        base_url: str = "http://sensor-service",
        timeout: float = 30.0,
        *,
        variables_endpoint: str = "/api/variables",
        health_endpoint: str = "/api/health",
        api_key: Optional[str] = None,
        auth_header: str = "X-API-Key",
        max_retries: int = 3,
        retry_delay_ms: int = 1000,
    ):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.variables_endpoint = variables_endpoint if variables_endpoint.startswith("/") else f"/{variables_endpoint}"
        self.health_endpoint = health_endpoint if health_endpoint.startswith("/") else f"/{health_endpoint}"
        self.api_key = api_key
        self.auth_header = auth_header
        self.max_retries = max(1, int(max_retries))
        self.retry_delay_ms = max(0, int(retry_delay_ms))

    async def _get_headers(self) -> Dict[str, str]:
        """Obtiene headers con autenticación M2M."""
        headers = {"Content-Type": "application/json"}

        # Obtener M2M token para autenticación service-to-service
        try:
            auth_service = get_auth_service()
            m2m_token = await auth_service.get_m2m_token()
            headers["Authorization"] = f"Bearer {m2m_token}"
            _logger.debug("M2M token obtenido para sensor-service")
        except Exception as e:
            _logger.warning(f"No se pudo obtener M2M token: {e}")
            # Fallback a API key si está configurado
            if self.api_key:
                headers[self.auth_header] = self.api_key

        return headers

    async def validate_variable_exists(self, variable_id: str) -> bool:
        """Valida que una variable existe en sensor-service.

        Args:
            variable_id: ID de la variable a validar

        Returns:
            True si la variable existe, False en caso contrario
        """
        try:
            headers = await self._get_headers()
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        url = f"{self.base_url}{self.variables_endpoint}/{variable_id}"
                        response = await client.get(url, headers=headers)

                        if response.status_code == 200:
                            _logger.debug(f"Variable {variable_id} validada en sensor-service")
                            return True
                        elif response.status_code == 404:
                            _logger.warning(f"Variable {variable_id} no encontrada en sensor-service")
                            return False
                        else:
                            _logger.warning(
                                f"Error al validar variable (HTTP {response.status_code}): {response.text} (intento {attempt}/{self.max_retries})"
                            )
                    except Exception as e:
                        _logger.warning(
                            f"Excepción al validar variable en sensor-service: {e} (intento {attempt}/{self.max_retries})"
                        )

                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)

        except Exception as e:
            _logger.error(f"Error de cliente HTTP para sensor-service: {e}")
            return False

        _logger.error(f"No fue posible validar variable después de {self.max_retries} reintentos")
        return False

    async def get_variable_details(self, variable_id: str) -> Optional[Dict[str, Any]]:
        """Obtiene los detalles de una variable desde sensor-service.

        Args:
            variable_id: ID de la variable

        Returns:
            Diccionario con los detalles de la variable o None si no existe
        """
        try:
            headers = await self._get_headers()
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        url = f"{self.base_url}{self.variables_endpoint}/{variable_id}"
                        response = await client.get(url, headers=headers)

                        if response.status_code == 200:
                            data = response.json()
                            _logger.debug(f"Detalles de variable {variable_id} obtenidos desde sensor-service")
                            return data
                        elif response.status_code == 404:
                            _logger.warning(f"Variable {variable_id} no encontrada en sensor-service")
                            return None
                        else:
                            _logger.warning(
                                f"Error al obtener detalles de variable (HTTP {response.status_code}): {response.text} (intento {attempt}/{self.max_retries})"
                            )
                    except Exception as e:
                        _logger.warning(
                            f"Excepción al obtener detalles de variable: {e} (intento {attempt}/{self.max_retries})"
                        )

                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)

        except Exception as e:
            _logger.error(f"Error de cliente HTTP para sensor-service: {e}")
            return None

        _logger.error(f"No fue posible obtener detalles de variable después de {self.max_retries} reintentos")
        return None

    async def is_available(self) -> bool:
        """Verifica si el sensor-service está disponible."""
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
