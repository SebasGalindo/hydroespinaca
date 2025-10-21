import logging
import httpx
import os
from typing import Dict, Any
from medyator import CommandHandler

from FuzzyService.Application.Features.ActuatorIntegration.Commands.SendCommandsToActuatorCommand import (
    SendCommandsToActuatorCommand
)
from FuzzyService.Infrastructure.Authentication.jwt_auth import get_auth_service

_logger = logging.getLogger(__name__)


class SendCommandsToActuatorHandler(CommandHandler[SendCommandsToActuatorCommand]):

    async def __call__(self, request: SendCommandsToActuatorCommand) -> None:
        try:
            commands = request.commands

            if not commands:
                _logger.warning("No hay comandos para enviar al actuator-service")
                return

            payload = {"commands": commands}

            actuator_service_url = os.getenv("FUZZY_ACTUATOR_SERVICE_URL") or os.getenv("ACTUATOR_SERVICE_URL") or "http://localhost:5002"
            if not actuator_service_url:
                _logger.error("FUZZY_ACTUATOR_SERVICE_URL no configurada")
                return

            endpoint = f"{actuator_service_url}/api/commands/execute"

            headers = {"Content-Type": "application/json"}

            # Obtener M2M token para autenticación service-to-service
            try:
                auth_service = get_auth_service()
                m2m_token = await auth_service.get_m2m_token()
                headers["Authorization"] = f"Bearer {m2m_token}"
                token_preview = f"{m2m_token[:20]}...{m2m_token[-20:]}" if len(m2m_token) > 40 else m2m_token
                _logger.info(f"M2M token obtenido: {token_preview}")
                _logger.debug(f"Authorization header: Bearer {m2m_token[:30]}...")
            except Exception as e:
                _logger.error(f"Error al obtener M2M token: {type(e).__name__}: {e}")
                _logger.warning("Continuando sin autenticación - esto causará error 401")

            _logger.info(f"Enviando {len(commands)} comandos al actuator-service: {endpoint}")

            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(endpoint, json=payload, headers=headers)
                response.raise_for_status()

                _logger.info(
                    f"Comandos enviados exitosamente al actuator-service. "
                    f"Status: {response.status_code}"
                )

        except httpx.HTTPError as e:
            _logger.error(f"Error HTTP al enviar comandos al actuator-service: {e}", exc_info=True)
            raise
        except Exception as e:
            _logger.error(f"Error al enviar comandos al actuator-service: {e}", exc_info=True)
            raise
