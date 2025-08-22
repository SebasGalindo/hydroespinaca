"""Configuración de la base de datos MongoDB.

Siguiendo el patrón de auth-service y sensor-service para mantener
consistencia en la arquitectura.
"""

import logging
from typing import Optional

from pymongo.asynchronous.mongo_client import AsyncMongoClient
from pymongo.asynchronous.database import AsyncDatabase
from pymongo.errors import ConnectionFailure, ServerSelectionTimeoutError

from infrastructure.config import settings


class MongoDatabase:
    """Gestor de conexión a MongoDB.
    
    Proporciona una conexión singleton a la base de datos MongoDB
    con manejo de errores y reconexión automática.
    """
    
    def __init__(self):
        self._client: Optional[AsyncMongoClient] = None
        self._database: Optional[AsyncDatabase] = None
        
    async def connect(self) -> None:
        """Establece la conexión a MongoDB."""
        try:
            self._client = AsyncMongoClient(
                settings.mongo_connection_string,
                serverSelectionTimeoutMS=settings.mongo_timeout
            )
            
            # Verificar conexión
            await self._client.admin.command('ping')
            
            self._database = self._client[settings.mongo_database]
            
            logging.info(
                "Conexión a MongoDB establecida: %s/%s",
                settings.mongo_connection_string,
                settings.mongo_database
            )
            
        except (ConnectionFailure, ServerSelectionTimeoutError) as e:
            logging.error("Error conectando a MongoDB: %s", str(e))
            raise
            
    async def disconnect(self) -> None:
        """Cierra la conexión a MongoDB."""
        if self._client:
            self._client.close()
            self._client = None
            self._database = None
            logging.info("Conexión a MongoDB cerrada")
            
    @property
    def database(self) -> AsyncDatabase:
        """Retorna la instancia de la base de datos."""
        if self._database is None:
            raise RuntimeError("Base de datos no inicializada. Llama a connect() primero.")
        return self._database
        
    async def health_check(self) -> bool:
        """Verifica el estado de la conexión a MongoDB."""
        try:
            if self._client is None:
                return False
                
            await self._client.admin.command('ping')
            return True
            
        except Exception as e:
            logging.warning("Health check de MongoDB falló: %s", str(e))
            return False


# Instancia singleton
mongo_db = MongoDatabase()