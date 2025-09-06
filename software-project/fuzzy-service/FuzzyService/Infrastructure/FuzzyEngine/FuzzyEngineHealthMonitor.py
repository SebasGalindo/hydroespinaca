from dataclasses import dataclass, field
from datetime import datetime, timezone, timedelta
from enum import Enum
from typing import Dict, List, Optional, Any, Callable, Set
import asyncio
import threading
import time
import logging
from concurrent.futures import ThreadPoolExecutor, as_completed
import json
from pathlib import Path

from .FuzzyEngineExceptions import ValidationException
from .FuzzyEngineConfiguration import FuzzyEngineConfiguration


class HealthStatus(Enum):
    """Estados de salud del sistema."""
    HEALTHY = "healthy"
    DEGRADED = "degraded"
    UNHEALTHY = "unhealthy"
    CRITICAL = "critical"
    UNKNOWN = "unknown"


class ComponentType(Enum):
    """Tipos de componentes monitoreados."""
    ENGINE = "engine"
    FUZZIFICATION = "fuzzification"
    RULE_EVALUATION = "rule_evaluation"
    AGGREGATION = "aggregation"
    DEFUZZIFICATION = "defuzzification"
    METRICS = "metrics"
    VALIDATORS = "validators"
    CACHE = "cache"
    MEMORY = "memory"
    PERFORMANCE = "performance"


class RecoveryAction(Enum):
    """Acciones de recuperación disponibles."""
    RESTART_COMPONENT = "restart_component"
    CLEAR_CACHE = "clear_cache"
    REDUCE_LOAD = "reduce_load"
    INCREASE_TIMEOUT = "increase_timeout"
    DISABLE_FEATURE = "disable_feature"
    ALERT_ADMIN = "alert_admin"
    GRACEFUL_SHUTDOWN = "graceful_shutdown"


@dataclass
class HealthCheckResult:
    """Resultado de un health check individual."""
    component_name: str
    component_type: ComponentType
    status: HealthStatus
    timestamp: datetime
    response_time_ms: float
    details: Dict[str, Any] = field(default_factory=dict)
    issues: List[str] = field(default_factory=list)
    metrics: Dict[str, float] = field(default_factory=dict)
    recovery_suggestions: List[RecoveryAction] = field(default_factory=list)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convierte el resultado a diccionario."""
        return {
            'component_name': self.component_name,
            'component_type': self.component_type.value,
            'status': self.status.value,
            'timestamp': self.timestamp.isoformat(),
            'response_time_ms': self.response_time_ms,
            'details': self.details,
            'issues': self.issues,
            'metrics': self.metrics,
            'recovery_suggestions': [action.value for action in self.recovery_suggestions]
        }


@dataclass
class SystemHealthReport:
    """Reporte completo de salud del sistema."""
    overall_status: HealthStatus
    timestamp: datetime
    component_results: List[HealthCheckResult]
    system_metrics: Dict[str, float]
    active_issues: List[str]
    recovery_actions_taken: List[str]
    uptime_seconds: float
    last_recovery_time: Optional[datetime] = None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convierte el reporte a diccionario."""
        return {
            'overall_status': self.overall_status.value,
            'timestamp': self.timestamp.isoformat(),
            'component_results': [result.to_dict() for result in self.component_results],
            'system_metrics': self.system_metrics,
            'active_issues': self.active_issues,
            'recovery_actions_taken': self.recovery_actions_taken,
            'uptime_seconds': self.uptime_seconds,
            'last_recovery_time': self.last_recovery_time.isoformat() if self.last_recovery_time else None
        }


@dataclass
class HealthThresholds:
    """Umbrales para determinar el estado de salud."""
    response_time_warning_ms: float = 100.0
    response_time_critical_ms: float = 500.0
    memory_usage_warning_percent: float = 80.0
    memory_usage_critical_percent: float = 95.0
    cpu_usage_warning_percent: float = 80.0
    cpu_usage_critical_percent: float = 95.0
    error_rate_warning_percent: float = 5.0
    error_rate_critical_percent: float = 15.0
    cache_hit_rate_warning_percent: float = 70.0
    consecutive_failures_warning: int = 3
    consecutive_failures_critical: int = 5


class HealthCheckRegistry:
    """Registro de health checks disponibles."""
    
    def __init__(self):
        self._checks: Dict[str, Callable[[], Dict[str, Any]]] = {}
        self._component_types: Dict[str, ComponentType] = {}
        self._check_intervals: Dict[str, float] = {}
        self._enabled_checks: Set[str] = set()
    
    def register_check(self, name: str, check_func: Callable[[], Dict[str, Any]], 
                      component_type: ComponentType, interval_seconds: float = 60.0, 
                      enabled: bool = True) -> None:
        """Registra un nuevo health check."""
        self._checks[name] = check_func
        self._component_types[name] = component_type
        self._check_intervals[name] = interval_seconds
        if enabled:
            self._enabled_checks.add(name)
    
    def unregister_check(self, name: str) -> None:
        """Desregistra un health check."""
        self._checks.pop(name, None)
        self._component_types.pop(name, None)
        self._check_intervals.pop(name, None)
        self._enabled_checks.discard(name)
    
    def enable_check(self, name: str) -> None:
        """Habilita un health check."""
        if name in self._checks:
            self._enabled_checks.add(name)
    
    def disable_check(self, name: str) -> None:
        """Deshabilita un health check."""
        self._enabled_checks.discard(name)
    
    def get_enabled_checks(self) -> Dict[str, tuple]:
        """Obtiene los checks habilitados con su información."""
        return {
            name: (self._checks[name], self._component_types[name], self._check_intervals[name])
            for name in self._enabled_checks
            if name in self._checks
        }


class RecoveryManager:
    """Gestor de acciones de recuperación automática."""
    
    def __init__(self, config: FuzzyEngineConfiguration):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self._recovery_actions: Dict[RecoveryAction, Callable] = {}
        self._recovery_history: List[Dict[str, Any]] = []
        self._recovery_lock = threading.Lock()
        self._cooldown_periods: Dict[str, datetime] = {}
        self._max_recovery_attempts = 3
        self._cooldown_minutes = 15
    
    def register_recovery_action(self, action: RecoveryAction, 
                               action_func: Callable[[str, Dict[str, Any]], bool]) -> None:
        """Registra una acción de recuperación."""
        self._recovery_actions[action] = action_func
    
    def execute_recovery(self, component_name: str, actions: List[RecoveryAction], 
                        context: Dict[str, Any]) -> List[str]:
        """Ejecuta acciones de recuperación para un componente."""
        executed_actions = []
        
        with self._recovery_lock:
            # Verificar cooldown
            cooldown_key = f"{component_name}_{'-'.join([a.value for a in actions])}"
            if cooldown_key in self._cooldown_periods:
                if datetime.now(timezone.utc) < self._cooldown_periods[cooldown_key]:
                    self.logger.info(f"Recovery en cooldown para {component_name}")
                    return executed_actions
            
            # Verificar límite de intentos
            recent_attempts = sum(
                1 for entry in self._recovery_history[-10:]
                if entry['component'] == component_name and 
                   datetime.fromisoformat(entry['timestamp']) > datetime.now(timezone.utc) - timedelta(hours=1)
            )
            
            if recent_attempts >= self._max_recovery_attempts:
                self.logger.warning(f"Límite de intentos de recovery alcanzado para {component_name}")
                return executed_actions
            
            # Ejecutar acciones
            for action in actions:
                if action in self._recovery_actions:
                    try:
                        success = self._recovery_actions[action](component_name, context)
                        if success:
                            executed_actions.append(action.value)
                            self.logger.info(f"Recovery action {action.value} ejecutada para {component_name}")
                        else:
                            self.logger.warning(f"Recovery action {action.value} falló para {component_name}")
                    except Exception as e:
                        self.logger.error(f"Error ejecutando recovery action {action.value}: {e}")
            
            # Registrar en historial
            if executed_actions:
                self._recovery_history.append({
                    'component': component_name,
                    'actions': executed_actions,
                    'timestamp': datetime.now(timezone.utc).isoformat(),
                    'context': context
                })
                
                # Establecer cooldown
                self._cooldown_periods[cooldown_key] = (
                    datetime.now(timezone.utc) + timedelta(minutes=self._cooldown_minutes)
                )
        
        return executed_actions
    
    def get_recovery_history(self, component_name: Optional[str] = None, 
                           hours: int = 24) -> List[Dict[str, Any]]:
        """Obtiene el historial de recuperación."""
        cutoff_time = datetime.now(timezone.utc) - timedelta(hours=hours)
        
        filtered_history = [
            entry for entry in self._recovery_history
            if datetime.fromisoformat(entry['timestamp']) > cutoff_time
        ]
        
        if component_name:
            filtered_history = [
                entry for entry in filtered_history
                if entry['component'] == component_name
            ]
        
        return filtered_history


class FuzzyEngineHealthMonitor:
    """Monitor de salud completo para el FuzzyEngine."""
    
    def __init__(self, config: FuzzyEngineConfiguration):
        self.config = config
        self.logger = logging.getLogger(__name__)
        self.thresholds = HealthThresholds()
        self.registry = HealthCheckRegistry()
        self.recovery_manager = RecoveryManager(config)
        
        # Estado del monitor
        self._start_time = datetime.now(timezone.utc)
        self._is_running = False
        self._monitor_thread: Optional[threading.Thread] = None
        self._stop_event = threading.Event()
        self._health_history: List[SystemHealthReport] = []
        self._current_health: Optional[SystemHealthReport] = None
        self._health_lock = threading.Lock()
        
        # Métricas del sistema
        self._system_metrics: Dict[str, float] = {}
        self._component_failure_counts: Dict[str, int] = {}
        
        # Configurar recovery actions por defecto
        self._setup_default_recovery_actions()
        
        # Configurar health checks por defecto
        self._setup_default_health_checks()
    
    def _setup_default_recovery_actions(self) -> None:
        """Configura las acciones de recuperación por defecto."""
        def clear_cache_action(component_name: str, context: Dict[str, Any]) -> bool:
            try:
                # Implementar limpieza de cache específica por componente
                self.logger.info(f"Limpiando cache para {component_name}")
                return True
            except Exception as e:
                self.logger.error(f"Error limpiando cache: {e}")
                return False
        
        def restart_component_action(component_name: str, context: Dict[str, Any]) -> bool:
            try:
                # Implementar reinicio de componente
                self.logger.info(f"Reiniciando componente {component_name}")
                return True
            except Exception as e:
                self.logger.error(f"Error reiniciando componente: {e}")
                return False
        
        def reduce_load_action(component_name: str, context: Dict[str, Any]) -> bool:
            try:
                # Implementar reducción de carga
                self.logger.info(f"Reduciendo carga para {component_name}")
                return True
            except Exception as e:
                self.logger.error(f"Error reduciendo carga: {e}")
                return False
        
        self.recovery_manager.register_recovery_action(RecoveryAction.CLEAR_CACHE, clear_cache_action)
        self.recovery_manager.register_recovery_action(RecoveryAction.RESTART_COMPONENT, restart_component_action)
        self.recovery_manager.register_recovery_action(RecoveryAction.REDUCE_LOAD, reduce_load_action)
    
    def _setup_default_health_checks(self) -> None:
        """Configura los health checks por defecto."""
        # Health check del sistema
        def system_health_check() -> Dict[str, Any]:
            import psutil
            try:
                return {
                    'status': 'healthy',
                    'cpu_percent': psutil.cpu_percent(interval=1),
                    'memory_percent': psutil.virtual_memory().percent,
                    'disk_percent': psutil.disk_usage('/').percent if hasattr(psutil.disk_usage('/'), 'percent') else 0,
                    'load_average': psutil.getloadavg()[0] if hasattr(psutil, 'getloadavg') else 0
                }
            except Exception as e:
                return {
                    'status': 'unhealthy',
                    'error': str(e)
                }
        
        # Health check de memoria
        def memory_health_check() -> Dict[str, Any]:
            import psutil
            try:
                memory = psutil.virtual_memory()
                return {
                    'status': 'healthy' if memory.percent < self.thresholds.memory_usage_warning_percent else 'degraded',
                    'memory_percent': memory.percent,
                    'available_mb': memory.available / (1024 * 1024),
                    'used_mb': memory.used / (1024 * 1024)
                }
            except Exception as e:
                return {
                    'status': 'unhealthy',
                    'error': str(e)
                }
        
        self.registry.register_check('system', system_health_check, ComponentType.ENGINE, 30.0)
        self.registry.register_check('memory', memory_health_check, ComponentType.MEMORY, 15.0)
    
    def register_component_health_check(self, component_name: str, component, 
                                      component_type: ComponentType, 
                                      interval_seconds: float = 60.0) -> None:
        """Registra el health check de un componente."""
        def component_health_check() -> Dict[str, Any]:
            try:
                if hasattr(component, 'health_check'):
                    return component.health_check()
                else:
                    return {
                        'status': 'healthy',
                        'message': 'Component operational (no health_check method)'
                    }
            except Exception as e:
                return {
                    'status': 'unhealthy',
                    'error': str(e)
                }
        
        self.registry.register_check(component_name, component_health_check, 
                                   component_type, interval_seconds)
    
    def start_monitoring(self) -> None:
        """Inicia el monitoreo continuo."""
        if self._is_running:
            self.logger.warning("Health monitor ya está ejecutándose")
            return
        
        self._is_running = True
        self._stop_event.clear()
        self._monitor_thread = threading.Thread(target=self._monitoring_loop, daemon=True)
        self._monitor_thread.start()
        self.logger.info("Health monitor iniciado")
    
    def stop_monitoring(self) -> None:
        """Detiene el monitoreo."""
        if not self._is_running:
            return
        
        self._is_running = False
        self._stop_event.set()
        
        if self._monitor_thread and self._monitor_thread.is_alive():
            self._monitor_thread.join(timeout=5.0)
        
        self.logger.info("Health monitor detenido")
    
    def _monitoring_loop(self) -> None:
        """Loop principal de monitoreo."""
        while self._is_running and not self._stop_event.is_set():
            try:
                # Ejecutar health check completo
                health_report = self.perform_health_check()
                
                # Actualizar estado actual
                with self._health_lock:
                    self._current_health = health_report
                    self._health_history.append(health_report)
                    
                    # Mantener solo las últimas 100 entradas
                    if len(self._health_history) > 100:
                        self._health_history = self._health_history[-100:]
                
                # Ejecutar acciones de recuperación si es necesario
                self._process_recovery_actions(health_report)
                
                # Esperar hasta el próximo check
                self._stop_event.wait(self.config.health_check_interval_seconds)
                
            except Exception as e:
                self.logger.error(f"Error en monitoring loop: {e}")
                self._stop_event.wait(10)  # Esperar antes de reintentar
    
    def perform_health_check(self) -> SystemHealthReport:
        """Ejecuta un health check completo del sistema."""
        start_time = time.time()
        component_results = []
        active_issues = []
        system_metrics = {}
        
        # Ejecutar health checks en paralelo
        enabled_checks = self.registry.get_enabled_checks()
        
        with ThreadPoolExecutor(max_workers=min(len(enabled_checks), 10)) as executor:
            future_to_check = {
                executor.submit(self._execute_health_check, name, check_func, component_type): name
                for name, (check_func, component_type, _) in enabled_checks.items()
            }
            
            for future in as_completed(future_to_check):
                check_name = future_to_check[future]
                try:
                    result = future.result(timeout=30)  # Timeout de 30 segundos
                    component_results.append(result)
                    
                    # Recopilar issues
                    if result.issues:
                        active_issues.extend(result.issues)
                    
                    # Recopilar métricas
                    system_metrics.update(result.metrics)
                    
                except Exception as e:
                    self.logger.error(f"Error en health check {check_name}: {e}")
                    # Crear resultado de error
                    error_result = HealthCheckResult(
                        component_name=check_name,
                        component_type=ComponentType.UNKNOWN,
                        status=HealthStatus.CRITICAL,
                        timestamp=datetime.now(timezone.utc),
                        response_time_ms=0,
                        issues=[f"Health check failed: {str(e)}"]
                    )
                    component_results.append(error_result)
                    active_issues.append(f"{check_name}: Health check failed")
        
        # Determinar estado general
        overall_status = self._determine_overall_status(component_results)
        
        # Calcular uptime
        uptime_seconds = (datetime.now(timezone.utc) - self._start_time).total_seconds()
        
        return SystemHealthReport(
            overall_status=overall_status,
            timestamp=datetime.now(timezone.utc),
            component_results=component_results,
            system_metrics=system_metrics,
            active_issues=active_issues,
            recovery_actions_taken=[],
            uptime_seconds=uptime_seconds
        )
    
    def _execute_health_check(self, name: str, check_func: Callable, 
                            component_type: ComponentType) -> HealthCheckResult:
        """Ejecuta un health check individual."""
        start_time = time.time()
        
        try:
            result_dict = check_func()
            response_time_ms = (time.time() - start_time) * 1000
            
            # Parsear resultado
            status_str = result_dict.get('status', 'unknown')
            status = HealthStatus(status_str) if status_str in [s.value for s in HealthStatus] else HealthStatus.UNKNOWN
            
            # Determinar acciones de recuperación sugeridas
            recovery_suggestions = self._suggest_recovery_actions(name, result_dict, response_time_ms)
            
            return HealthCheckResult(
                component_name=name,
                component_type=component_type,
                status=status,
                timestamp=datetime.now(timezone.utc),
                response_time_ms=response_time_ms,
                details=result_dict,
                issues=result_dict.get('issues', []),
                metrics=self._extract_metrics(result_dict),
                recovery_suggestions=recovery_suggestions
            )
            
        except Exception as e:
            response_time_ms = (time.time() - start_time) * 1000
            return HealthCheckResult(
                component_name=name,
                component_type=component_type,
                status=HealthStatus.CRITICAL,
                timestamp=datetime.now(timezone.utc),
                response_time_ms=response_time_ms,
                issues=[f"Health check exception: {str(e)}"]
            )
    
    def _suggest_recovery_actions(self, component_name: str, result_dict: Dict[str, Any], 
                                response_time_ms: float) -> List[RecoveryAction]:
        """Sugiere acciones de recuperación basadas en el resultado del health check."""
        suggestions = []
        
        # Sugerencias basadas en tiempo de respuesta
        if response_time_ms > self.thresholds.response_time_critical_ms:
            suggestions.extend([RecoveryAction.RESTART_COMPONENT, RecoveryAction.REDUCE_LOAD])
        elif response_time_ms > self.thresholds.response_time_warning_ms:
            suggestions.append(RecoveryAction.CLEAR_CACHE)
        
        # Sugerencias basadas en métricas específicas
        if 'memory_percent' in result_dict:
            memory_percent = result_dict['memory_percent']
            if memory_percent > self.thresholds.memory_usage_critical_percent:
                suggestions.extend([RecoveryAction.CLEAR_CACHE, RecoveryAction.REDUCE_LOAD])
        
        if 'cache_hit_rate' in result_dict:
            cache_hit_rate = result_dict['cache_hit_rate']
            if cache_hit_rate < self.thresholds.cache_hit_rate_warning_percent / 100:
                suggestions.append(RecoveryAction.CLEAR_CACHE)
        
        # Sugerencias basadas en errores
        if result_dict.get('status') == 'unhealthy':
            suggestions.append(RecoveryAction.RESTART_COMPONENT)
        
        return list(set(suggestions))  # Eliminar duplicados
    
    def _extract_metrics(self, result_dict: Dict[str, Any]) -> Dict[str, float]:
        """Extrae métricas numéricas del resultado del health check."""
        metrics = {}
        
        numeric_fields = [
            'response_time_ms', 'memory_percent', 'cpu_percent', 'cache_hit_rate',
            'total_evaluations', 'avg_evaluation_time_ms', 'error_rate'
        ]
        
        for field in numeric_fields:
            if field in result_dict and isinstance(result_dict[field], (int, float)):
                metrics[field] = float(result_dict[field])
        
        return metrics
    
    def _determine_overall_status(self, component_results: List[HealthCheckResult]) -> HealthStatus:
        """Determina el estado general del sistema basado en los resultados de componentes."""
        if not component_results:
            return HealthStatus.UNKNOWN
        
        status_counts = {status: 0 for status in HealthStatus}
        for result in component_results:
            status_counts[result.status] += 1
        
        total_components = len(component_results)
        
        # Si hay componentes críticos, el sistema está crítico
        if status_counts[HealthStatus.CRITICAL] > 0:
            return HealthStatus.CRITICAL
        
        # Si más del 50% están unhealthy, el sistema está unhealthy
        if status_counts[HealthStatus.UNHEALTHY] > total_components * 0.5:
            return HealthStatus.UNHEALTHY
        
        # Si hay algún componente unhealthy o más del 30% degraded, el sistema está degraded
        if (status_counts[HealthStatus.UNHEALTHY] > 0 or 
            status_counts[HealthStatus.DEGRADED] > total_components * 0.3):
            return HealthStatus.DEGRADED
        
        # Si la mayoría están healthy, el sistema está healthy
        if status_counts[HealthStatus.HEALTHY] > total_components * 0.7:
            return HealthStatus.HEALTHY
        
        return HealthStatus.DEGRADED
    
    def _process_recovery_actions(self, health_report: SystemHealthReport) -> None:
        """Procesa y ejecuta acciones de recuperación basadas en el reporte de salud."""
        if not self.config.enable_circuit_breaker:  # Usar como flag para auto-recovery
            return
        
        for result in health_report.component_results:
            if result.status in [HealthStatus.UNHEALTHY, HealthStatus.CRITICAL] and result.recovery_suggestions:
                # Actualizar contador de fallos
                self._component_failure_counts[result.component_name] = (
                    self._component_failure_counts.get(result.component_name, 0) + 1
                )
                
                # Ejecutar recovery solo si hay suficientes fallos consecutivos
                failure_count = self._component_failure_counts[result.component_name]
                if failure_count >= self.thresholds.consecutive_failures_warning:
                    executed_actions = self.recovery_manager.execute_recovery(
                        result.component_name,
                        result.recovery_suggestions,
                        {'health_result': result.to_dict()}
                    )
                    
                    if executed_actions:
                        health_report.recovery_actions_taken.extend(executed_actions)
                        health_report.last_recovery_time = datetime.now(timezone.utc)
            else:
                # Reset contador si el componente está healthy
                self._component_failure_counts.pop(result.component_name, None)
    
    def get_current_health(self) -> Optional[SystemHealthReport]:
        """Obtiene el reporte de salud actual."""
        with self._health_lock:
            return self._current_health
    
    def get_health_history(self, hours: int = 24) -> List[SystemHealthReport]:
        """Obtiene el historial de salud."""
        cutoff_time = datetime.now(timezone.utc) - timedelta(hours=hours)
        
        with self._health_lock:
            return [
                report for report in self._health_history
                if report.timestamp > cutoff_time
            ]
    
    def get_health_summary(self) -> Dict[str, Any]:
        """Obtiene un resumen del estado de salud."""
        current_health = self.get_current_health()
        if not current_health:
            return {'status': 'unknown', 'message': 'No health data available'}
        
        recent_history = self.get_health_history(hours=1)
        
        # Calcular estadísticas
        status_distribution = {}
        for report in recent_history:
            status = report.overall_status.value
            status_distribution[status] = status_distribution.get(status, 0) + 1
        
        # Componentes con más problemas
        component_issues = {}
        for report in recent_history:
            for result in report.component_results:
                if result.issues:
                    component_name = result.component_name
                    component_issues[component_name] = component_issues.get(component_name, 0) + len(result.issues)
        
        return {
            'current_status': current_health.overall_status.value,
            'uptime_hours': current_health.uptime_seconds / 3600,
            'total_components': len(current_health.component_results),
            'active_issues_count': len(current_health.active_issues),
            'recent_status_distribution': status_distribution,
            'problematic_components': dict(sorted(component_issues.items(), key=lambda x: x[1], reverse=True)[:5]),
            'last_recovery': current_health.last_recovery_time.isoformat() if current_health.last_recovery_time else None,
            'monitoring_active': self._is_running
        }
    
    def export_health_report(self, file_path: str, format: str = 'json') -> None:
        """Exporta el reporte de salud a un archivo."""
        current_health = self.get_current_health()
        if not current_health:
            raise ValidationException("No hay datos de salud disponibles para exportar")
        
        try:
            if format.lower() == 'json':
                with open(file_path, 'w', encoding='utf-8') as f:
                    json.dump(current_health.to_dict(), f, indent=2, ensure_ascii=False)
            else:
                raise ValidationException(f"Formato de exportación no soportado: {format}")
            
            self.logger.info(f"Reporte de salud exportado a {file_path}")
            
        except Exception as e:
            raise ValidationException(f"Error exportando reporte de salud: {str(e)}")
    
    def configure_thresholds(self, **kwargs) -> None:
        """Configura los umbrales de salud."""
        for key, value in kwargs.items():
            if hasattr(self.thresholds, key):
                setattr(self.thresholds, key, value)
                self.logger.info(f"Threshold {key} actualizado a {value}")
            else:
                self.logger.warning(f"Threshold desconocido: {key}")
    
    def __enter__(self):
        """Context manager entry."""
        self.start_monitoring()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit."""
        self.stop_monitoring()