"""Módulo de seguridad para validación JWT.

Implementa la validación de tokens JWT y verificación de scopes
compatible con el auth-service existente.
"""

import logging
from typing import List, Optional

from fastapi import HTTPException, Request, status
from fastapi.security import HTTPBearer
import jwt
from jwt import InvalidTokenError

from infrastructure.config import settings


class ScopeChecker:
    """Dependency para verificar scopes JWT.
    
    Valida tokens JWT y verifica que el usuario tenga los scopes requeridos.
    Si JWT está deshabilitado (sin secret key), permite acceso libre.
    """
    
    def __init__(self, required_scopes: List[str]):
        """
        Args:
            required_scopes: Lista de scopes requeridos para acceder al endpoint
        """
        self.required_scopes = required_scopes
    
    async def __call__(self, request: Request) -> dict:
        """Valida el token JWT y verifica scopes.
        
        Args:
            request: Request HTTP de FastAPI
            
        Returns:
            Dict con información del token validado
            
        Raises:
            HTTPException: Si el token es inválido o faltan scopes
        """
        # Si no hay secret key configurado, modo desarrollo sin seguridad
        if not settings.jwt_secret_key:
            logging.warning(
                "JWT deshabilitado - permitiendo acceso sin autenticación (desarrollo)"
            )
            return {
                "sub": "dev-user",
                "scopes": self.required_scopes,
                "iss": "dev",
                "aud": "dev"
            }
        
        # Extraer token del header Authorization
        token = self._extract_token(request)
        if not token:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Token JWT requerido",
                headers={"WWW-Authenticate": "Bearer"},
            )
        
        # Validar y decodificar token
        try:
            payload = jwt.decode(
                token,
                settings.jwt_secret_key,
                algorithms=[settings.jwt_algorithm],
                issuer=settings.jwt_issuer,
                audience=settings.jwt_audience
            )
        except InvalidTokenError as e:
            logging.warning("Token JWT inválido: %s", str(e))
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Token JWT inválido",
                headers={"WWW-Authenticate": "Bearer"},
            )
        
        # Verificar scopes
        token_scopes = payload.get("scope", "").split()
        missing_scopes = [scope for scope in self.required_scopes if scope not in token_scopes]
        
        if missing_scopes:
            logging.warning(
                "Scopes insuficientes. Requeridos: %s, Token: %s",
                self.required_scopes,
                token_scopes
            )
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail=f"Scopes insuficientes. Requeridos: {missing_scopes}"
            )
        
        logging.debug(
            "Token JWT validado: sub=%s, scopes=%s",
            payload.get("sub"),
            token_scopes
        )
        
        return payload
    
    def _extract_token(self, request: Request) -> Optional[str]:
        """Extrae el token JWT del header Authorization.
        
        Args:
            request: Request HTTP
            
        Returns:
            Token JWT sin el prefijo 'Bearer ', o None si no existe
        """
        authorization = request.headers.get("Authorization")
        if not authorization:
            return None
        
        try:
            scheme, token = authorization.split(" ", 1)
            if scheme.lower() != "bearer":
                return None
            return token
        except ValueError:
            return None


class OptionalScopeChecker(ScopeChecker):
    """Variant de ScopeChecker que no requiere autenticación.
    
    Útil para endpoints que pueden funcionar con o sin autenticación,
    proporcionando funcionalidad adicional si el usuario está autenticado.
    """
    
    async def __call__(self, request: Request) -> Optional[dict]:
        """Valida el token JWT si está presente.
        
        Returns:
            Dict con información del token si es válido, None si no hay token
        """
        try:
            return await super().__call__(request)
        except HTTPException:
            # Si falla la validación, retornar None en lugar de lanzar excepción
            return None


# Instancia global del security scheme
security_scheme = HTTPBearer(auto_error=False)