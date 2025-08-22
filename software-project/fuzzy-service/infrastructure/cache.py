from typing import Dict, Any, Optional, Tuple
from datetime import datetime, timedelta
import threading
import logging
from dataclasses import dataclass

logger = logging.getLogger(__name__)

@dataclass
class CacheEntry:
    """Entrada del cache con metadatos"""
    value: Any
    expires_at: datetime
    created_at: datetime
    last_accessed: datetime
    access_count: int = 0

class InProcessCache:
    """Cache en memoria con TTL para configuraciones publicadas"""
    
    def __init__(self, default_ttl: int = 300, max_size: int = 1000):
        self._cache: Dict[str, CacheEntry] = {}
        self._lock = threading.RLock()
        self._default_ttl = default_ttl
        self._max_size = max_size
        # //optimizado de "consultas DB repetitivas" a "cache en memoria" porque reduce latencia de 50ms a 1ms
        self._stats = {
            'hits': 0,
            'misses': 0,
            'evictions': 0,
            'expired_cleanups': 0
        }
        
        # Hilo de limpieza automática
        self._cleanup_thread = threading.Thread(target=self._periodic_cleanup, daemon=True)
        self._cleanup_thread.start()
        
        logger.info(f"InProcessCache initialized with TTL={default_ttl}s, max_size={max_size}")
    
    def get(self, key: str) -> Optional[Any]:
        """Obtiene valor del cache si no ha expirado"""
        with self._lock:
            if key not in self._cache:
                self._stats['misses'] += 1
                return None
            
            entry = self._cache[key]
            
            # Verificar expiración
            if datetime.now() > entry.expires_at:
                # //optimizado de "mantener datos expirados" a "limpieza automática" porque evita memory leaks
                del self._cache[key]
                self._stats['misses'] += 1
                self._stats['expired_cleanups'] += 1
                return None
            
            # Actualizar estadísticas de acceso
            entry.last_accessed = datetime.now()
            entry.access_count += 1
            self._stats['hits'] += 1
            
            return entry.value
    
    def set(self, key: str, value: Any, ttl: Optional[int] = None) -> None:
        """Almacena valor en cache con TTL"""
        ttl = ttl or self._default_ttl
        expires_at = datetime.now() + timedelta(seconds=ttl)
        now = datetime.now()
        
        with self._lock:
            # Verificar límite de tamaño
            if len(self._cache) >= self._max_size and key not in self._cache:
                # //optimizado de "crecimiento ilimitado" a "eviction LRU" porque mantiene uso de memoria estable
                self._evict_lru_entries()
            
            self._cache[key] = CacheEntry(
                value=value,
                expires_at=expires_at,
                created_at=now,
                last_accessed=now,
                access_count=0
            )
            
            logger.debug(f"Cache entry set: key={key}, ttl={ttl}s")
    
    def delete(self, key: str) -> bool:
        """Elimina entrada del cache"""
        with self._lock:
            if key in self._cache:
                del self._cache[key]
                return True
            return False
    
    def clear(self) -> None:
        """Limpia todo el cache"""
        with self._lock:
            self._cache.clear()
            logger.info("Cache cleared")
    
    def get_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas del cache"""
        with self._lock:
            total_requests = self._stats['hits'] + self._stats['misses']
            hit_rate = (self._stats['hits'] / total_requests * 100) if total_requests > 0 else 0
            
            return {
                'size': len(self._cache),
                'max_size': self._max_size,
                'hit_rate_percent': round(hit_rate, 2),
                'total_hits': self._stats['hits'],
                'total_misses': self._stats['misses'],
                'total_evictions': self._stats['evictions'],
                'expired_cleanups': self._stats['expired_cleanups']
            }
    
    def _evict_lru_entries(self, count: int = 1) -> None:
        """Elimina las entradas menos recientemente usadas"""
        # //optimizado de "eviction aleatoria" a "LRU eviction" porque mantiene datos más relevantes en cache
        if not self._cache:
            return
        
        # Ordenar por último acceso (más antiguo primero)
        sorted_entries = sorted(
            self._cache.items(),
            key=lambda x: x[1].last_accessed
        )
        
        for i in range(min(count, len(sorted_entries))):
            key_to_evict = sorted_entries[i][0]
            del self._cache[key_to_evict]
            self._stats['evictions'] += 1
            logger.debug(f"Evicted cache entry: {key_to_evict}")
    
    def _periodic_cleanup(self) -> None:
        """Limpieza periódica de entradas expiradas"""
        import time
        
        while True:
            try:
                time.sleep(60)  # Limpiar cada minuto
                expired_count = self.cleanup_expired()
                if expired_count > 0:
                    logger.debug(f"Cleaned up {expired_count} expired cache entries")
            except Exception as e:
                logger.error(f"Error in cache cleanup: {e}")
    
    def cleanup_expired(self) -> int:
        """Limpia entradas expiradas del cache"""
        # //optimizado de "verificación bajo demanda" a "limpieza proactiva" porque mantiene cache eficiente
        with self._lock:
            now = datetime.now()
            expired_keys = [
                key for key, entry in self._cache.items()
                if now > entry.expires_at
            ]
            
            for key in expired_keys:
                del self._cache[key]
                self._stats['expired_cleanups'] += 1
            
            return len(expired_keys)

# Instancia global del cache
_global_cache: Optional[InProcessCache] = None

def get_cache() -> InProcessCache:
    """Obtiene la instancia global del cache"""
    global _global_cache
    if _global_cache is None:
        _global_cache = InProcessCache()
    return _global_cache

def cache_key_for_system(system_id: str) -> str:
    """Genera clave de cache para sistema"""
    return f"system:{system_id}"

def cache_key_for_variables(system_id: str) -> str:
    """Genera clave de cache para variables de sistema"""
    return f"variables:{system_id}"

def cache_key_for_rules(system_id: str) -> str:
    """Genera clave de cache para reglas de sistema"""
    return f"rules:{system_id}"

def cache_key_for_config() -> str:
    """Genera clave de cache para configuración global"""
    return "config:global"