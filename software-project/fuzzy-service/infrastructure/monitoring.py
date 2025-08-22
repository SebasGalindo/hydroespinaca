import logging
import time
import threading
from typing import Dict, Any, Optional, List, Callable
from datetime import datetime, timedelta
from dataclasses import dataclass, field
from collections import defaultdict, deque
from enum import Enum
import json
import psutil
import os

class MetricType(Enum):
    """Tipos de métricas"""
    COUNTER = "counter"
    GAUGE = "gauge"
    HISTOGRAM = "histogram"
    TIMER = "timer"

class LogLevel(Enum):
    """Niveles de log estructurado"""
    DEBUG = "debug"
    INFO = "info"
    WARNING = "warning"
    ERROR = "error"
    CRITICAL = "critical"

@dataclass
class MetricValue:
    """Valor de métrica con timestamp"""
    value: float
    timestamp: datetime
    labels: Dict[str, str] = field(default_factory=dict)

@dataclass
class HistogramBucket:
    """Bucket de histograma"""
    upper_bound: float
    count: int = 0

class StructuredLogger:
    """Logger estructurado para mejor observabilidad"""
    
    def __init__(self, name: str):
        self.logger = logging.getLogger(name)
        self.service_name = "fuzzy-service"
        self.version = "1.0.0"
        
        # //optimizado de "logs no estructurados" a "logs estructurados" porque mejora observabilidad y debugging
        
    def _log_structured(self, level: LogLevel, message: str, **kwargs):
        """Log estructurado con contexto"""
        log_entry = {
            "timestamp": datetime.now().isoformat(),
            "service": self.service_name,
            "version": self.version,
            "level": level.value,
            "message": message,
            "thread_id": threading.get_ident(),
            "process_id": os.getpid()
        }
        
        # Agregar contexto adicional
        log_entry.update(kwargs)
        
        # Log como JSON para facilitar parsing
        json_log = json.dumps(log_entry, default=str)
        
        if level == LogLevel.DEBUG:
            self.logger.debug(json_log)
        elif level == LogLevel.INFO:
            self.logger.info(json_log)
        elif level == LogLevel.WARNING:
            self.logger.warning(json_log)
        elif level == LogLevel.ERROR:
            self.logger.error(json_log)
        elif level == LogLevel.CRITICAL:
            self.logger.critical(json_log)
    
    def debug(self, message: str, **kwargs):
        self._log_structured(LogLevel.DEBUG, message, **kwargs)
    
    def info(self, message: str, **kwargs):
        self._log_structured(LogLevel.INFO, message, **kwargs)
    
    def warning(self, message: str, **kwargs):
        self._log_structured(LogLevel.WARNING, message, **kwargs)
    
    def error(self, message: str, **kwargs):
        self._log_structured(LogLevel.ERROR, message, **kwargs)
    
    def critical(self, message: str, **kwargs):
        self._log_structured(LogLevel.CRITICAL, message, **kwargs)
    
    def log_request(self, method: str, path: str, status_code: int, duration_ms: float, **kwargs):
        """Log específico para requests HTTP"""
        self.info(
            "HTTP request processed",
            request_method=method,
            request_path=path,
            response_status=status_code,
            duration_ms=duration_ms,
            **kwargs
        )
    
    def log_fuzzy_evaluation(self, system_id: str, duration_ms: float, rules_fired: int, **kwargs):
        """Log específico para evaluaciones fuzzy"""
        self.info(
            "Fuzzy evaluation completed",
            system_id=system_id,
            duration_ms=duration_ms,
            rules_fired=rules_fired,
            **kwargs
        )
    
    def log_actuator_command(self, actuator_id: str, command: str, value: float, success: bool, **kwargs):
        """Log específico para comandos de actuadores"""
        level = LogLevel.INFO if success else LogLevel.ERROR
        self._log_structured(
            level,
            "Actuator command executed",
            actuator_id=actuator_id,
            command=command,
            value=value,
            success=success,
            **kwargs
        )

class MetricsCollector:
    """Colector de métricas en memoria"""
    
    def __init__(self, retention_minutes: int = 60):
        self._metrics: Dict[str, List[MetricValue]] = defaultdict(list)
        self._counters: Dict[str, float] = defaultdict(float)
        self._gauges: Dict[str, float] = defaultdict(float)
        self._histograms: Dict[str, List[HistogramBucket]] = defaultdict(list)
        self._timers: Dict[str, deque] = defaultdict(lambda: deque(maxlen=1000))
        
        self._retention = timedelta(minutes=retention_minutes)
        self._lock = threading.RLock()
        
        # //optimizado de "sin métricas" a "métricas en memoria" porque permite monitoreo sin dependencias externas
        
        # Hilo de limpieza
        self._cleanup_thread = threading.Thread(target=self._periodic_cleanup, daemon=True)
        self._cleanup_thread.start()
        
        # Métricas del sistema
        self._system_metrics_thread = threading.Thread(target=self._collect_system_metrics, daemon=True)
        self._system_metrics_thread.start()
    
    def increment_counter(self, name: str, value: float = 1.0, labels: Optional[Dict[str, str]] = None):
        """Incrementa un contador"""
        with self._lock:
            key = self._make_key(name, labels)
            self._counters[key] += value
            
            # También guardar en historial
            self._metrics[key].append(MetricValue(
                value=self._counters[key],
                timestamp=datetime.now(),
                labels=labels or {}
            ))
    
    def set_gauge(self, name: str, value: float, labels: Optional[Dict[str, str]] = None):
        """Establece valor de gauge"""
        with self._lock:
            key = self._make_key(name, labels)
            self._gauges[key] = value
            
            self._metrics[key].append(MetricValue(
                value=value,
                timestamp=datetime.now(),
                labels=labels or {}
            ))
    
    def record_histogram(self, name: str, value: float, buckets: List[float], labels: Optional[Dict[str, str]] = None):
        """Registra valor en histograma"""
        with self._lock:
            key = self._make_key(name, labels)
            
            # Inicializar buckets si no existen
            if key not in self._histograms:
                self._histograms[key] = [HistogramBucket(bound) for bound in sorted(buckets)]
            
            # Incrementar buckets apropiados
            for bucket in self._histograms[key]:
                if value <= bucket.upper_bound:
                    bucket.count += 1
    
    def record_timer(self, name: str, duration_ms: float, labels: Optional[Dict[str, str]] = None):
        """Registra duración de timer"""
        with self._lock:
            key = self._make_key(name, labels)
            self._timers[key].append({
                'duration_ms': duration_ms,
                'timestamp': datetime.now(),
                'labels': labels or {}
            })
    
    def get_counter(self, name: str, labels: Optional[Dict[str, str]] = None) -> float:
        """Obtiene valor de contador"""
        with self._lock:
            key = self._make_key(name, labels)
            return self._counters.get(key, 0.0)
    
    def get_gauge(self, name: str, labels: Optional[Dict[str, str]] = None) -> Optional[float]:
        """Obtiene valor de gauge"""
        with self._lock:
            key = self._make_key(name, labels)
            return self._gauges.get(key)
    
    def get_timer_stats(self, name: str, labels: Optional[Dict[str, str]] = None) -> Dict[str, float]:
        """Obtiene estadísticas de timer"""
        with self._lock:
            key = self._make_key(name, labels)
            timers = list(self._timers[key])
            
            if not timers:
                return {}
            
            durations = [t['duration_ms'] for t in timers]
            durations.sort()
            
            return {
                'count': len(durations),
                'min_ms': min(durations),
                'max_ms': max(durations),
                'avg_ms': sum(durations) / len(durations),
                'p50_ms': durations[len(durations) // 2],
                'p95_ms': durations[int(len(durations) * 0.95)],
                'p99_ms': durations[int(len(durations) * 0.99)]
            }
    
    def get_all_metrics(self) -> Dict[str, Any]:
        """Obtiene todas las métricas"""
        with self._lock:
            return {
                'counters': dict(self._counters),
                'gauges': dict(self._gauges),
                'histograms': {
                    name: [{'upper_bound': b.upper_bound, 'count': b.count} for b in buckets]
                    for name, buckets in self._histograms.items()
                },
                'timers': {
                    name: self.get_timer_stats(name.split('|')[0], self._parse_labels(name))
                    for name in self._timers.keys()
                },
                'timestamp': datetime.now().isoformat()
            }
    
    def _make_key(self, name: str, labels: Optional[Dict[str, str]]) -> str:
        """Crea clave única para métrica con labels"""
        if not labels:
            return name
        
        label_str = ','.join(f'{k}={v}' for k, v in sorted(labels.items()))
        return f"{name}|{label_str}"
    
    def _parse_labels(self, key: str) -> Optional[Dict[str, str]]:
        """Parsea labels de una clave"""
        if '|' not in key:
            return None
        
        _, label_str = key.split('|', 1)
        labels = {}
        
        for pair in label_str.split(','):
            if '=' in pair:
                k, v = pair.split('=', 1)
                labels[k] = v
        
        return labels
    
    def _periodic_cleanup(self):
        """Limpieza periódica de métricas antiguas"""
        while True:
            try:
                time.sleep(300)  # Cada 5 minutos
                self._cleanup_old_metrics()
            except Exception as e:
                logging.error(f"Error in metrics cleanup: {e}")
    
    def _cleanup_old_metrics(self):
        """Limpia métricas antiguas"""
        # //optimizado de "métricas crecientes" a "retención con TTL" porque mantiene uso de memoria estable
        with self._lock:
            cutoff_time = datetime.now() - self._retention
            
            for metric_name, values in self._metrics.items():
                # Filtrar valores antiguos
                self._metrics[metric_name] = [
                    v for v in values if v.timestamp > cutoff_time
                ]
    
    def _collect_system_metrics(self):
        """Recolecta métricas del sistema"""
        while True:
            try:
                # Métricas de CPU
                cpu_percent = psutil.cpu_percent(interval=1)
                self.set_gauge('system_cpu_percent', cpu_percent)
                
                # Métricas de memoria
                memory = psutil.virtual_memory()
                self.set_gauge('system_memory_percent', memory.percent)
                self.set_gauge('system_memory_used_mb', memory.used / 1024 / 1024)
                
                # Métricas de proceso
                process = psutil.Process()
                process_memory = process.memory_info()
                self.set_gauge('process_memory_rss_mb', process_memory.rss / 1024 / 1024)
                self.set_gauge('process_memory_vms_mb', process_memory.vms / 1024 / 1024)
                self.set_gauge('process_cpu_percent', process.cpu_percent())
                
                time.sleep(30)  # Cada 30 segundos
                
            except Exception as e:
                logging.error(f"Error collecting system metrics: {e}")
                time.sleep(60)

class PerformanceMonitor:
    """Monitor de rendimiento para operaciones"""
    
    def __init__(self, metrics_collector: MetricsCollector, logger: StructuredLogger):
        self.metrics = metrics_collector
        self.logger = logger
    
    def time_operation(self, operation_name: str, labels: Optional[Dict[str, str]] = None):
        """Decorador/context manager para medir tiempo de operaciones"""
        return TimingContext(self.metrics, self.logger, operation_name, labels)

class TimingContext:
    """Context manager para medir tiempo de ejecución"""
    
    def __init__(self, metrics: MetricsCollector, logger: StructuredLogger, operation: str, labels: Optional[Dict[str, str]]):
        self.metrics = metrics
        self.logger = logger
        self.operation = operation
        self.labels = labels or {}
        self.start_time = None
    
    def __enter__(self):
        self.start_time = time.perf_counter()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.start_time is not None:
            duration_ms = (time.perf_counter() - self.start_time) * 1000
            
            # Registrar métricas
            self.metrics.record_timer(f"{self.operation}_duration", duration_ms, self.labels)
            self.metrics.increment_counter(f"{self.operation}_total", 1.0, self.labels)
            
            # Log estructurado
            success = exc_type is None
            if not success:
                self.labels['error_type'] = exc_type.__name__ if exc_type else 'unknown'
                self.metrics.increment_counter(f"{self.operation}_errors", 1.0, self.labels)
            
            self.logger.info(
                f"Operation {self.operation} completed",
                duration_ms=duration_ms,
                success=success,
                **self.labels
            )

# Instancias globales
_global_logger: Optional[StructuredLogger] = None
_global_metrics: Optional[MetricsCollector] = None
_global_monitor: Optional[PerformanceMonitor] = None

def get_logger(name: str = "fuzzy-service") -> StructuredLogger:
    """Obtiene logger estructurado"""
    global _global_logger
    if _global_logger is None:
        _global_logger = StructuredLogger(name)
    return _global_logger

def get_metrics() -> MetricsCollector:
    """Obtiene colector de métricas"""
    global _global_metrics
    if _global_metrics is None:
        _global_metrics = MetricsCollector()
    return _global_metrics

def get_monitor() -> PerformanceMonitor:
    """Obtiene monitor de rendimiento"""
    global _global_monitor
    if _global_monitor is None:
        _global_monitor = PerformanceMonitor(get_metrics(), get_logger())
    return _global_monitor

def monitor_operation(operation_name: str, labels: Optional[Dict[str, str]] = None):
    """Decorador para monitorear operaciones"""
    def decorator(func: Callable) -> Callable:
        def wrapper(*args, **kwargs):
            monitor = get_monitor()
            with monitor.time_operation(operation_name, labels):
                return func(*args, **kwargs)
        return wrapper
    return decorator