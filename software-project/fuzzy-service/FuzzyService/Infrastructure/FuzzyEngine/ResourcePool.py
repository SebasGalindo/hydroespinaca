from __future__ import annotations

import asyncio
import logging
import threading
import time
import weakref
from abc import ABC, abstractmethod
from concurrent.futures import ThreadPoolExecutor, Future
from contextlib import contextmanager, asynccontextmanager
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Dict, List, Optional, Any, Callable, TypeVar, Generic, Iterator, AsyncIterator
from collections import deque
import uuid

from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics


T = TypeVar('T')


class ResourceState(Enum):
    """Estados de un recurso en el pool."""
    AVAILABLE = "available"
    IN_USE = "in_use"
    MAINTENANCE = "maintenance"
    EXPIRED = "expired"
    ERROR = "error"


class PoolStrategy(Enum):
    """Estrategias de gestión del pool."""
    FIFO = "fifo"  # First In, First Out
    LIFO = "lifo"  # Last In, First Out
    LEAST_USED = "least_used"  # Menos utilizado
    ROUND_ROBIN = "round_robin"  # Rotación circular


@dataclass
class ResourceMetrics:
    """Métricas de uso de un recurso."""
    resource_id: str
    created_at: datetime
    last_used_at: datetime
    usage_count: int = 0
    total_usage_time_seconds: float = 0.0
    error_count: int = 0
    last_error_at: Optional[datetime] = None
    
    @property
    def avg_usage_time_seconds(self) -> float:
        """Tiempo promedio de uso."""
        return self.total_usage_time_seconds / max(1, self.usage_count)
    
    @property
    def age_seconds(self) -> float:
        """Edad del recurso en segundos."""
        return (datetime.now(timezone.utc) - self.created_at).total_seconds()
    
    @property
    def idle_time_seconds(self) -> float:
        """Tiempo inactivo en segundos."""
        return (datetime.now(timezone.utc) - self.last_used_at).total_seconds()


@dataclass
class PooledResource(Generic[T]):
    """Wrapper para un recurso en el pool."""
    resource_id: str
    resource: T
    state: ResourceState
    metrics: ResourceMetrics
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    expires_at: Optional[datetime] = None
    
    def mark_used(self) -> None:
        """Marca el recurso como usado."""
        self.metrics.usage_count += 1
        self.metrics.last_used_at = datetime.now(timezone.utc)
        self.state = ResourceState.IN_USE
    
    def mark_available(self, usage_time_seconds: float = 0.0) -> None:
        """Marca el recurso como disponible."""
        self.metrics.total_usage_time_seconds += usage_time_seconds
        self.state = ResourceState.AVAILABLE
    
    def mark_error(self, error: Exception) -> None:
        """Marca el recurso con error."""
        self.metrics.error_count += 1
        self.metrics.last_error_at = datetime.now(timezone.utc)
        self.state = ResourceState.ERROR
    
    def is_expired(self) -> bool:
        """Verifica si el recurso ha expirado."""
        if self.expires_at is None:
            return False
        return datetime.now(timezone.utc) >= self.expires_at
    
    def should_retire(self, max_age_seconds: float, max_usage_count: int) -> bool:
        """Determina si el recurso debe ser retirado."""
        return (self.metrics.age_seconds > max_age_seconds or 
                self.metrics.usage_count > max_usage_count or
                self.is_expired() or
                self.state == ResourceState.ERROR)


class IResourceFactory(ABC, Generic[T]):
    """Interfaz para factory de recursos."""
    
    @abstractmethod
    def create_resource(self) -> T:
        """Crea un nuevo recurso."""
        pass
    
    @abstractmethod
    def validate_resource(self, resource: T) -> bool:
        """Valida que un recurso esté en buen estado."""
        pass
    
    @abstractmethod
    def cleanup_resource(self, resource: T) -> None:
        """Limpia un recurso antes de destruirlo."""
        pass
    
    @abstractmethod
    def reset_resource(self, resource: T) -> bool:
        """Resetea un recurso para reutilización. Retorna True si fue exitoso."""
        pass


class ThreadPoolFactory(IResourceFactory[ThreadPoolExecutor]):
    """Factory para ThreadPoolExecutor."""
    
    def __init__(self, max_workers: int, thread_name_prefix: str = "FuzzyPool"):
        self.max_workers = max_workers
        self.thread_name_prefix = thread_name_prefix
    
    def create_resource(self) -> ThreadPoolExecutor:
        """Crea un nuevo ThreadPoolExecutor."""
        return ThreadPoolExecutor(
            max_workers=self.max_workers,
            thread_name_prefix=self.thread_name_prefix
        )
    
    def validate_resource(self, resource: ThreadPoolExecutor) -> bool:
        """Valida que el ThreadPoolExecutor esté funcional."""
        try:
            # Verificar que no esté shutdown
            if resource._shutdown:
                return False
            
            # Enviar una tarea simple para verificar funcionalidad
            future = resource.submit(lambda: True)
            result = future.result(timeout=1.0)
            return result is True
            
        except Exception:
            return False
    
    def cleanup_resource(self, resource: ThreadPoolExecutor) -> None:
        """Limpia el ThreadPoolExecutor."""
        try:
            resource.shutdown(wait=True, cancel_futures=True)
        except Exception:
            pass  # Ignorar errores durante cleanup
    
    def reset_resource(self, resource: ThreadPoolExecutor) -> bool:
        """Los ThreadPoolExecutor no se pueden resetear, siempre retorna False."""
        return False


class ResourcePool(Generic[T]):
    """Pool genérico de recursos reutilizables."""
    
    def __init__(self,
                 factory: IResourceFactory[T],
                 min_size: int = 1,
                 max_size: int = 10,
                 max_age_seconds: float = 3600.0,  # 1 hora
                 max_usage_count: int = 1000,
                 max_idle_seconds: float = 300.0,  # 5 minutos
                 strategy: PoolStrategy = PoolStrategy.FIFO,
                 logger: Optional[logging.Logger] = None):
        
        self.factory = factory
        self.min_size = min_size
        self.max_size = max_size
        self.max_age_seconds = max_age_seconds
        self.max_usage_count = max_usage_count
        self.max_idle_seconds = max_idle_seconds
        self.strategy = strategy
        self.logger = logger or logging.getLogger("ResourcePool")
        
        # Pool de recursos
        self.available_resources: deque[PooledResource[T]] = deque()
        self.in_use_resources: Dict[str, PooledResource[T]] = {}
        self.all_resources: Dict[str, PooledResource[T]] = {}
        
        # Locks para thread safety
        self.pool_lock = threading.RLock()
        
        # Estadísticas
        self.total_created = 0
        self.total_destroyed = 0
        self.total_acquisitions = 0
        self.total_releases = 0
        self.total_errors = 0
        
        # Tareas de mantenimiento
        self._maintenance_task: Optional[asyncio.Task] = None
        self._running = False
        
        # Inicializar pool mínimo
        self._initialize_pool()
        
        self.logger.info(
            f"ResourcePool inicializado: min={min_size}, max={max_size}, "
            f"strategy={strategy.value}"
        )
    
    def _initialize_pool(self) -> None:
        """Inicializa el pool con el tamaño mínimo."""
        with self.pool_lock:
            for _ in range(self.min_size):
                try:
                    resource = self._create_pooled_resource()
                    self.available_resources.append(resource)
                except Exception as e:
                    self.logger.error(f"Error inicializando recurso: {e}")
    
    def _create_pooled_resource(self) -> PooledResource[T]:
        """Crea un nuevo recurso pooled."""
        resource_id = str(uuid.uuid4())
        resource = self.factory.create_resource()
        
        metrics = ResourceMetrics(
            resource_id=resource_id,
            created_at=datetime.now(timezone.utc),
            last_used_at=datetime.now(timezone.utc)
        )
        
        pooled = PooledResource(
            resource_id=resource_id,
            resource=resource,
            state=ResourceState.AVAILABLE,
            metrics=metrics
        )
        
        self.all_resources[resource_id] = pooled
        self.total_created += 1
        
        self.logger.debug(f"Recurso creado: {resource_id}")
        return pooled
    
    @contextmanager
    def acquire_resource(self, timeout_seconds: float = 30.0) -> Iterator[T]:
        """Adquiere un recurso del pool (context manager síncrono)."""
        pooled_resource = None
        start_time = time.time()
        
        try:
            pooled_resource = self._acquire_pooled_resource(timeout_seconds)
            self.total_acquisitions += 1
            
            yield pooled_resource.resource
            
        except Exception as e:
            self.total_errors += 1
            if pooled_resource:
                pooled_resource.mark_error(e)
            raise
        finally:
            if pooled_resource:
                usage_time = time.time() - start_time
                self._release_pooled_resource(pooled_resource, usage_time)
    
    @asynccontextmanager
    async def acquire_resource_async(self, timeout_seconds: float = 30.0) -> AsyncIterator[T]:
        """Adquiere un recurso del pool (context manager asíncrono)."""
        pooled_resource = None
        start_time = time.time()
        
        try:
            pooled_resource = await self._acquire_pooled_resource_async(timeout_seconds)
            self.total_acquisitions += 1
            
            yield pooled_resource.resource
            
        except Exception as e:
            self.total_errors += 1
            if pooled_resource:
                pooled_resource.mark_error(e)
            raise
        finally:
            if pooled_resource:
                usage_time = time.time() - start_time
                self._release_pooled_resource(pooled_resource, usage_time)
    
    def _acquire_pooled_resource(self, timeout_seconds: float) -> PooledResource[T]:
        """Adquiere un recurso pooled (síncrono)."""
        start_time = time.time()
        
        while time.time() - start_time < timeout_seconds:
            with self.pool_lock:
                # Buscar recurso disponible
                pooled_resource = self._get_available_resource()
                
                if pooled_resource:
                    # Validar recurso
                    if self._validate_pooled_resource(pooled_resource):
                        pooled_resource.mark_used()
                        self.in_use_resources[pooled_resource.resource_id] = pooled_resource
                        return pooled_resource
                    else:
                        # Recurso inválido, destruir
                        self._destroy_pooled_resource(pooled_resource)
                
                # Crear nuevo recurso si es posible
                if len(self.all_resources) < self.max_size:
                    try:
                        pooled_resource = self._create_pooled_resource()
                        pooled_resource.mark_used()
                        self.in_use_resources[pooled_resource.resource_id] = pooled_resource
                        return pooled_resource
                    except Exception as e:
                        self.logger.error(f"Error creando recurso: {e}")
            
            # Esperar un poco antes de reintentar
            time.sleep(0.1)
        
        raise TimeoutError(f"No se pudo adquirir recurso en {timeout_seconds} segundos")
    
    async def _acquire_pooled_resource_async(self, timeout_seconds: float) -> PooledResource[T]:
        """Adquiere un recurso pooled (asíncrono)."""
        start_time = time.time()
        
        while time.time() - start_time < timeout_seconds:
            with self.pool_lock:
                # Buscar recurso disponible
                pooled_resource = self._get_available_resource()
                
                if pooled_resource:
                    # Validar recurso
                    if self._validate_pooled_resource(pooled_resource):
                        pooled_resource.mark_used()
                        self.in_use_resources[pooled_resource.resource_id] = pooled_resource
                        return pooled_resource
                    else:
                        # Recurso inválido, destruir
                        self._destroy_pooled_resource(pooled_resource)
                
                # Crear nuevo recurso si es posible
                if len(self.all_resources) < self.max_size:
                    try:
                        pooled_resource = self._create_pooled_resource()
                        pooled_resource.mark_used()
                        self.in_use_resources[pooled_resource.resource_id] = pooled_resource
                        return pooled_resource
                    except Exception as e:
                        self.logger.error(f"Error creando recurso: {e}")
            
            # Esperar un poco antes de reintentar
            await asyncio.sleep(0.1)
        
        raise TimeoutError(f"No se pudo adquirir recurso en {timeout_seconds} segundos")
    
    def _get_available_resource(self) -> Optional[PooledResource[T]]:
        """Obtiene un recurso disponible según la estrategia."""
        if not self.available_resources:
            return None
        
        if self.strategy == PoolStrategy.FIFO:
            return self.available_resources.popleft()
        elif self.strategy == PoolStrategy.LIFO:
            return self.available_resources.pop()
        elif self.strategy == PoolStrategy.LEAST_USED:
            # Encontrar el menos usado
            least_used = min(self.available_resources, key=lambda r: r.metrics.usage_count)
            self.available_resources.remove(least_used)
            return least_used
        elif self.strategy == PoolStrategy.ROUND_ROBIN:
            # Rotar al final
            resource = self.available_resources.popleft()
            return resource
        else:
            return self.available_resources.popleft()
    
    def _validate_pooled_resource(self, pooled_resource: PooledResource[T]) -> bool:
        """Valida un recurso pooled."""
        try:
            # Verificar si debe ser retirado
            if pooled_resource.should_retire(self.max_age_seconds, self.max_usage_count):
                return False
            
            # Validar con el factory
            return self.factory.validate_resource(pooled_resource.resource)
            
        except Exception as e:
            self.logger.error(f"Error validando recurso {pooled_resource.resource_id}: {e}")
            return False
    
    def _release_pooled_resource(self, pooled_resource: PooledResource[T], usage_time_seconds: float) -> None:
        """Libera un recurso pooled."""
        with self.pool_lock:
            try:
                # Remover de recursos en uso
                self.in_use_resources.pop(pooled_resource.resource_id, None)
                
                # Marcar como disponible
                pooled_resource.mark_available(usage_time_seconds)
                
                # Verificar si debe ser retirado
                if pooled_resource.should_retire(self.max_age_seconds, self.max_usage_count):
                    self._destroy_pooled_resource(pooled_resource)
                else:
                    # Intentar resetear el recurso
                    if self.factory.reset_resource(pooled_resource.resource):
                        self.available_resources.append(pooled_resource)
                    else:
                        # No se pudo resetear, destruir
                        self._destroy_pooled_resource(pooled_resource)
                
                self.total_releases += 1
                
            except Exception as e:
                self.logger.error(f"Error liberando recurso {pooled_resource.resource_id}: {e}")
                self._destroy_pooled_resource(pooled_resource)
    
    def _destroy_pooled_resource(self, pooled_resource: PooledResource[T]) -> None:
        """Destruye un recurso pooled."""
        try:
            # Cleanup del recurso
            self.factory.cleanup_resource(pooled_resource.resource)
            
            # Remover de todas las colecciones
            self.available_resources = deque(
                r for r in self.available_resources 
                if r.resource_id != pooled_resource.resource_id
            )
            self.in_use_resources.pop(pooled_resource.resource_id, None)
            self.all_resources.pop(pooled_resource.resource_id, None)
            
            self.total_destroyed += 1
            
            self.logger.debug(f"Recurso destruido: {pooled_resource.resource_id}")
            
        except Exception as e:
            self.logger.error(f"Error destruyendo recurso {pooled_resource.resource_id}: {e}")
    
    async def start_maintenance(self, interval_seconds: float = 60.0) -> None:
        """Inicia el mantenimiento automático del pool."""
        if self._running:
            return
        
        self._running = True
        self.logger.info("Iniciando mantenimiento automático del pool")
        
        self._maintenance_task = asyncio.create_task(
            self._maintenance_loop(interval_seconds)
        )
    
    async def stop_maintenance(self) -> None:
        """Detiene el mantenimiento automático."""
        if not self._running:
            return
        
        self._running = False
        self.logger.info("Deteniendo mantenimiento del pool")
        
        if self._maintenance_task:
            self._maintenance_task.cancel()
            try:
                await self._maintenance_task
            except asyncio.CancelledError:
                pass
    
    async def _maintenance_loop(self, interval_seconds: float) -> None:
        """Loop de mantenimiento del pool."""
        while self._running:
            try:
                await self._perform_maintenance()
                await asyncio.sleep(interval_seconds)
            except asyncio.CancelledError:
                break
            except Exception as e:
                self.logger.error(f"Error en mantenimiento del pool: {e}")
                await asyncio.sleep(5.0)
    
    async def _perform_maintenance(self) -> None:
        """Ejecuta tareas de mantenimiento."""
        with self.pool_lock:
            # Limpiar recursos expirados o inválidos
            expired_resources = [
                r for r in self.available_resources
                if (r.should_retire(self.max_age_seconds, self.max_usage_count) or
                    r.metrics.idle_time_seconds > self.max_idle_seconds)
            ]
            
            for resource in expired_resources:
                self._destroy_pooled_resource(resource)
            
            # Asegurar tamaño mínimo
            current_available = len(self.available_resources)
            if current_available < self.min_size:
                needed = self.min_size - current_available
                for _ in range(needed):
                    try:
                        resource = self._create_pooled_resource()
                        self.available_resources.append(resource)
                    except Exception as e:
                        self.logger.error(f"Error creando recurso en mantenimiento: {e}")
                        break
            
            self.logger.debug(
                f"Mantenimiento completado: disponibles={len(self.available_resources)}, "
                f"en_uso={len(self.in_use_resources)}, total={len(self.all_resources)}"
            )
    
    def get_pool_status(self) -> Dict[str, Any]:
        """Obtiene el estado del pool."""
        with self.pool_lock:
            available_count = len(self.available_resources)
            in_use_count = len(self.in_use_resources)
            total_count = len(self.all_resources)
            
            # Calcular métricas agregadas
            all_metrics = [r.metrics for r in self.all_resources.values()]
            
            avg_usage_count = sum(m.usage_count for m in all_metrics) / max(1, len(all_metrics))
            avg_age_seconds = sum(m.age_seconds for m in all_metrics) / max(1, len(all_metrics))
            total_errors = sum(m.error_count for m in all_metrics)
            
            return {
                'available_resources': available_count,
                'in_use_resources': in_use_count,
                'total_resources': total_count,
                'min_size': self.min_size,
                'max_size': self.max_size,
                'strategy': self.strategy.value,
                'maintenance_running': self._running,
                'statistics': {
                    'total_created': self.total_created,
                    'total_destroyed': self.total_destroyed,
                    'total_acquisitions': self.total_acquisitions,
                    'total_releases': self.total_releases,
                    'total_errors': self.total_errors,
                    'avg_usage_count': avg_usage_count,
                    'avg_age_seconds': avg_age_seconds,
                    'total_resource_errors': total_errors
                }
            }
    
    async def shutdown(self) -> None:
        """Cierra el pool y limpia todos los recursos."""
        self.logger.info("Cerrando ResourcePool")
        
        # Detener mantenimiento
        await self.stop_maintenance()
        
        with self.pool_lock:
            # Destruir todos los recursos
            all_resources_copy = list(self.all_resources.values())
            for resource in all_resources_copy:
                self._destroy_pooled_resource(resource)
            
            # Limpiar colecciones
            self.available_resources.clear()
            self.in_use_resources.clear()
            self.all_resources.clear()
        
        self.logger.info("ResourcePool cerrado")


class ThreadPoolManager:
    """Gestor especializado para pools de ThreadPoolExecutor."""
    
    def __init__(self,
                 config: FuzzyEngineConfiguration,
                 metrics: FuzzyEngineMetrics,
                 logger: Optional[logging.Logger] = None):
        
        self.config = config
        self.metrics = metrics
        self.logger = logger or logging.getLogger("ThreadPoolManager")
        
        # Crear factory para ThreadPoolExecutor
        self.factory = ThreadPoolFactory(
            max_workers=config.performance_limits.max_workers,
            thread_name_prefix="FuzzyEngine"
        )
        
        # Crear pool de ThreadPoolExecutor
        self.pool = ResourcePool(
            factory=self.factory,
            min_size=1,
            max_size=3,  # Máximo 3 ThreadPoolExecutor simultáneos
            max_age_seconds=1800.0,  # 30 minutos
            max_usage_count=500,
            max_idle_seconds=600.0,  # 10 minutos
            strategy=PoolStrategy.LEAST_USED,
            logger=self.logger
        )
        
        self.logger.info("ThreadPoolManager inicializado")
    
    @contextmanager
    def get_executor(self, timeout_seconds: float = 30.0) -> Iterator[ThreadPoolExecutor]:
        """Obtiene un ThreadPoolExecutor del pool."""
        with self.pool.acquire_resource(timeout_seconds) as executor:
            yield executor
    
    @asynccontextmanager
    async def get_executor_async(self, timeout_seconds: float = 30.0) -> AsyncIterator[ThreadPoolExecutor]:
        """Obtiene un ThreadPoolExecutor del pool (asíncrono)."""
        async with self.pool.acquire_resource_async(timeout_seconds) as executor:
            yield executor
    
    async def start(self) -> None:
        """Inicia el gestor de pools."""
        await self.pool.start_maintenance()
        self.logger.info("ThreadPoolManager iniciado")
    
    async def stop(self) -> None:
        """Detiene el gestor de pools."""
        await self.pool.shutdown()
        self.logger.info("ThreadPoolManager detenido")
    
    def get_status(self) -> Dict[str, Any]:
        """Obtiene el estado del gestor."""
        return {
            'thread_pool_manager': self.pool.get_pool_status(),
            'configuration': {
                'max_workers': self.config.performance_limits.max_workers,
                'parallel_threshold': self.config.performance_limits.parallel_threshold
            }
        }