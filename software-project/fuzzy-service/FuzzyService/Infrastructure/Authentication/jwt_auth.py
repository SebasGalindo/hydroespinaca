"""
JWT Authentication middleware and utilities for fuzzy-service.
Implements similar patterns to the .NET services with M2M token support.
"""

import os
import logging
import asyncio
import time
from typing import Optional, List, Set, Dict, Any
from dataclasses import dataclass
import httpx
import jwt
from jwt import PyJWKClient
from fastapi import HTTPException, Security, Depends
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel
import aiofiles

_logger = logging.getLogger(__name__)

security = HTTPBearer(auto_error=False)


@dataclass
class JwtSettings:
    """JWT configuration similar to .NET services."""
    issuer: str
    audience: str
    public_key_path: Optional[str] = None
    public_key_content: Optional[str] = None
    jwks_url: Optional[str] = None
    algorithm: str = "RS256"


@dataclass 
class M2MSettings:
    """M2M authentication settings."""
    client_id: str
    client_secret: str
    auth_service_url: str
    token_endpoint: str = "/api/auth/token"
    token_cache_duration_minutes: int = 50


class UserClaims(BaseModel):
    """User claims extracted from JWT token."""
    user_id: str
    role: str
    scopes: List[str]
    is_system_admin: bool = False


class AuthenticationService:
    """Authentication service for JWT validation and M2M token management."""
    
    def __init__(self, jwt_settings: JwtSettings, m2m_settings: Optional[M2MSettings] = None):
        self.jwt_settings = jwt_settings
        self.m2m_settings = m2m_settings
        self._public_key: Optional[str] = None
        self._jwks_client: Optional[PyJWKClient] = None
        self._m2m_token: Optional[str] = None
        self._m2m_token_expiry: Optional[int] = None
        
    async def initialize(self):
        """Initialize the authentication service."""
        await self._load_public_key()
        
    async def _load_public_key(self):
        """Load the JWT public key or initialize JWKS client."""
        # Priority: 1. JWKS URL, 2. Public key content, 3. Public key file
        if self.jwt_settings.jwks_url:
            try:
                self._jwks_client = PyJWKClient(
                    uri=self.jwt_settings.jwks_url,
                    cache_keys=True,
                    max_cached_keys=16
                )
                _logger.info(f"JWKS client initialized with URL: {self.jwt_settings.jwks_url}")
                return
            except Exception as e:
                _logger.error(f"Failed to initialize JWKS client: {e}")
        
        # Fallback to static public key
        if self.jwt_settings.public_key_content:
            self._public_key = self.jwt_settings.public_key_content
            _logger.info("Using configured public key content")
        elif self.jwt_settings.public_key_path and os.path.exists(self.jwt_settings.public_key_path):
            async with aiofiles.open(self.jwt_settings.public_key_path, 'r') as f:
                self._public_key = await f.read()
            _logger.info(f"Loaded public key from file: {self.jwt_settings.public_key_path}")
        else:
            _logger.warning("No JWT public key or JWKS URL configured. JWT validation will fail.")
            
    def validate_token(self, token: str) -> UserClaims:
        """Validate JWT token and extract claims."""
        try:
            # Get the signing key
            signing_key = self._get_signing_key(token)
            
            payload = jwt.decode(
                token,
                signing_key,
                algorithms=[self.jwt_settings.algorithm],
                issuer=self.jwt_settings.issuer,
                audience=self.jwt_settings.audience
            )
            
            # Extract claims similar to .NET pattern
            scopes = payload.get("scope", "").split() if payload.get("scope") else []
            
            return UserClaims(
                user_id=payload.get("sub", ""),
                role=payload.get("role", ""),
                scopes=scopes,
                is_system_admin="system:admin" in scopes
            )
            
        except jwt.ExpiredSignatureError:
            raise HTTPException(status_code=401, detail="Token has expired")
        except jwt.InvalidTokenError as e:
            raise HTTPException(status_code=401, detail=f"Invalid token: {str(e)}")
        except Exception as e:
            _logger.error(f"Token validation error: {e}")
            raise HTTPException(status_code=401, detail="Token validation failed")
    
    def _get_signing_key(self, token: str) -> str:
        """Get the signing key for token validation."""
        if self._jwks_client:
            try:
                # Get the key ID from token header
                signing_key = self._jwks_client.get_signing_key_from_jwt(token)
                return signing_key.key
            except Exception as e:
                _logger.error(f"Failed to get signing key from JWKS: {e}")
                # Fall through to static key
        
        # Use static public key
        if self._public_key:
            return self._public_key
        
        raise HTTPException(status_code=500, detail="JWT public key not configured")
            
    async def get_m2m_token(self) -> str:
        """Get M2M token for service-to-service communication."""
        if not self.m2m_settings:
            raise ValueError("M2M settings not configured")
            
        # Check if current token is still valid
        if self._m2m_token and self._m2m_token_expiry:
            import time
            if time.time() < self._m2m_token_expiry:
                return self._m2m_token
                
        # Request new token
        async with httpx.AsyncClient() as client:
            try:
                # ASP.NET Core model binding es case-insensitive, pero usamos el formato del DTO
                # El DTO ClientCredentialsRequestDto tiene: ClientId, ClientSecret (PascalCase)
                url = f"{self.m2m_settings.auth_service_url}{self.m2m_settings.token_endpoint}"
                payload = {
                    "clientId": self.m2m_settings.client_id,
                    "clientSecret": self.m2m_settings.client_secret
                }

                _logger.info(f"Requesting M2M token from {url}")
                _logger.debug(f"Request payload: {{'clientId': '{self.m2m_settings.client_id}', 'clientSecret': '***'}}")

                response = await client.post(url, json=payload, timeout=10.0)

                if response.status_code != 200:
                    _logger.error(
                        f"Auth service returned {response.status_code}: {response.text[:300]}"
                    )

                response.raise_for_status()

                token_data = response.json()
                _logger.debug(f"Token response keys: {list(token_data.keys())}")

                # El auth-service retorna camelCase: accessToken, expiresAt
                # Intentar ambos formatos para compatibilidad
                self._m2m_token = (
                    token_data.get("accessToken") or
                    token_data.get("AccessToken") or
                    token_data.get("access_token")
                )

                if not self._m2m_token:
                    _logger.error(f"Token response structure: {token_data}")
                    raise ValueError(f"No access token in response. Keys: {list(token_data.keys())}")

                # Cache token with safety margin
                import time
                # Intentar leer expiresAt (camelCase) o ExpiresAt (PascalCase)
                expires_at_str = token_data.get("expiresAt") or token_data.get("ExpiresAt")

                if expires_at_str:
                    from datetime import datetime, timezone
                    # Manejar formatos: "2025-10-04T08:35:12Z" o "2025-10-04T08:35:12+00:00"
                    expires_at_str_clean = expires_at_str.replace('Z', '+00:00')
                    expires_at = datetime.fromisoformat(expires_at_str_clean)
                    expires_in = (expires_at - datetime.now(timezone.utc)).total_seconds()
                    _logger.debug(f"Token expires in {expires_in} seconds")
                else:
                    expires_in = 3600  # Fallback: 1 hora
                    _logger.warning("No expiresAt in response, using 1 hour default")

                self._m2m_token_expiry = time.time() + expires_in - 60  # 60 seconds safety margin
                
                _logger.info("M2M token obtained successfully")
                return self._m2m_token
                
            except httpx.HTTPStatusError as e:
                # Log detalles específicos del error HTTP
                _logger.error(
                    f"M2M token HTTP error: {e.response.status_code} - {e.response.text[:200]}"
                )
                raise HTTPException(
                    status_code=503,
                    detail=f"Auth service error: {e.response.status_code}"
                )
            except httpx.RequestError as e:
                _logger.error(f"Failed to connect to auth-service: {e}")
                raise HTTPException(status_code=503, detail="Authentication service unavailable")
            except ValueError as e:
                _logger.error(f"Invalid token response format: {e}")
                raise HTTPException(status_code=500, detail="Invalid auth response")
            except Exception as e:
                _logger.error(f"M2M token error: {type(e).__name__}: {e}")
                raise HTTPException(status_code=500, detail="Authentication error")


# Global authentication service instance
_auth_service: Optional[AuthenticationService] = None


def get_auth_service() -> AuthenticationService:
    """Get the global authentication service instance."""
    if _auth_service is None:
        raise RuntimeError("Authentication service not initialized")
    return _auth_service


async def initialize_auth_service():
    """Initialize the global authentication service."""
    global _auth_service
    
    # Build JWKS URL from auth service URL
    auth_service_url = os.getenv("M2M_AUTH_SERVICE_URL", "http://auth-service:8080")
    jwks_url = f"{auth_service_url}/api/auth/keys/public"
    
    jwt_settings = JwtSettings(
        issuer=os.getenv("JWT_ISSUER", "http://auth-service:8080"),
        audience=os.getenv("JWT_AUDIENCE", "hydroespinaca-services"),
        public_key_path=os.getenv("JWT_PUBLIC_KEY_PATH"),
        jwks_url=jwks_url
    )
    
    m2m_settings = None
    if all([
        os.getenv("M2M_CLIENT_ID"),
        os.getenv("M2M_CLIENT_SECRET"),
        os.getenv("M2M_AUTH_SERVICE_URL")
    ]):
        m2m_settings = M2MSettings(
            client_id=os.getenv("M2M_CLIENT_ID", ""),
            client_secret=os.getenv("M2M_CLIENT_SECRET", ""),
            auth_service_url=os.getenv("M2M_AUTH_SERVICE_URL", ""),
            token_endpoint=os.getenv("M2M_TOKEN_ENDPOINT", "/api/auth/token"),
            token_cache_duration_minutes=int(os.getenv("M2M_TOKEN_CACHE_DURATION_MINUTES", "50"))
        )
    
    _auth_service = AuthenticationService(jwt_settings, m2m_settings)
    await _auth_service.initialize()
    _logger.info("Authentication service initialized")


def get_current_user(credentials: HTTPAuthorizationCredentials = Security(security)) -> UserClaims:
    """FastAPI dependency to get current authenticated user."""
    if not credentials:
        raise HTTPException(status_code=401, detail="Authentication required")
        
    auth_service = get_auth_service()
    return auth_service.validate_token(credentials.credentials)


def require_scopes(*required_scopes: str):
    """Decorator factory to require specific scopes (similar to .NET Authorize policies)."""
    def scope_dependency(user: UserClaims = Depends(get_current_user)) -> UserClaims:
        if user.is_system_admin:
            return user  # System admin bypasses scope checks
            
        user_scopes = set(user.scopes)
        required_scopes_set = set(required_scopes)
        
        if not required_scopes_set.issubset(user_scopes):
            missing_scopes = required_scopes_set - user_scopes
            raise HTTPException(
                status_code=403, 
                detail=f"Insufficient permissions. Missing scopes: {', '.join(missing_scopes)}"
            )
        return user
    return scope_dependency


# Common scope requirements (similar to .NET PolicyNames)
class Scopes:
    """Fuzzy service authorization scopes."""
    
    # Fuzzy System scopes
    FUZZY_SYSTEM_READ = "fuzzy:system:read"
    FUZZY_SYSTEM_CREATE = "fuzzy:system:create"
    FUZZY_SYSTEM_UPDATE = "fuzzy:system:update"
    FUZZY_SYSTEM_DELETE = "fuzzy:system:delete"
    
    # Fuzzy Variable scopes
    FUZZY_VARIABLE_READ = "fuzzy:variable:read"
    FUZZY_VARIABLE_CREATE = "fuzzy:variable:create"
    FUZZY_VARIABLE_UPDATE = "fuzzy:variable:update"
    FUZZY_VARIABLE_DELETE = "fuzzy:variable:delete"
    
    # Fuzzy Rule scopes
    FUZZY_RULE_READ = "fuzzy:rule:read"
    FUZZY_RULE_CREATE = "fuzzy:rule:create"
    FUZZY_RULE_UPDATE = "fuzzy:rule:update"
    FUZZY_RULE_DELETE = "fuzzy:rule:delete"
    
    # Fuzzy Evaluation scopes
    FUZZY_EVALUATION_READ = "fuzzy:evaluation:read"
    FUZZY_EVALUATION_CREATE = "fuzzy:evaluation:create"
    
    # Sensor processing (M2M)
    SENSOR_PROCESS = "sensor:process"
    
    # System scopes
    SYSTEM_ADMIN = "system:admin"
    SYSTEM_HEALTH = "system:health"