from __future__ import annotations

import asyncio
import logging
import time
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Any, Dict, List, Optional, Callable, Awaitable
import threading
from collections import defaultdict, deque

from .AsyncTaskQueue import AsyncTask, TaskPriority, TaskResult, AsyncTaskManager
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration
from .FuzzyEngineMetrics import FuzzyEngineMetrics


class LoadBalancingStrategy(Enum):
    """Estrategias de balanceeo de carga."""
    ROUND_ROBIN = "round_robin"
    LEAST_LOADED = "least_loaded"
    WEIGHTED_ROUND_ROBIN = "weighted_round_robin"
    ADAPTIVE = "adaptive"


class WorkloadType(Enum):
    """Tipos de carga de trabajo."""
    CPU_INTENSIVE = "cpu_intensive"
    IO_INTENSIVE = "io_intensive"
    MEMORY_INTENSIVE = "memory_intensive"
    MIXED = "mixed"


@dataclass
class WorkerLoad:
    """Información de carga de un worker."""
    worker_id: str
    current_tasks: int = 0
    total_tasks_processed: int = 0
    average_execution_time_ms: float = 0.0
    success_rate: float = 1.0
    last_task_completed_at: Optional[datetime] = None
    cpu_usage_percent: float = 0.0
    memory_usage_mb: float = 0.0
    weight: float = 1.0
    is_healthy: bool = True
    
    def get_load_score(self) -> float:
        """Calcula un score de carga (menor es mejor)."""
        if not self.is_healthy:
            return float('inf')
        
        # Factores de carga
        task_factor = self.current_tasks * 10
        time_factor = self.average_execution_time_ms / 1000
        failure_factor = (1 - self.success_rate) * 100
        cpu_factor = self.cpu_usage_percent / 10
        memory_factor = self.memory_usage_mb / 100
        
        return (task_factor + time_factor + failure_factor + cpu_factor + memory_factor) / self.weight


@dataclass
class LoadBalancerConfig:
    """Configuración del balanceador de carga."""
    strategy: LoadBalancingStrategy = LoadBalancingStrategy.ADAPTIVE
    health_check_interval_seconds: float = 30.0
    load_update_interval_seconds: float = 5.0
    unhealthy_threshold_failures: int = 5
    recovery_threshold_successes: int = 3
    adaptive_rebalance_threshold: float = 0.3  # 30% diferencia para rebalancear
    max_queue_size_per_worker: int = 100
    enable_circuit_breaker: bool = True


class ILoadBalancer(ABC):
    """Interfaz para balanceadores de carga."""
    
    @abstractmethod
    async def select_worker(self, task: AsyncTask) -> str:
        """Selecciona el worker más apropiado para una tarea."""
        pass
    
    @abstractmethod
    async def update_worker_load(self, worker_id: str, load_info: Dict[str, Any]) -> None:
        """Actualiza la información de carga de un worker."""
        pass
    
    @abstractmethod
    def get_load_distribution(self) -> Dict[str, WorkerLoad]:
        """Obtiene la distribución de carga actual."""
        pass


class AdaptiveLoadBalancer(ILoadBalancer):
    """Balanceador de carga adaptativo."""
    
    def __init__(self, 
                 config: LoadBalancerConfig,
                 worker_ids: List[str],
                 metrics: Optional[FuzzyEngineMetrics] = None,
                 logger: Optional[logging.Logger] = None):
        self.config = config
        self.worker_ids = worker_ids
        self.metrics = metrics
        self.logger = logger or logging.getLogger("AdaptiveLoadBalancer")
        
        # Estado de workers
        self.worker_loads: Dict[str, WorkerLoad] = {
            worker_id: WorkerLoad(worker_id=worker_id)
            for worker_id in worker_ids
        }
        
        # Estrategias de balanceeo
        self.current_strategy = config.strategy
        self.round_robin_index = 0
        
        # Historial de rendimiento
        self.performance_history: Dict[str, deque] = defaultdict(lambda: deque(maxlen=100))
        
        # Locks para thread safety
        self.load_lock = threading.RLock()
        
        # Tareas de monitoreo
        self._monitoring_tasks: List[asyncio.Task] = []
        self._running = False
    
    async def start_monitoring(self) -> None:
        """Inicia el monitoreo de carga."""
        if self._running:
            return
        
        self._running = True
        self.logger.info("Iniciando monitoreo de balanceador de carga")
        
        # Tarea de actualización de carga
        load_task = asyncio.create_task(self._load_monitoring_loop())
        self._monitoring_tasks.append(load_task)
        
        # Tarea de health check
        health_task = asyncio.create_task(self._health_check_loop())
        self._monitoring_tasks.append(health_task)
    
    async def stop_monitoring(self) -> None:
        """Detiene el monitoreo de carga."""
        if not self._running:
            return
        
        self._running = False
        self.logger.info("Deteniendo monitoreo de balanceador de carga")
        
        # Cancelar tareas de monitoreo
        for task in self._monitoring_tasks:
            task.cancel()
        
        try:
            await asyncio.gather(*self._monitoring_tasks, return_exceptions=True)
        except Exception as e:
            self.logger.error(f"Error deteniendo monitoreo: {e}")
        
        self._monitoring_tasks.clear()
    
    async def select_worker(self, task: AsyncTask) -> str:
        """Selecciona el worker más apropiado para una tarea."""
        with self.load_lock:
            healthy_workers = [
                worker_id for worker_id, load in self.worker_loads.items()
                if load.is_healthy
            ]
            
            if not healthy_workers:
                # Si no hay workers saludables, usar el menos cargado
                self.logger.warning("No hay workers saludables, usando el menos cargado")
                healthy_workers = list(self.worker_loads.keys())
            
            if len(healthy_workers) == 1:
                return healthy_workers[0]
            
            # Seleccionar según estrategia
            if self.current_strategy == LoadBalancingStrategy.ROUND_ROBIN:
                return self._select_round_robin(healthy_workers)
            elif self.current_strategy == LoadBalancingStrategy.LEAST_LOADED:
                return self._select_least_loaded(healthy_workers)
            elif self.current_strategy == LoadBalancingStrategy.WEIGHTED_ROUND_ROBIN:
                return self._select_weighted_round_robin(healthy_workers)
            elif self.current_strategy == LoadBalancingStrategy.ADAPTIVE:
                return self._select_adaptive(healthy_workers, task)
            else:
                return self._select_least_loaded(healthy_workers)
    
    def _select_round_robin(self, workers: List[str]) -> str:
        """Selección round robin."""
        worker = workers[self.round_robin_index % len(workers)]
        self.round_robin_index += 1
        return worker
    
    def _select_least_loaded(self, workers: List[str]) -> str:
        """Selección por menor carga."""
        return min(workers, key=lambda w: self.worker_loads[w].get_load_score())
    
    def _select_weighted_round_robin(self, workers: List[str]) -> str:
        """Selección round robin ponderada."""
        # Calcular pesos inversos (mayor peso = menor carga)
        weights = []
        for worker_id in workers:
            load = self.worker_loads[worker_id]
            # Peso inverso al score de carga
            weight = 1.0 / max(load.get_load_score(), 0.1)
            weights.append(weight)
        
        # Seleccionar basado en pesos
        total_weight = sum(weights)
        if total_weight == 0:
            return workers[0]
        
        # Usar round robin ponderado simple
        normalized_weights = [w / total_weight for w in weights]
        cumulative = 0
        target = (self.round_robin_index % 100) / 100.0
        
        for i, weight in enumerate(normalized_weights):
            cumulative += weight
            if target <= cumulative:
                self.round_robin_index += 1
                return workers[i]
        
        return workers[-1]
    
    def _select_adaptive(self, workers: List[str], task: AsyncTask) -> str:
        """Selección adaptativa basada en tipo de tarea y rendimiento."""
        # Determinar tipo de workload de la tarea
        workload_type = self._determine_workload_type(task)
        
        # Obtener workers especializados para este tipo de workload
        specialized_workers = self._get_specialized_workers(workers, workload_type)
        
        if specialized_workers:
            # Usar workers especializados
            return self._select_least_loaded(specialized_workers)
        else:
            # Usar selección por menor carga
            return self._select_least_loaded(workers)
    
    def _determine_workload_type(self, task: AsyncTask) -> WorkloadType:
        """Determina el tipo de workload de una tarea."""
        # Usar metadata de la tarea si está disponible
        if 'workload_type' in task.metadata:
            return WorkloadType(task.metadata['workload_type'])
        
        # Heurísticas basadas en el nombre de la función
        func_name = task.func.__name__.lower()
        
        if any(keyword in func_name for keyword in ['fuzzify', 'defuzzify', 'evaluate']):
            return WorkloadType.CPU_INTENSIVE
        elif any(keyword in func_name for keyword in ['io', 'read', 'write', 'fetch']):
            return WorkloadType.IO_INTENSIVE
        elif any(keyword in func_name for keyword in ['cache', 'memory', 'store']):
            return WorkloadType.MEMORY_INTENSIVE
        else:
            return WorkloadType.MIXED
    
    def _get_specialized_workers(self, workers: List[str], workload_type: WorkloadType) -> List[str]:
        """Obtiene workers especializados para un tipo de workload."""
        # Por ahora, todos los workers manejan todos los tipos
        # En el futuro se podría implementar especialización
        return workers
    
    async def update_worker_load(self, worker_id: str, load_info: Dict[str, Any]) -> None:
        """Actualiza la información de carga de un worker."""
        with self.load_lock:
            if worker_id not in self.worker_loads:
                self.worker_loads[worker_id] = WorkerLoad(worker_id=worker_id)
            
            load = self.worker_loads[worker_id]
            
            # Actualizar métricas
            load.current_tasks = load_info.get('current_tasks', 0)
            load.total_tasks_processed = load_info.get('tasks_processed', 0)
            load.average_execution_time_ms = load_info.get('average_execution_time_ms', 0.0)
            load.success_rate = load_info.get('success_rate', 1.0)
            
            if 'last_task_at' in load_info and load_info['last_task_at']:
                load.last_task_completed_at = load_info['last_task_at']
            
            # Actualizar estado de salud
            self._update_worker_health(worker_id, load_info)
            
            # Registrar en historial
            self.performance_history[worker_id].append({
                'timestamp': datetime.now(timezone.utc),
                'load_score': load.get_load_score(),
                'success_rate': load.success_rate,
                'execution_time': load.average_execution_time_ms
            })
    
    def _update_worker_health(self, worker_id: str, load_info: Dict[str, Any]) -> None:
        """Actualiza el estado de salud de un worker."""
        load = self.worker_loads[worker_id]
        
        # Criterios de salud
        is_responsive = load_info.get('is_running', True)
        has_reasonable_success_rate = load.success_rate >= 0.8
        has_reasonable_response_time = load.average_execution_time_ms < 10000  # 10s
        
        # Determinar si está saludable
        is_healthy = is_responsive and has_reasonable_success_rate and has_reasonable_response_time
        
        if load.is_healthy != is_healthy:
            load.is_healthy = is_healthy
            status = "saludable" if is_healthy else "no saludable"
            self.logger.info(f"Worker {worker_id} ahora está {status}")
    
    async def _load_monitoring_loop(self) -> None:
        """Loop de monitoreo de carga."""
        while self._running:
            try:
                await self._analyze_load_distribution()
                await asyncio.sleep(self.config.load_update_interval_seconds)
            except asyncio.CancelledError:
                break
            except Exception as e:
                self.logger.error(f"Error en monitoreo de carga: {e}")
                await asyncio.sleep(1.0)
    
    async def _health_check_loop(self) -> None:
        """Loop de health check."""
        while self._running:
            try:
                await self._perform_health_checks()
                await asyncio.sleep(self.config.health_check_interval_seconds)
            except asyncio.CancelledError:
                break
            except Exception as e:
                self.logger.error(f"Error en health check: {e}")
                await asyncio.sleep(1.0)
    
    async def _analyze_load_distribution(self) -> None:
        """Analiza la distribución de carga y ajusta estrategia si es necesario."""
        with self.load_lock:
            if len(self.worker_loads) < 2:
                return
            
            # Calcular estadísticas de carga
            load_scores = [load.get_load_score() for load in self.worker_loads.values() if load.is_healthy]
            
            if not load_scores:
                return
            
            avg_load = sum(load_scores) / len(load_scores)
            max_load = max(load_scores)
            min_load = min(load_scores)
            
            # Calcular desbalance
            if avg_load > 0:
                load_imbalance = (max_load - min_load) / avg_load
            else:
                load_imbalance = 0
            
            # Ajustar estrategia si hay desbalance significativo
            if load_imbalance > self.config.adaptive_rebalance_threshold:
                if self.current_strategy != LoadBalancingStrategy.LEAST_LOADED:
                    self.logger.info(
                        f"Desbalance detectado ({load_imbalance:.2f}), "
                        f"cambiando a estrategia LEAST_LOADED"
                    )
                    self.current_strategy = LoadBalancingStrategy.LEAST_LOADED
            else:
                # Volver a estrategia adaptativa si el desbalance es bajo
                if self.current_strategy != self.config.strategy:
                    self.logger.info("Carga balanceada, volviendo a estrategia configurada")
                    self.current_strategy = self.config.strategy
    
    async def _perform_health_checks(self) -> None:
        """Realiza health checks en los workers."""
        current_time = datetime.now(timezone.utc)
        
        with self.load_lock:
            for worker_id, load in self.worker_loads.items():
                # Verificar si el worker ha estado inactivo por mucho tiempo
                if load.last_task_completed_at:
                    time_since_last_task = (current_time - load.last_task_completed_at).total_seconds()
                    
                    # Si ha estado inactivo por más de 5 minutos, considerarlo potencialmente problemático
                    if time_since_last_task > 300 and load.current_tasks > 0:
                        self.logger.warning(
                            f"Worker {worker_id} puede estar bloqueado: "
                            f"{time_since_last_task:.0f}s desde última tarea completada"
                        )
    
    def get_load_distribution(self) -> Dict[str, WorkerLoad]:
        """Obtiene la distribución de carga actual."""
        with self.load_lock:
            return {worker_id: load for worker_id, load in self.worker_loads.items()}
    
    def get_statistics(self) -> Dict[str, Any]:
        """Obtiene estadísticas del balanceador."""
        with self.load_lock:
            healthy_workers = sum(1 for load in self.worker_loads.values() if load.is_healthy)
            total_workers = len(self.worker_loads)
            
            load_scores = [load.get_load_score() for load in self.worker_loads.values() if load.is_healthy]
            
            if load_scores:
                avg_load = sum(load_scores) / len(load_scores)
                max_load = max(load_scores)
                min_load = min(load_scores)
                load_variance = sum((score - avg_load) ** 2 for score in load_scores) / len(load_scores)
            else:
                avg_load = max_load = min_load = load_variance = 0
            
            return {
                'strategy': self.current_strategy.value,
                'healthy_workers': healthy_workers,
                'total_workers': total_workers,
                'health_ratio': healthy_workers / total_workers if total_workers > 0 else 0,
                'load_statistics': {
                    'average': avg_load,
                    'maximum': max_load,
                    'minimum': min_load,
                    'variance': load_variance
                },
                'worker_loads': {
                    worker_id: {
                        'load_score': load.get_load_score(),
                        'current_tasks': load.current_tasks,
                        'success_rate': load.success_rate,
                        'is_healthy': load.is_healthy
                    }
                    for worker_id, load in self.worker_loads.items()
                }
            }


class AsyncTaskManagerWithLoadBalancer(AsyncTaskManager):
    """Gestor de tareas con balanceador de carga integrado."""
    
    def __init__(self, 
                 config: FuzzyEngineConfiguration,
                 load_balancer_config: Optional[LoadBalancerConfig] = None,
                 metrics: Optional[FuzzyEngineMetrics] = None,
                 logger: Optional[logging.Logger] = None):
        super().__init__(config, metrics, logger)
        
        # Compatibilidad con tests: exponer referencia anidada como task_manager
        # para que se pueda acceder como async_task_manager.task_manager.workers
        self.task_manager = self
        
        self.load_balancer_config = load_balancer_config or LoadBalancerConfig()
        self.load_balancer: Optional[AdaptiveLoadBalancer] = None
    
    async def start(self) -> None:
        """Inicia el gestor con balanceador de carga."""
        await super().start()
        
        # Crear balanceador de carga
        worker_ids = [f"worker-{i+1}" for i in range(self.max_workers)]
        self.load_balancer = AdaptiveLoadBalancer(
            config=self.load_balancer_config,
            worker_ids=worker_ids,
            metrics=self.metrics,
            logger=self.logger.getChild("LoadBalancer")
        )
        
        await self.load_balancer.start_monitoring()
        
        # Configurar callback para actualizar carga de workers
        self.add_result_callback(self._update_worker_load_callback)
    
    async def stop(self, timeout: float = 30.0) -> None:
        """Detiene el gestor y el balanceador."""
        if self.load_balancer:
            await self.load_balancer.stop_monitoring()
        
        await super().stop(timeout)
    
    async def _update_worker_load_callback(self, result: TaskResult) -> None:
        """Callback para actualizar carga de workers."""
        if not self.load_balancer or not result.worker_id:
            return
        
        # Obtener estadísticas del worker
        worker_stats = None
        for worker in self.workers:
            if worker.worker_id == result.worker_id:
                worker_stats = worker.get_stats()
                break
        
        if worker_stats:
            await self.load_balancer.update_worker_load(result.worker_id, worker_stats)
    
    def get_load_balancer_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas del balanceador de carga."""
        if self.load_balancer:
            return self.load_balancer.get_statistics()
        return {}