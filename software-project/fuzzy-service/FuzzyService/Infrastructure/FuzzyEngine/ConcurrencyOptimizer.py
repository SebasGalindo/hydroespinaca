from __future__ import annotations

import asyncio
import logging
import threading
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import Enum
from typing import Dict, List, Optional, Any, Callable
import psutil
import statistics
from collections import deque

from .FuzzyEngineConfiguration import FuzzyEngineConfiguration, PerformanceLimits
from .FuzzyEngineMetrics import FuzzyEngineMetrics
from .AsyncTaskQueue import AsyncTaskManager
from .AsyncLoadBalancer import AdaptiveLoadBalancer


class SystemLoadLevel(Enum):
    """Niveles de carga del sistema."""
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"
    CRITICAL = "critical"


class OptimizationStrategy(Enum):
    """Estrategias de optimización."""
    CONSERVATIVE = "conservative"  # Cambios graduales
    AGGRESSIVE = "aggressive"     # Cambios rápidos
    ADAPTIVE = "adaptive"         # Basado en historial
    PREDICTIVE = "predictive"     # Basado en tendencias


@dataclass
class SystemMetrics:
    """Métricas del sistema en tiempo real."""
    cpu_usage_percent: float
    memory_usage_percent: float
    available_memory_mb: float
    active_threads: int
    queue_size: int
    avg_response_time_ms: float
    error_rate_percent: float
    throughput_requests_per_second: float
    timestamp: datetime = field(default_factory=lambda: datetime.now(timezone.utc))
    
    def get_load_level(self) -> SystemLoadLevel:
        """Determina el nivel de carga basado en las métricas."""
        # Calcular score de carga (0-100)
        cpu_score = self.cpu_usage_percent
        memory_score = self.memory_usage_percent
        response_score = min(self.avg_response_time_ms / 10, 100)  # Normalizar a 100
        error_score = self.error_rate_percent * 10  # Amplificar impacto de errores
        
        overall_score = (cpu_score + memory_score + response_score + error_score) / 4
        
        if overall_score < 25:
            return SystemLoadLevel.LOW
        elif overall_score < 50:
            return SystemLoadLevel.MEDIUM
        elif overall_score < 75:
            return SystemLoadLevel.HIGH
        else:
            return SystemLoadLevel.CRITICAL


@dataclass
class OptimizationRecommendation:
    """Recomendación de optimización."""
    parameter_name: str
    current_value: Any
    recommended_value: Any
    reason: str
    impact_level: str  # low, medium, high
    confidence_score: float  # 0.0 - 1.0
    

@dataclass
class ConcurrencyProfile:
    """Perfil de configuración de concurrencia."""
    name: str
    max_workers: int
    max_concurrent_evaluations: int
    parallel_threshold: int
    queue_size_limit: int
    async_workers: int
    thread_pool_size: int
    description: str = ""
    
    @classmethod
    def create_low_load_profile(cls) -> 'ConcurrencyProfile':
        """Perfil para carga baja - conservar recursos."""
        return cls(
            name="low_load",
            max_workers=2,
            max_concurrent_evaluations=5,
            parallel_threshold=10,
            queue_size_limit=500,
            async_workers=2,
            thread_pool_size=2,
            description="Configuración optimizada para carga baja"
        )
    
    @classmethod
    def create_medium_load_profile(cls) -> 'ConcurrencyProfile':
        """Perfil para carga media - balanceado."""
        return cls(
            name="medium_load",
            max_workers=4,
            max_concurrent_evaluations=10,
            parallel_threshold=5,
            queue_size_limit=1000,
            async_workers=4,
            thread_pool_size=4,
            description="Configuración balanceada para carga media"
        )
    
    @classmethod
    def create_high_load_profile(cls) -> 'ConcurrencyProfile':
        """Perfil para carga alta - máximo rendimiento."""
        return cls(
            name="high_load",
            max_workers=8,
            max_concurrent_evaluations=20,
            parallel_threshold=3,
            queue_size_limit=2000,
            async_workers=6,
            thread_pool_size=8,
            description="Configuración optimizada para carga alta"
        )
    
    @classmethod
    def create_critical_load_profile(cls) -> 'ConcurrencyProfile':
        """Perfil para carga crítica - modo supervivencia."""
        return cls(
            name="critical_load",
            max_workers=2,
            max_concurrent_evaluations=3,
            parallel_threshold=20,
            queue_size_limit=200,
            async_workers=2,
            thread_pool_size=2,
            description="Configuración de supervivencia para carga crítica"
        )


class ConcurrencyOptimizer:
    """Optimizador dinámico de configuración de concurrencia."""
    
    def __init__(self,
                 config: FuzzyEngineConfiguration,
                 metrics: FuzzyEngineMetrics,
                 task_manager: Optional[AsyncTaskManager] = None,
                 load_balancer: Optional[AdaptiveLoadBalancer] = None,
                 logger: Optional[logging.Logger] = None):
        self.config = config
        self.metrics = metrics
        self.task_manager = task_manager
        self.load_balancer = load_balancer
        self.logger = logger or logging.getLogger("ConcurrencyOptimizer")
        
        # Configuración del optimizador
        self.optimization_strategy = OptimizationStrategy.ADAPTIVE
        self.optimization_interval_seconds = 30.0
        self.metrics_history_size = 100
        self.min_samples_for_optimization = 10
        
        # Historial de métricas
        self.metrics_history: deque[SystemMetrics] = deque(maxlen=self.metrics_history_size)
        self.optimization_history: List[Dict[str, Any]] = []
        
        # Perfiles de configuración
        self.profiles = {
            SystemLoadLevel.LOW: ConcurrencyProfile.create_low_load_profile(),
            SystemLoadLevel.MEDIUM: ConcurrencyProfile.create_medium_load_profile(),
            SystemLoadLevel.HIGH: ConcurrencyProfile.create_high_load_profile(),
            SystemLoadLevel.CRITICAL: ConcurrencyProfile.create_critical_load_profile()
        }
        
        # Estado del optimizador
        self.current_profile: Optional[ConcurrencyProfile] = None
        self.last_optimization_time = datetime.now(timezone.utc)
        self.optimization_enabled = True
        
        # Locks para thread safety
        self.metrics_lock = threading.RLock()
        self.optimization_lock = threading.RLock()
        
        # Tareas de monitoreo
        self._monitoring_task: Optional[asyncio.Task] = None
        self._running = False
        
        self.logger.info("ConcurrencyOptimizer inicializado")
    
    async def start_optimization(self) -> None:
        """Inicia el proceso de optimización automática."""
        if self._running:
            return
        
        self._running = True
        self.logger.info("Iniciando optimización automática de concurrencia")
        
        # Crear tarea de monitoreo
        self._monitoring_task = asyncio.create_task(self._optimization_loop())
    
    async def stop_optimization(self) -> None:
        """Detiene el proceso de optimización."""
        if not self._running:
            return
        
        self._running = False
        self.logger.info("Deteniendo optimización de concurrencia")
        
        if self._monitoring_task:
            self._monitoring_task.cancel()
            try:
                await self._monitoring_task
            except asyncio.CancelledError:
                pass
    
    async def _optimization_loop(self) -> None:
        """Loop principal de optimización."""
        while self._running:
            try:
                # Recopilar métricas del sistema
                system_metrics = await self._collect_system_metrics()
                
                with self.metrics_lock:
                    self.metrics_history.append(system_metrics)
                
                # Ejecutar optimización si es necesario
                if self._should_optimize():
                    await self._perform_optimization()
                
                # Esperar hasta la próxima iteración
                await asyncio.sleep(self.optimization_interval_seconds)
                
            except asyncio.CancelledError:
                break
            except Exception as e:
                self.logger.error(f"Error en loop de optimización: {e}")
                await asyncio.sleep(5.0)  # Pausa antes de reintentar
    
    async def _collect_system_metrics(self) -> SystemMetrics:
        """Recopila métricas del sistema."""
        try:
            # Métricas del sistema
            cpu_percent = psutil.cpu_percent(interval=1)
            memory = psutil.virtual_memory()
            memory_percent = memory.percent
            available_memory_mb = memory.available / (1024 * 1024)
            
            # Métricas del task manager
            queue_size = 0
            active_threads = threading.active_count()
            
            if self.task_manager:
                status = self.task_manager.get_status()
                queue_size = status.get('queue_size', 0)
                active_threads = status.get('active_workers', 0)
            
            # Métricas de rendimiento del fuzzy engine
            performance_summary = self.metrics.get_performance_summary()
            avg_response_time = performance_summary.get('avg_execution_time_ms', 0.0)
            error_rate = performance_summary.get('error_rate_percent', 0.0)
            throughput = performance_summary.get('requests_per_second', 0.0)
            
            return SystemMetrics(
                cpu_usage_percent=cpu_percent,
                memory_usage_percent=memory_percent,
                available_memory_mb=available_memory_mb,
                active_threads=active_threads,
                queue_size=queue_size,
                avg_response_time_ms=avg_response_time,
                error_rate_percent=error_rate,
                throughput_requests_per_second=throughput
            )
            
        except Exception as e:
            self.logger.error(f"Error recopilando métricas del sistema: {e}")
            # Retornar métricas por defecto en caso de error
            return SystemMetrics(
                cpu_usage_percent=0.0,
                memory_usage_percent=0.0,
                available_memory_mb=1000.0,
                active_threads=1,
                queue_size=0,
                avg_response_time_ms=0.0,
                error_rate_percent=0.0,
                throughput_requests_per_second=0.0
            )
    
    def _should_optimize(self) -> bool:
        """Determina si se debe ejecutar optimización."""
        if not self.optimization_enabled:
            return False
        
        with self.metrics_lock:
            # Verificar si hay suficientes muestras
            if len(self.metrics_history) < self.min_samples_for_optimization:
                return False
            
            # Verificar intervalo de tiempo
            time_since_last = datetime.now(timezone.utc) - self.last_optimization_time
            if time_since_last.total_seconds() < self.optimization_interval_seconds:
                return False
            
            # Verificar si hay cambios significativos en las métricas
            recent_metrics = list(self.metrics_history)[-5:]  # Últimas 5 muestras
            if len(recent_metrics) < 3:
                return False
            
            # Calcular variabilidad en las métricas clave
            cpu_values = [m.cpu_usage_percent for m in recent_metrics]
            memory_values = [m.memory_usage_percent for m in recent_metrics]
            response_values = [m.avg_response_time_ms for m in recent_metrics]
            
            cpu_std = statistics.stdev(cpu_values) if len(cpu_values) > 1 else 0
            memory_std = statistics.stdev(memory_values) if len(memory_values) > 1 else 0
            response_std = statistics.stdev(response_values) if len(response_values) > 1 else 0
            
            # Si hay alta variabilidad, optimizar
            return cpu_std > 10 or memory_std > 10 or response_std > 50
    
    async def _perform_optimization(self) -> None:
        """Ejecuta el proceso de optimización."""
        with self.optimization_lock:
            try:
                self.logger.info("Ejecutando optimización de concurrencia")
                
                # Analizar métricas actuales
                current_metrics = self._analyze_current_metrics()
                load_level = current_metrics.get_load_level()
                
                # Obtener perfil recomendado
                recommended_profile = self.profiles[load_level]
                
                # Verificar si necesita cambio de perfil
                if self._should_change_profile(recommended_profile):
                    await self._apply_profile(recommended_profile)
                
                # Generar recomendaciones específicas
                recommendations = self._generate_recommendations(current_metrics)
                
                # Aplicar recomendaciones si es apropiado
                if recommendations:
                    await self._apply_recommendations(recommendations)
                
                # Registrar optimización
                self._record_optimization(load_level, recommended_profile, recommendations)
                
                self.last_optimization_time = datetime.now(timezone.utc)
                
            except Exception as e:
                self.logger.error(f"Error durante optimización: {e}")
    
    def _analyze_current_metrics(self) -> SystemMetrics:
        """Analiza las métricas actuales y retorna un resumen."""
        with self.metrics_lock:
            if not self.metrics_history:
                return SystemMetrics(
                    cpu_usage_percent=0.0,
                    memory_usage_percent=0.0,
                    available_memory_mb=1000.0,
                    active_threads=1,
                    queue_size=0,
                    avg_response_time_ms=0.0,
                    error_rate_percent=0.0,
                    throughput_requests_per_second=0.0
                )
            
            # Calcular promedios de las últimas métricas
            recent_metrics = list(self.metrics_history)[-10:]  # Últimas 10 muestras
            
            avg_cpu = statistics.mean([m.cpu_usage_percent for m in recent_metrics])
            avg_memory = statistics.mean([m.memory_usage_percent for m in recent_metrics])
            avg_available_memory = statistics.mean([m.available_memory_mb for m in recent_metrics])
            avg_threads = statistics.mean([m.active_threads for m in recent_metrics])
            avg_queue_size = statistics.mean([m.queue_size for m in recent_metrics])
            avg_response_time = statistics.mean([m.avg_response_time_ms for m in recent_metrics])
            avg_error_rate = statistics.mean([m.error_rate_percent for m in recent_metrics])
            avg_throughput = statistics.mean([m.throughput_requests_per_second for m in recent_metrics])
            
            return SystemMetrics(
                cpu_usage_percent=avg_cpu,
                memory_usage_percent=avg_memory,
                available_memory_mb=avg_available_memory,
                active_threads=int(avg_threads),
                queue_size=int(avg_queue_size),
                avg_response_time_ms=avg_response_time,
                error_rate_percent=avg_error_rate,
                throughput_requests_per_second=avg_throughput
            )
    
    def _should_change_profile(self, recommended_profile: ConcurrencyProfile) -> bool:
        """Determina si se debe cambiar al perfil recomendado."""
        if self.current_profile is None:
            return True
        
        return self.current_profile.name != recommended_profile.name
    
    async def _apply_profile(self, profile: ConcurrencyProfile) -> None:
        """Aplica un perfil de configuración."""
        try:
            self.logger.info(f"Aplicando perfil de concurrencia: {profile.name}")
            
            # Actualizar configuración de performance limits
            self.config.performance_limits.max_workers = profile.max_workers
            self.config.performance_limits.max_concurrent_evaluations = profile.max_concurrent_evaluations
            self.config.performance_limits.parallel_threshold = profile.parallel_threshold
            
            # Actualizar task manager si está disponible
            if self.task_manager:
                self.task_manager.max_workers = profile.async_workers
                self.task_manager.queue_size = profile.queue_size_limit
            
            self.current_profile = profile
            
            self.logger.info(
                f"Perfil aplicado: workers={profile.max_workers}, "
                f"concurrent={profile.max_concurrent_evaluations}, "
                f"threshold={profile.parallel_threshold}"
            )
            
        except Exception as e:
            self.logger.error(f"Error aplicando perfil {profile.name}: {e}")
    
    def _generate_recommendations(self, metrics: SystemMetrics) -> List[OptimizationRecommendation]:
        """Genera recomendaciones específicas de optimización."""
        recommendations = []
        
        # Recomendación basada en CPU
        if metrics.cpu_usage_percent > 80:
            recommendations.append(OptimizationRecommendation(
                parameter_name="max_workers",
                current_value=self.config.performance_limits.max_workers,
                recommended_value=max(1, self.config.performance_limits.max_workers - 1),
                reason="CPU usage alto, reducir workers para evitar contención",
                impact_level="medium",
                confidence_score=0.8
            ))
        elif metrics.cpu_usage_percent < 30 and metrics.queue_size > 10:
            recommendations.append(OptimizationRecommendation(
                parameter_name="max_workers",
                current_value=self.config.performance_limits.max_workers,
                recommended_value=min(8, self.config.performance_limits.max_workers + 1),
                reason="CPU usage bajo con cola grande, aumentar workers",
                impact_level="medium",
                confidence_score=0.7
            ))
        
        # Recomendación basada en memoria
        if metrics.memory_usage_percent > 85:
            recommendations.append(OptimizationRecommendation(
                parameter_name="max_concurrent_evaluations",
                current_value=self.config.performance_limits.max_concurrent_evaluations,
                recommended_value=max(1, self.config.performance_limits.max_concurrent_evaluations - 2),
                reason="Memoria alta, reducir evaluaciones concurrentes",
                impact_level="high",
                confidence_score=0.9
            ))
        
        # Recomendación basada en tiempo de respuesta
        if metrics.avg_response_time_ms > 1000:
            recommendations.append(OptimizationRecommendation(
                parameter_name="parallel_threshold",
                current_value=self.config.performance_limits.parallel_threshold,
                recommended_value=max(1, self.config.performance_limits.parallel_threshold - 1),
                reason="Tiempo de respuesta alto, reducir threshold para más paralelización",
                impact_level="medium",
                confidence_score=0.6
            ))
        
        return recommendations
    
    async def _apply_recommendations(self, recommendations: List[OptimizationRecommendation]) -> None:
        """Aplica las recomendaciones de optimización."""
        for rec in recommendations:
            if rec.confidence_score >= 0.7:  # Solo aplicar recomendaciones con alta confianza
                try:
                    self.logger.info(
                        f"Aplicando recomendación: {rec.parameter_name} "
                        f"{rec.current_value} -> {rec.recommended_value} ({rec.reason})"
                    )
                    
                    # Aplicar cambio según el parámetro
                    if rec.parameter_name == "max_workers":
                        self.config.performance_limits.max_workers = rec.recommended_value
                    elif rec.parameter_name == "max_concurrent_evaluations":
                        self.config.performance_limits.max_concurrent_evaluations = rec.recommended_value
                    elif rec.parameter_name == "parallel_threshold":
                        self.config.performance_limits.parallel_threshold = rec.recommended_value
                    
                except Exception as e:
                    self.logger.error(f"Error aplicando recomendación {rec.parameter_name}: {e}")
    
    def _record_optimization(self, 
                           load_level: SystemLoadLevel, 
                           profile: ConcurrencyProfile, 
                           recommendations: List[OptimizationRecommendation]) -> None:
        """Registra la optimización realizada."""
        optimization_record = {
            'timestamp': datetime.now(timezone.utc).isoformat(),
            'load_level': load_level.value,
            'profile_applied': profile.name,
            'recommendations_count': len(recommendations),
            'recommendations': [
                {
                    'parameter': rec.parameter_name,
                    'old_value': rec.current_value,
                    'new_value': rec.recommended_value,
                    'reason': rec.reason,
                    'confidence': rec.confidence_score
                }
                for rec in recommendations
            ]
        }
        
        self.optimization_history.append(optimization_record)
        
        # Mantener solo los últimos 100 registros
        if len(self.optimization_history) > 100:
            self.optimization_history = self.optimization_history[-100:]
    
    def get_optimization_status(self) -> Dict[str, Any]:
        """Obtiene el estado actual del optimizador."""
        with self.metrics_lock:
            current_metrics = self._analyze_current_metrics() if self.metrics_history else None
            
            return {
                'enabled': self.optimization_enabled,
                'running': self._running,
                'current_profile': self.current_profile.name if self.current_profile else None,
                'last_optimization': self.last_optimization_time.isoformat(),
                'metrics_samples': len(self.metrics_history),
                'optimization_history_count': len(self.optimization_history),
                'current_load_level': current_metrics.get_load_level().value if current_metrics else None,
                'current_metrics': {
                    'cpu_percent': current_metrics.cpu_usage_percent if current_metrics else 0,
                    'memory_percent': current_metrics.memory_usage_percent if current_metrics else 0,
                    'queue_size': current_metrics.queue_size if current_metrics else 0,
                    'avg_response_time_ms': current_metrics.avg_response_time_ms if current_metrics else 0
                } if current_metrics else None
            }
    
    def enable_optimization(self) -> None:
        """Habilita la optimización automática."""
        self.optimization_enabled = True
        self.logger.info("Optimización automática habilitada")
    
    def disable_optimization(self) -> None:
        """Deshabilita la optimización automática."""
        self.optimization_enabled = False
        self.logger.info("Optimización automática deshabilitada")
    
    def force_optimization(self) -> None:
        """Fuerza una optimización inmediata."""
        self.last_optimization_time = datetime.now(timezone.utc) - \
            asyncio.get_event_loop().time() * 2  # Forzar que sea tiempo de optimizar
        self.logger.info("Optimización forzada programada")