"""
MongoDB database configuration for the Fuzzy Service (Infrastructure layer).
- Provides a singleton AsyncMongoClient and AsyncDatabase accessor functions
- Loads configuration from environment variables (supports .env via python-dotenv)
- Exposes lifecycle helpers to initialize and close the client
"""
from __future__ import annotations

import os
import logging
from dataclasses import dataclass
from typing import Optional, Dict, Any

from dotenv import load_dotenv
from pymongo import AsyncMongoClient

# Load environment variables from a .env file if present
load_dotenv()

_logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class MongoSettings:
    """Strongly-typed Mongo settings loaded from environment variables."""
    connection_string: str
    database: str
    max_pool_size: int = 20
    min_pool_size: int = 0
    server_selection_timeout_ms: int = 5000
    connect_timeout_ms: int = 5000
    socket_timeout_ms: int = 10000
    direct_connection: bool = False

    # Collection names (allow override via env)
    collection_systems: str = "fuzzy_systems"
    collection_variables: str = "fuzzy_variables"
    collection_terms: str = "fuzzy_terms"
    collection_rules: str = "fuzzy_rules"
    collection_routines: str = "fuzzy_routines"
    collection_evaluations: str = "fuzzy_evaluations"


_settings: Optional[MongoSettings] = None
_client: Optional[AsyncMongoClient] = None


def _get_env_bool(name: str, default: bool) -> bool:
    v = os.getenv(name)
    if v is None:
        return default
    return v.strip().lower() in {"1", "true", "t", "yes", "y"}


def get_settings() -> MongoSettings:
    global _settings
    if _settings is not None:
        return _settings

    # Allow FUZZY_* aliases for backwards/compatibility with existing .env
    connection_string = (
        os.getenv("MONGO_CONNECTION_STRING")
        or os.getenv("FUZZY_MONGO_CONNECTION_STRING")
        or "mongodb://localhost:27017"
    )
    database = (
        os.getenv("MONGO_DATABASE_NAME")
        or os.getenv("FUZZY_MONGO_DATABASE")
        or "fuzzy_dev"
    )

    # Numeric options with safe parsing
    def _int(name: str, default: int) -> int:
        try:
            return int(os.getenv(name, str(default)))
        except (TypeError, ValueError):
            return default

    # If a single FUZZY_MONGO_TIMEOUT is provided, use it as default for specific timeouts
    fuzzy_timeout = os.getenv("FUZZY_MONGO_TIMEOUT")
    default_server_sel = int(fuzzy_timeout) if fuzzy_timeout else 5000
    default_connect = int(fuzzy_timeout) if fuzzy_timeout else 5000

    settings = MongoSettings(
        connection_string=connection_string,
        database=database,
        max_pool_size=_int("MONGO_MAX_POOL_SIZE", 20),
        min_pool_size=_int("MONGO_MIN_POOL_SIZE", 0),
        server_selection_timeout_ms=_int("MONGO_SERVER_SELECTION_TIMEOUT_MS", default_server_sel),
        connect_timeout_ms=_int("MONGO_CONNECT_TIMEOUT_MS", default_connect),
        socket_timeout_ms=_int("MONGO_SOCKET_TIMEOUT_MS", 10000),
        direct_connection=_get_env_bool("MONGO_DIRECT_CONNECTION", False),
        collection_systems=os.getenv("COLLECTION_FUZZY_SYSTEMS", "fuzzy_systems"),
        collection_variables=os.getenv("COLLECTION_FUZZY_VARIABLES", "fuzzy_variables"),
        collection_terms=os.getenv("COLLECTION_FUZZY_TERMS", "fuzzy_terms"),
        collection_rules=os.getenv("COLLECTION_FUZZY_RULES", "fuzzy_rules"),
        collection_routines=os.getenv("COLLECTION_FUZZY_ROUTINES", "fuzzy_routines"),
        collection_evaluations=os.getenv("COLLECTION_FUZZY_EVALUATIONS", "fuzzy_evaluations"),
    )

    _logger.info(
        "MongoSettings loaded: database=%s, max_pool_size=%s, min_pool_size=%s, direct_connection=%s",
        settings.database,
        settings.max_pool_size,
        settings.min_pool_size,
        settings.direct_connection,
    )
    _settings = settings
    return settings


async def init_mongo() -> AsyncMongoClient:
    """Initialize the AsyncMongoClient singleton and optionally verify connectivity with a ping."""
    global _client
    if _client is not None:
        return _client

    s = get_settings()

    client = AsyncMongoClient(
        s.connection_string,
        maxPoolSize=s.max_pool_size,
        minPoolSize=s.min_pool_size,
        serverSelectionTimeoutMS=s.server_selection_timeout_ms,
        connectTimeoutMS=s.connect_timeout_ms,
        socketTimeoutMS=s.socket_timeout_ms,
        directConnection=s.direct_connection,
        appname="fuzzy-service",
        retryWrites=True,
        retryReads=True,
        # Removed tls parameter to avoid passing None; rely on connection string for TLS settings
    )

    # Optionally establish connection early to catch config issues fast.
    # In PyMongo's async API, you can explicitly connect with aconnect() or run a simple admin command.
    if _get_env_bool("MONGO_PING_ON_STARTUP", True):
        try:
            # Either of these works; using an admin ping is a simple health check.
            # await client.aconnect()  # alternative explicit connect
            await client.admin.command("ping")
            _logger.info("MongoDB connection established and ping successful.")
        except Exception as exc:
            _logger.exception("Failed to connect to MongoDB (ping): %s", exc)
            try:
                client.close()
            except Exception:
                pass
            raise
    else:
        _logger.info("Skipping MongoDB ping on startup (MONGO_PING_ON_STARTUP=false). Lazy connection will be used.")

    _client = client
    return _client


def get_client() -> AsyncMongoClient:
    if _client is None:
        raise RuntimeError("Mongo client not initialized. Call init_mongo() on startup.")
    return _client


def get_database():
    """Returns the configured AsyncDatabase instance."""
    client = get_client()
    return client[get_settings().database]


def get_collections() -> Dict[str, str]:
    """Returns a mapping of logical names to collection names."""
    s = get_settings()
    return {
        "systems": s.collection_systems,
        "variables": s.collection_variables,
        "terms": s.collection_terms,
        "rules": s.collection_rules,
        "routines": s.collection_routines,
        "evaluations": s.collection_evaluations,
    }


def get_collection(name: str):
    """Helper to get a collection by logical name or raw collection name."""
    db = get_database()
    mapping = get_collections()
    physical = mapping.get(name, name)  # allow passing raw collection names too
    return db[physical]


async def close_mongo() -> None:
    """Close the AsyncMongoClient singleton if initialized."""
    global _client
    if _client is not None:
        try:
            # AsyncMongoClient.close() is a coroutine and must be awaited
            await _client.close()
            _logger.info("MongoDB client closed.")
        finally:
            _client = None
