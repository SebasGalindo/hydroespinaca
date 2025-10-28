from __future__ import annotations

import logging
from typing import List, Dict, Any, Optional
import httpx
import asyncio

from FuzzyService.Domain.Interfaces.IActuatorService import IActuatorService
from FuzzyService.Infrastructure.Authentication.jwt_auth import get_auth_service
from FuzzyService.Infrastructure.Constants.RepositoryConstants import CONTENT_TYPE_JSON

_logger = logging.getLogger(__name__)


class ActuatorService(IActuatorService):
    """Implementación concreta del servicio de actuadores.
    
    Envía rutinas al actuator-service usando HTTP.
    """
    
    def __init__(
        self,
        base_url: str = "http://actuator-service",
        timeout: float = 30.0,
        *,
        send_endpoint: str = "/api/commands",
        health_endpoint: str = "/health",
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
        headers = {"Content-Type": CONTENT_TYPE_JSON}

        # Obtener M2M token para autenticación service-to-service
        try:
            auth_service = get_auth_service()
            m2m_token = await auth_service.get_m2m_token()
            headers["Authorization"] = f"Bearer {m2m_token}"
            # Log token para debug (primeros y últimos caracteres)
            token_preview = f"{m2m_token[:20]}...{m2m_token[-20:]}" if len(m2m_token) > 40 else m2m_token
            _logger.info(f"M2M token obtenido: {token_preview}")
            _logger.debug(f"Authorization header: Bearer {m2m_token[:30]}...")
        except Exception as e:
            _logger.error(f"Error al obtener M2M token: {type(e).__name__}: {e}")
            _logger.warning("Continuando sin autenticación - esto causará error 401")
            # Fallback a API key si está configurado
            if self.api_key:
                headers[self.auth_header] = self.api_key
                _logger.debug("Usando API key como fallback")

        # El controller espera List<RoutineCommandDto> directamente, NO envuelto en objeto
        # Ver CommandsController.cs:29 - [FromBody] List<RoutineCommandDto> routines
        payload = routines_payload
        target_url = f"{self.base_url}{self.endpoint}"

        _logger.info(f"Enviando {len(routines_payload)} rutina(s) a {target_url}")
        _logger.debug(f"Headers: {headers}")

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        _logger.debug(f"Intento {attempt}/{self.max_retries} - POST {target_url}")
                        response = await client.post(
                            target_url,
                            json=payload,
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
                _logger.debug(f"Verificando disponibilidad del actuator-service en: {url}")

                for attempt in range(1, self.max_retries + 1):
                    try:
                        response = await client.get(url)
                        _logger.debug(f"Health check attempt {attempt}/{self.max_retries}: HTTP {response.status_code}")

                        if response.status_code == 200:
                            _logger.info(f"Actuator-service disponible en {url}")
                            return True
                    except Exception as e:
                        _logger.warning(
                            f"Error en health check (intento {attempt}/{self.max_retries}): {type(e).__name__}: {str(e)}"
                        )
                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)

                _logger.error(f"Actuator-service NO disponible después de {self.max_retries} intentos en {url}")
                return False
        except Exception as e:
            _logger.error(f"Error crítico al verificar disponibilidad del actuator-service: {type(e).__name__}: {str(e)}")
            return False

    async def validate_output_exists(self, output_id: str) -> bool:
        """Valida que un output existe en actuator-service mediante GET /api/outputs/{id}.

        Args:
            output_id: ID del output a validar

        Returns:
            True si el output existe (HTTP 200), False en caso contrario (HTTP 404)
        """
        headers = {"Content-Type": CONTENT_TYPE_JSON}

        # Obtener M2M token
        try:
            auth_service = get_auth_service()
            m2m_token = await auth_service.get_m2m_token()
            headers["Authorization"] = f"Bearer {m2m_token}"
            _logger.debug("M2M token obtenido para validación de output")
        except Exception as e:
            _logger.warning(f"No se pudo obtener M2M token: {e}")
            if self.api_key:
                headers[self.auth_header] = self.api_key

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        url = f"{self.base_url}/api/outputs/{output_id}"
                        response = await client.get(url, headers=headers)

                        if response.status_code == 200:
                            _logger.debug(f"Output {output_id} validado en actuator-service")
                            return True
                        elif response.status_code == 404:
                            _logger.warning(f"Output {output_id} no encontrado en actuator-service")
                            return False
                        else:
                            _logger.warning(
                                f"Error al validar output (HTTP {response.status_code}): {response.text} (intento {attempt}/{self.max_retries})"
                            )
                    except Exception as e:
                        _logger.warning(
                            f"Excepción al validar output: {e} (intento {attempt}/{self.max_retries})"
                        )

                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)

        except Exception as e:
            _logger.error(f"Error de cliente HTTP para actuator-service: {e}")
            return False

        _logger.error(f"No fue posible validar output después de {self.max_retries} reintentos")
        return False

    async def get_output_details(self, output_id: str) -> Optional[Dict[str, Any]]:
        """Obtiene los detalles de un output desde actuator-service mediante GET /api/outputs/{id}.

        Args:
            output_id: ID del output

        Returns:
            Diccionario con los detalles del output o None si no existe (HTTP 404)
        """
        headers = {"Content-Type": CONTENT_TYPE_JSON}

        # Obtener M2M token
        try:
            auth_service = get_auth_service()
            m2m_token = await auth_service.get_m2m_token()
            headers["Authorization"] = f"Bearer {m2m_token}"
            _logger.debug("M2M token obtenido para obtener detalles de output")
        except Exception as e:
            _logger.warning(f"No se pudo obtener M2M token: {e}")
            if self.api_key:
                headers[self.auth_header] = self.api_key

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                for attempt in range(1, self.max_retries + 1):
                    try:
                        url = f"{self.base_url}/api/outputs/{output_id}"
                        response = await client.get(url, headers=headers)

                        if response.status_code == 200:
                            data = response.json()
                            _logger.debug(f"Detalles de output {output_id} obtenidos desde actuator-service")
                            return data
                        elif response.status_code == 404:
                            _logger.warning(f"Output {output_id} no encontrado en actuator-service")
                            return None
                        else:
                            _logger.warning(
                                f"Error al obtener detalles de output (HTTP {response.status_code}): {response.text} (intento {attempt}/{self.max_retries})"
                            )
                    except Exception as e:
                        _logger.warning(
                            f"Excepción al obtener detalles de output: {e} (intento {attempt}/{self.max_retries})"
                        )

                    if attempt < self.max_retries:
                        await asyncio.sleep(self.retry_delay_ms / 1000.0)

        except Exception as e:
            _logger.error(f"Error de cliente HTTP para actuator-service: {e}")
            return None

        _logger.error(f"No fue posible obtener detalles de output después de {self.max_retries} reintentos")
        return None