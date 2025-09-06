from __future__ import annotations

import asyncio
import logging
import time
import uuid
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Any, Callable, Dict, List, Optional, Union, Awaitable
from concurrent.futures import ThreadPoolExecutor
import threading
from contextlib import asynccontextmanager

from .FuzzyEngineExceptions import FuzzyEngineException
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics


class TaskPriority(Enum):
    """Prioridades de tareas."""
    LOW = 1
    NORMAL = 2
    HIGH = 3
    CRITICAL = 4


class TaskStatus(Enum):
    """Estados de las tareas."""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"
    TIMEOUT = "timeout"


@dataclass
class TaskResult:
    """Resultado de una tarea."""
    task_id: str
    status: TaskStatus
    result: Any = None
    error: Optional[Exception] = None
    execution_time_ms: float = 0.0
    worker_id: Optional[str] = None
    completed_at: Optional[datetime] = None
    metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass
class AsyncTask:
    """Tarea asíncrona para procesamiento."""
    task_id: str
    func: Callable[..., Awaitable[Any]]
    args: tuple = field(default_factory=tuple)
    kwargs: Dict[str, Any] = field(default_factory=dict)
    priority: TaskPriority = TaskPriority.NORMAL
    timeout_seconds: Optional[float] = None
    retry_count: int = 0
    max_retries: int = 3
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    metadata: Dict[str, Any] = field(default_factory=dict)
    
    def __lt__(self, other: 'AsyncTask') -> bool:
        """Comparación para ordenamiento por prioridad."""
        return self.priority.value > other.priority.value


class ITaskQueue(ABC):
    """Interfaz para colas de tareas."""
    
    @abstractmethod
    async def put(self, task: AsyncTask) -> None:
        """Añade una tarea a la cola."""
        pass
    
    @abstractmethod
    async def get(self) -> AsyncTask:
        """Obtiene una tarea de la cola."""
        pass
    
    @abstractmethod
    def qsize(self) -> int:
        """Retorna el tamaño de la cola."""
        pass
    
    @abstractmethod
    def empty(self) -> bool:
        """Verifica si la cola está vacía."""
        pass


class PriorityTaskQueue(ITaskQueue):
    """Cola de tareas con prioridad."""
    
    def __init__(self, maxsize: int = 0):
        self._queue = asyncio.PriorityQueue(maxsize=maxsize)
        self._counter = 0
        self._lock = asyncio.Lock()
    
    async def put(self, task: AsyncTask) -> None:
        """Añade una tarea a la cola con prioridad."""
        async with self._lock:
            # Usar counter para mantener orden FIFO en misma prioridad
            priority_item = (task.priority.value, self._counter, task)
            self._counter += 1
            await self._queue.put(priority_item)
    
    async def get(self) -> AsyncTask:
        """Obtiene la tarea de mayor prioridad."""
        _, _, task = await self._queue.get()
        return task
    
    def qsize(self) -> int:
        """Retorna el tamaño de la cola."""
        return self._queue.qsize()
    
    def empty(self) -> bool:
        """Verifica si la cola está vacía."""
        return self._queue.empty()


class AsyncTaskWorker:
    """Worker para procesar tareas asíncronas."""
    
    def __init__(self, 
                 worker_id: str,
                 task_queue: ITaskQueue,
                 result_callback: Optional[Callable[[TaskResult], Awaitable[None]]] = None,
                 metrics: Optional[FuzzyEngineMetrics] = None,
                 logger: Optional[logging.Logger] = None):
        self.worker_id = worker_id
        self.task_queue = task_queue
        self.result_callback = result_callback
        self.metrics = metrics
        self.logger = logger or logging.getLogger(f"AsyncTaskWorker-{worker_id}")
        
        self._running = False
        self._current_task: Optional[AsyncTask] = None
        self._stats = {
            'tasks_processed': 0,
            'tasks_completed': 0,
            'tasks_failed': 0,
            'total_execution_time_ms': 0.0,
            'started_at': None,
            'last_task_at': None
        }
        self._stats_lock = threading.Lock()
    
    async def start(self) -> None:
        """Inicia el worker."""
        if self._running:
            return
        
        self._running = True
        with self._stats_lock:
            self._stats['started_at'] = datetime.now(timezone.utc)
        
        self.logger.info(f"Worker {self.worker_id} iniciado")
        
        try:
            while self._running:
                try:
                    # Obtener tarea con timeout
                    task = await asyncio.wait_for(
                        self.task_queue.get(),
                        timeout=1.0  # Timeout para permitir shutdown graceful
                    )
                    
                    await self._process_task(task)
                    
                except asyncio.TimeoutError:
                    # Timeout normal, continuar
                    continue
                except Exception as e:
                    self.logger.error(f"Error inesperado en worker {self.worker_id}: {e}")
                    await asyncio.sleep(0.1)  # Breve pausa antes de continuar
        
        except asyncio.CancelledError:
            self.logger.info(f"Worker {self.worker_id} cancelado")
        finally:
            self.logger.info(f"Worker {self.worker_id} detenido")
    
    async def stop(self) -> None:
        """Detiene el worker."""
        self._running = False
        
        # Si hay una tarea en ejecución, esperar a que termine
        if self._current_task:
            self.logger.info(f"Worker {self.worker_id} esperando tarea actual...")
            # Dar tiempo para que termine la tarea actual
            await asyncio.sleep(0.1)
    
    async def _process_task(self, task: AsyncTask) -> None:
        """Procesa una tarea individual."""
        self._current_task = task
        start_time = time.perf_counter()
        
        with self._stats_lock:
            self._stats['tasks_processed'] += 1
            self._stats['last_task_at'] = datetime.now(timezone.utc)
        
        self.logger.debug(f"Worker {self.worker_id} procesando tarea {task.task_id}")
        
        result = TaskResult(
            task_id=task.task_id,
            status=TaskStatus.RUNNING,
            worker_id=self.worker_id
        )
        
        try:
            # Ejecutar tarea con timeout si está especificado
            if task.timeout_seconds:
                task_result = await asyncio.wait_for(
                    task.func(*task.args, **task.kwargs),
                    timeout=task.timeout_seconds
                )
            else:
                task_result = await task.func(*task.args, **task.kwargs)
            
            # Tarea completada exitosamente
            execution_time = (time.perf_counter() - start_time) * 1000
            result.status = TaskStatus.COMPLETED
            result.result = task_result
            result.execution_time_ms = execution_time
            result.completed_at = datetime.now(timezone.utc)
            
            with self._stats_lock:
                self._stats['tasks_completed'] += 1
                self._stats['total_execution_time_ms'] += execution_time
            
            if self.metrics:
                self.metrics.record_task_completion(execution_time)
            
            self.logger.debug(
                f"Worker {self.worker_id} completó tarea {task.task_id} "
                f"en {execution_time:.2f}ms"
            )
        
        except asyncio.TimeoutError:
            execution_time = (time.perf_counter() - start_time) * 1000
            result.status = TaskStatus.TIMEOUT
            result.error = TimeoutError(f"Tarea {task.task_id} excedió timeout de {task.timeout_seconds}s")
            result.execution_time_ms = execution_time
            result.completed_at = datetime.now(timezone.utc)
            
            with self._stats_lock:
                self._stats['tasks_failed'] += 1
            
            self.logger.warning(
                f"Worker {self.worker_id} timeout en tarea {task.task_id} "
                f"después de {execution_time:.2f}ms"
            )
        
        except Exception as e:
            execution_time = (time.perf_counter() - start_time) * 1000
            result.status = TaskStatus.FAILED
            result.error = e
            result.execution_time_ms = execution_time
            result.completed_at = datetime.now(timezone.utc)
            
            with self._stats_lock:
                self._stats['tasks_failed'] += 1
            
            self.logger.error(
                f"Worker {self.worker_id} falló tarea {task.task_id}: {e}",
                exc_info=True
            )
        
        finally:
            self._current_task = None
            
            # Llamar callback si está configurado
            if self.result_callback:
                try:
                    await self.result_callback(result)
                except Exception as e:
                    self.logger.error(f"Error en callback de resultado: {e}")
    
    def get_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas del worker."""
        with self._stats_lock:
            stats = self._stats.copy()
        
        if stats['tasks_processed'] > 0:
            stats['average_execution_time_ms'] = (
                stats['total_execution_time_ms'] / stats['tasks_processed']
            )
            stats['success_rate'] = (
                stats['tasks_completed'] / stats['tasks_processed']
            )
        else:
            stats['average_execution_time_ms'] = 0.0
            stats['success_rate'] = 0.0
        
        stats['current_task_id'] = self._current_task.task_id if self._current_task else None
        stats['is_running'] = self._running
        
        return stats


class AsyncTaskManager:
    """Gestor de tareas asíncronas con workers."""
    
    def __init__(self, 
                 config: FuzzyEngineConfiguration,
                 metrics: Optional[FuzzyEngineMetrics] = None,
                 logger: Optional[logging.Logger] = None):
        self.config = config
        self.metrics = metrics
        self.logger = logger or logging.getLogger("AsyncTaskManager")
        
        # Configuración de workers
        self.max_workers = 4  # Default async workers
        self.queue_size = 1000  # Default queue size
        
        # Cola de tareas y workers
        self.task_queue = PriorityTaskQueue(maxsize=self.queue_size)
        self.workers: List[AsyncTaskWorker] = []
        self.worker_tasks: List[asyncio.Task] = []
        
        # Resultados y callbacks
        self.result_callbacks: List[Callable[[TaskResult], Awaitable[None]]] = []
        self.task_results: Dict[str, TaskResult] = {}
        self.result_lock = asyncio.Lock()
        
        # Estado del manager
        self._running = False
        self._shutdown_event = asyncio.Event()
        
        # Estadísticas globales
        self._global_stats = {
            'total_tasks_submitted': 0,
            'total_tasks_completed': 0,
            'total_tasks_failed': 0,
            'queue_high_watermark': 0,
            'started_at': None
        }
        self._stats_lock = threading.Lock()
    
    async def start(self) -> None:
        """Inicia el gestor de tareas."""
        if self._running:
            return
        
        self._running = True
        with self._stats_lock:
            self._global_stats['started_at'] = datetime.now(timezone.utc)
        
        self.logger.info(f"Iniciando AsyncTaskManager con {self.max_workers} workers")
        
        # Crear y iniciar workers
        for i in range(self.max_workers):
            worker_id = f"worker-{i+1}"
            worker = AsyncTaskWorker(
                worker_id=worker_id,
                task_queue=self.task_queue,
                result_callback=self._handle_task_result,
                metrics=self.metrics,
                logger=self.logger.getChild(worker_id)
            )
            
            self.workers.append(worker)
            
            # Crear tarea para el worker
            worker_task = asyncio.create_task(worker.start())
            self.worker_tasks.append(worker_task)
        
        self.logger.info(f"AsyncTaskManager iniciado con {len(self.workers)} workers")
    
    async def stop(self, timeout: float = 30.0) -> None:
        """Detiene el gestor de tareas."""
        if not self._running:
            return
        
        self.logger.info("Deteniendo AsyncTaskManager...")
        self._running = False
        
        # Detener workers
        for worker in self.workers:
            await worker.stop()
        
        # Cancelar tareas de workers con timeout
        try:
            await asyncio.wait_for(
                asyncio.gather(*self.worker_tasks, return_exceptions=True),
                timeout=timeout
            )
        except asyncio.TimeoutError:
            self.logger.warning("Timeout deteniendo workers, cancelando...")
            for task in self.worker_tasks:
                task.cancel()
        
        self._shutdown_event.set()
        self.logger.info("AsyncTaskManager detenido")
    
    async def submit_task(self, 
                         task_func: Optional[Callable[..., Awaitable[Any]]] = None,
                         args: Optional[tuple] = None,
                         priority: TaskPriority = TaskPriority.NORMAL,
                         timeout_seconds: Optional[float] = None,
                         max_retries: int = 3,
                         task_id: Optional[str] = None,
                         metadata: Optional[Dict[str, Any]] = None,
                         **kwargs) -> Awaitable[TaskResult]:
        """Envía una tarea para procesamiento asíncrono y retorna un awaitable del resultado.
        
        Parámetros compatibles con tests:
        - task_func: callable async a ejecutar (alias soportado: func)
        - args: tupla de argumentos posicionales para la tarea
        - priority, timeout_seconds, max_retries, task_id, metadata: configuración de la tarea
        - **kwargs: argumentos nombrados para la ejecución de la tarea
        
        Retorna:
        - Awaitable[TaskResult]: objeto awaitable que resuelve con el resultado de la tarea
        """
        if not self._running:
            raise FuzzyEngineException("AsyncTaskManager no está ejecutándose")
        
        # Compatibilidad: permitir 'func' como alias de 'task_func'
        func = task_func or kwargs.pop('func', None)
        if func is None:
            raise TypeError("submit_task() requires 'task_func' (or 'func') argument")
        
        # Normalizar args
        task_args: tuple = ()
        if args is None:
            task_args = ()
        elif isinstance(args, tuple):
            task_args = args
        else:
            # Compatibilidad: si pasan un solo arg no-tuple
            task_args = (args,)
        
        if not task_id:
            task_id = str(uuid.uuid4())
        
        task = AsyncTask(
            task_id=task_id,
            func=func,
            args=task_args,
            kwargs=kwargs,
            priority=priority,
            timeout_seconds=timeout_seconds,
            max_retries=max_retries,
            metadata=metadata or {}
        )
        
        await self.task_queue.put(task)
        
        with self._stats_lock:
            self._global_stats['total_tasks_submitted'] += 1
            current_queue_size = self.task_queue.qsize()
            if current_queue_size > self._global_stats['queue_high_watermark']:
                self._global_stats['queue_high_watermark'] = current_queue_size
        
        self.logger.debug(f"Tarea {task_id} enviada con prioridad {priority.name}")
        
        # Devolver awaitable del resultado para coincidir con la expectativa de los tests
        return asyncio.create_task(self.get_task_result(task_id, timeout=timeout_seconds))
    
    async def get_task_result(self, task_id: str, timeout: Optional[float] = None) -> TaskResult:
        """Obtiene el resultado de una tarea."""
        start_time = time.time()
        
        while True:
            async with self.result_lock:
                if task_id in self.task_results:
                    return self.task_results.pop(task_id)
            
            if timeout and (time.time() - start_time) > timeout:
                raise asyncio.TimeoutError(f"Timeout esperando resultado de tarea {task_id}")
            
            await asyncio.sleep(0.01)  # Breve pausa
    
    def add_result_callback(self, callback: Callable[[TaskResult], Awaitable[None]]) -> None:
        """Añade un callback para resultados de tareas."""
        self.result_callbacks.append(callback)
    
    async def _handle_task_result(self, result: TaskResult) -> None:
        """Maneja el resultado de una tarea."""
        async with self.result_lock:
            self.task_results[result.task_id] = result
        
        with self._stats_lock:
            if result.status == TaskStatus.COMPLETED:
                self._global_stats['total_tasks_completed'] += 1
            elif result.status in [TaskStatus.FAILED, TaskStatus.TIMEOUT]:
                self._global_stats['total_tasks_failed'] += 1
        
        # Ejecutar callbacks
        for callback in self.result_callbacks:
            try:
                await callback(result)
            except Exception as e:
                self.logger.error(f"Error en callback de resultado: {e}")
    
    def get_status(self) -> Dict[str, Any]:
        """Obtiene el estado del gestor de tareas."""
        with self._stats_lock:
            global_stats = self._global_stats.copy()
        
        worker_stats = [worker.get_stats() for worker in self.workers]
        
        return {
            'running': self._running,
            'queue_size': self.task_queue.qsize(),
            'max_queue_size': self.queue_size,
            'active_workers': len([w for w in self.workers if w._running]),
            'total_workers': len(self.workers),
            'global_stats': global_stats,
            'worker_stats': worker_stats
        }
    
    @asynccontextmanager
    async def managed_lifecycle(self):
        """Context manager para gestión automática del ciclo de vida."""
        await self.start()
        try:
            yield self
        finally:
            await self.stop()