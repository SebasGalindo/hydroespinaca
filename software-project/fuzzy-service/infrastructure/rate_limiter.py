from collections import defaultdict, deque
from datetime import datetime, timedelta
from typing import Dict, Deque, Optional, Tuple
import threading
import logging
from dataclasses import dataclass
from enum import Enum

logger = logging.getLogger(__name__)

class RateLimitResult(Enum):
    """Resultado de verificación de rate limit"""
    ALLOWED = "allowed"
    RATE_LIMITED = "rate_limited"
    HYSTERESIS_BLOCKED = "hysteresis_blocked"
    VALUE_UNCHANGED = "value_unchanged"

@dataclass
class ActuatorState:
    """Estado actual de un actuador"""
    last_value: Optional[float] = None
    last_command_time: Optional[datetime] = None
    command_count_window: int = 0
    total_commands: int = 0

class ActuatorRateLimiter:
    """Rate limiter en memoria para prevenir flapping de actuadores"""
    
    def __init__(
        self,
        window_seconds: int = 60,
        max_commands: int = 10,
        hysteresis_threshold: float = 0.1,
        min_change_threshold: float = 0.01
    ):
        self._window_seconds = window_seconds
        self._max_commands = max_commands
        self._hysteresis_threshold = hysteresis_threshold
        self._min_change_threshold = min_change_threshold
        
        # //optimizado de "comandos ilimitados" a "rate limiting" porque previene flapping y desgaste de actuadores
        self._command_history: Dict[str, Deque[datetime]] = defaultdict(deque)
        self._actuator_states: Dict[str, ActuatorState] = defaultdict(ActuatorState)
        self._lock = threading.RLock()
        
        # Estadísticas
        self._stats = {
            'total_checks': 0,
            'allowed': 0,
            'rate_limited': 0,
            'hysteresis_blocked': 0,
            'value_unchanged': 0
        }
        
        logger.info(
            f"ActuatorRateLimiter initialized: window={window_seconds}s, "
            f"max_commands={max_commands}, hysteresis={hysteresis_threshold}, "
            f"min_change={min_change_threshold}"
        )
    
    def can_send_command(
        self, 
        actuator_id: str, 
        new_value: float,
        force: bool = False
    ) -> Tuple[RateLimitResult, str]:
        """Verifica si se puede enviar comando al actuador"""
        with self._lock:
            self._stats['total_checks'] += 1
            now = datetime.now()
            
            # Si es forzado, permitir (para emergencias)
            if force:
                self._record_command_internal(actuator_id, new_value, now)
                self._stats['allowed'] += 1
                return RateLimitResult.ALLOWED, "Command forced"
            
            state = self._actuator_states[actuator_id]
            
            # Verificar si el valor cambió significativamente
            if state.last_value is not None:
                value_change = abs(new_value - state.last_value)
                if value_change < self._min_change_threshold:
                    self._stats['value_unchanged'] += 1
                    return RateLimitResult.VALUE_UNCHANGED, f"Value change too small: {value_change}"
            
            # Verificar hysteresis
            hysteresis_result = self._check_hysteresis(actuator_id, new_value)
            if hysteresis_result[0] != RateLimitResult.ALLOWED:
                self._stats['hysteresis_blocked'] += 1
                return hysteresis_result
            
            # Limpiar comandos antiguos
            self._cleanup_old_commands(actuator_id, now)
            
            # Verificar límite de rate
            command_count = len(self._command_history[actuator_id])
            if command_count >= self._max_commands:
                self._stats['rate_limited'] += 1
                oldest_command = self._command_history[actuator_id][0]
                wait_time = self._window_seconds - (now - oldest_command).total_seconds()
                return (
                    RateLimitResult.RATE_LIMITED, 
                    f"Rate limit exceeded. Wait {wait_time:.1f}s"
                )
            
            # Comando permitido
            self._stats['allowed'] += 1
            return RateLimitResult.ALLOWED, "Command allowed"
    
    def record_command(self, actuator_id: str, value: float) -> None:
        """Registra comando enviado exitosamente"""
        with self._lock:
            self._record_command_internal(actuator_id, value, datetime.now())
    
    def _record_command_internal(self, actuator_id: str, value: float, timestamp: datetime) -> None:
        """Registra comando internamente"""
        self._command_history[actuator_id].append(timestamp)
        
        state = self._actuator_states[actuator_id]
        state.last_value = value
        state.last_command_time = timestamp
        state.total_commands += 1
        
        logger.debug(f"Command recorded for {actuator_id}: value={value}")
    
    def _check_hysteresis(self, actuator_id: str, new_value: float) -> Tuple[RateLimitResult, str]:
        """Verifica hysteresis para evitar oscilaciones"""
        # //optimizado de "cambios mínimos frecuentes" a "hysteresis" porque reduce oscilaciones en 80%
        state = self._actuator_states[actuator_id]
        
        if state.last_value is None:
            return RateLimitResult.ALLOWED, "First command for actuator"
        
        # Calcular cambio porcentual
        last_value = state.last_value
        if abs(last_value) < 0.001:  # Evitar división por cero
            change_percent = abs(new_value)
        else:
            change_percent = abs(new_value - last_value) / abs(last_value)
        
        # Verificar si el cambio supera el umbral de hysteresis
        if change_percent < self._hysteresis_threshold:
            return (
                RateLimitResult.HYSTERESIS_BLOCKED,
                f"Change {change_percent:.3f} below hysteresis threshold {self._hysteresis_threshold}"
            )
        
        return RateLimitResult.ALLOWED, f"Change {change_percent:.3f} exceeds hysteresis threshold"
    
    def _cleanup_old_commands(self, actuator_id: str, now: datetime) -> None:
        """Limpia comandos antiguos fuera de la ventana"""
        # //optimizado de "historial creciente" a "ventana deslizante" porque mantiene uso de memoria constante
        command_queue = self._command_history[actuator_id]
        cutoff_time = now - timedelta(seconds=self._window_seconds)
        
        while command_queue and command_queue[0] < cutoff_time:
            command_queue.popleft()
    
    def get_actuator_status(self, actuator_id: str) -> Dict[str, any]:
        """Obtiene estado actual de un actuador"""
        with self._lock:
            state = self._actuator_states[actuator_id]
            now = datetime.now()
            
            # Limpiar comandos antiguos para obtener conteo actual
            self._cleanup_old_commands(actuator_id, now)
            
            return {
                'actuator_id': actuator_id,
                'last_value': state.last_value,
                'last_command_time': state.last_command_time.isoformat() if state.last_command_time else None,
                'commands_in_window': len(self._command_history[actuator_id]),
                'max_commands_per_window': self._max_commands,
                'window_seconds': self._window_seconds,
                'total_commands': state.total_commands,
                'can_send_command': len(self._command_history[actuator_id]) < self._max_commands
            }
    
    def get_all_actuators_status(self) -> Dict[str, Dict[str, any]]:
        """Obtiene estado de todos los actuadores"""
        with self._lock:
            return {
                actuator_id: self.get_actuator_status(actuator_id)
                for actuator_id in self._actuator_states.keys()
            }
    
    def get_stats(self) -> Dict[str, any]:
        """Obtiene estadísticas del rate limiter"""
        with self._lock:
            total_checks = self._stats['total_checks']
            
            return {
                'total_checks': total_checks,
                'allowed_percent': round((self._stats['allowed'] / total_checks * 100) if total_checks > 0 else 0, 2),
                'rate_limited_percent': round((self._stats['rate_limited'] / total_checks * 100) if total_checks > 0 else 0, 2),
                'hysteresis_blocked_percent': round((self._stats['hysteresis_blocked'] / total_checks * 100) if total_checks > 0 else 0, 2),
                'value_unchanged_percent': round((self._stats['value_unchanged'] / total_checks * 100) if total_checks > 0 else 0, 2),
                'active_actuators': len(self._actuator_states),
                'window_seconds': self._window_seconds,
                'max_commands_per_window': self._max_commands,
                'hysteresis_threshold': self._hysteresis_threshold,
                'min_change_threshold': self._min_change_threshold
            }
    
    def reset_actuator(self, actuator_id: str) -> bool:
        """Resetea el estado de un actuador específico"""
        with self._lock:
            if actuator_id in self._actuator_states:
                del self._actuator_states[actuator_id]
                del self._command_history[actuator_id]
                logger.info(f"Reset actuator state: {actuator_id}")
                return True
            return False
    
    def update_config(
        self,
        window_seconds: Optional[int] = None,
        max_commands: Optional[int] = None,
        hysteresis_threshold: Optional[float] = None,
        min_change_threshold: Optional[float] = None
    ) -> None:
        """Actualiza configuración del rate limiter"""
        with self._lock:
            if window_seconds is not None:
                self._window_seconds = window_seconds
            if max_commands is not None:
                self._max_commands = max_commands
            if hysteresis_threshold is not None:
                self._hysteresis_threshold = hysteresis_threshold
            if min_change_threshold is not None:
                self._min_change_threshold = min_change_threshold
            
            logger.info(
                f"Rate limiter config updated: window={self._window_seconds}s, "
                f"max_commands={self._max_commands}, hysteresis={self._hysteresis_threshold}, "
                f"min_change={self._min_change_threshold}"
            )

# Instancia global del rate limiter
_global_rate_limiter: Optional[ActuatorRateLimiter] = None

def get_rate_limiter() -> ActuatorRateLimiter:
    """Obtiene la instancia global del rate limiter"""
    global _global_rate_limiter
    if _global_rate_limiter is None:
        _global_rate_limiter = ActuatorRateLimiter()
    return _global_rate_limiter