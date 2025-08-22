from typing import Dict, Set, Optional, Any, Callable
from datetime import datetime, timedelta
import threading
import logging
import hashlib
import json
from dataclasses import dataclass
from enum import Enum

logger = logging.getLogger(__name__)

class CommandStatus(Enum):
    """Estado de procesamiento de comando"""
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"

@dataclass
class CommandRecord:
    """Registro de comando procesado"""
    command_id: str
    status: CommandStatus
    timestamp: datetime
    result: Optional[Any] = None
    error: Optional[str] = None
    execution_time_ms: Optional[float] = None

class IdempotentCommandProcessor:
    """Procesador de comandos idempotente"""
    
    def __init__(self, ttl_minutes: int = 60, max_records: int = 10000):
        self._processed_commands: Dict[str, CommandRecord] = {}
        self._ttl = timedelta(minutes=ttl_minutes)
        self._max_records = max_records
        self._lock = threading.RLock()
        
        # //optimizado de "comandos duplicados" a "idempotencia" porque evita acciones duplicadas y estados inconsistentes
        self._stats = {
            'total_commands': 0,
            'duplicate_commands': 0,
            'successful_commands': 0,
            'failed_commands': 0,
            'cleanup_runs': 0
        }
        
        # Hilo de limpieza automática
        self._cleanup_thread = threading.Thread(target=self._periodic_cleanup, daemon=True)
        self._cleanup_thread.start()
        
        logger.info(f"IdempotentCommandProcessor initialized with TTL={ttl_minutes}min, max_records={max_records}")
    
    def process_command(
        self, 
        command_id: str, 
        command_func: Callable[[], Any],
        command_data: Optional[Dict] = None
    ) -> Tuple[bool, Any, Optional[str]]:
        """Procesa comando si no ha sido procesado antes
        
        Returns:
            Tuple[bool, Any, Optional[str]]: (is_new_execution, result, error)
        """
        with self._lock:
            self._stats['total_commands'] += 1
            
            # Verificar si ya fue procesado
            if command_id in self._processed_commands:
                record = self._processed_commands[command_id]
                
                # Si está en progreso, esperar o retornar error
                if record.status == CommandStatus.PROCESSING:
                    self._stats['duplicate_commands'] += 1
                    return False, None, "Command is currently being processed"
                
                # Si ya fue completado, retornar resultado previo
                if record.status == CommandStatus.COMPLETED:
                    self._stats['duplicate_commands'] += 1
                    logger.debug(f"Command {command_id} already processed, returning cached result")
                    return False, record.result, None
                
                # Si falló previamente, permitir reintento
                if record.status == CommandStatus.FAILED:
                    logger.info(f"Retrying previously failed command {command_id}")
                    del self._processed_commands[command_id]
            
            # Limpiar comandos expirados antes de procesar
            self._cleanup_expired_commands()
            
            # Verificar límite de registros
            if len(self._processed_commands) >= self._max_records:
                self._cleanup_oldest_commands()
            
            # Marcar como en procesamiento
            start_time = datetime.now()
            self._processed_commands[command_id] = CommandRecord(
                command_id=command_id,
                status=CommandStatus.PROCESSING,
                timestamp=start_time
            )
        
        # Ejecutar comando fuera del lock para evitar bloqueos
        try:
            logger.debug(f"Executing command {command_id}")
            result = command_func()
            execution_time = (datetime.now() - start_time).total_seconds() * 1000
            
            # Actualizar registro con resultado exitoso
            with self._lock:
                if command_id in self._processed_commands:
                    record = self._processed_commands[command_id]
                    record.status = CommandStatus.COMPLETED
                    record.result = result
                    record.execution_time_ms = execution_time
                    self._stats['successful_commands'] += 1
            
            logger.debug(f"Command {command_id} completed successfully in {execution_time:.2f}ms")
            return True, result, None
            
        except Exception as e:
            error_msg = str(e)
            execution_time = (datetime.now() - start_time).total_seconds() * 1000
            
            # Actualizar registro con error
            with self._lock:
                if command_id in self._processed_commands:
                    record = self._processed_commands[command_id]
                    record.status = CommandStatus.FAILED
                    record.error = error_msg
                    record.execution_time_ms = execution_time
                    self._stats['failed_commands'] += 1
            
            logger.error(f"Command {command_id} failed after {execution_time:.2f}ms: {error_msg}")
            return True, None, error_msg
    
    def generate_command_id(self, operation: str, data: Dict) -> str:
        """Genera ID de comando determinístico basado en operación y datos"""
        # //optimizado de "IDs aleatorios" a "IDs determinísticos" porque permite idempotencia natural
        
        # Crear hash de los datos para garantizar unicidad
        data_str = json.dumps(data, sort_keys=True, default=str)
        data_hash = hashlib.sha256(data_str.encode()).hexdigest()[:16]
        
        # Incluir timestamp truncado para evitar colisiones a largo plazo
        timestamp = datetime.now().strftime("%Y%m%d%H%M")
        
        return f"{operation}_{timestamp}_{data_hash}"
    
    def get_command_status(self, command_id: str) -> Optional[CommandRecord]:
        """Obtiene estado de un comando específico"""
        with self._lock:
            return self._processed_commands.get(command_id)
    
    def is_command_processed(self, command_id: str) -> bool:
        """Verifica si un comando ya fue procesado exitosamente"""
        with self._lock:
            record = self._processed_commands.get(command_id)
            return record is not None and record.status == CommandStatus.COMPLETED
    
    def cancel_command(self, command_id: str) -> bool:
        """Cancela un comando en procesamiento (marca como fallido)"""
        with self._lock:
            record = self._processed_commands.get(command_id)
            if record and record.status == CommandStatus.PROCESSING:
                record.status = CommandStatus.FAILED
                record.error = "Command cancelled"
                logger.info(f"Command {command_id} cancelled")
                return True
            return False
    
    def _cleanup_expired_commands(self) -> int:
        """Limpia comandos expirados"""
        # //optimizado de "memoria creciente" a "limpieza automática" porque mantiene uso de memoria constante
        now = datetime.now()
        expired_commands = [
            cmd_id for cmd_id, record in self._processed_commands.items()
            if now - record.timestamp > self._ttl
        ]
        
        for cmd_id in expired_commands:
            del self._processed_commands[cmd_id]
        
        if expired_commands:
            logger.debug(f"Cleaned up {len(expired_commands)} expired commands")
        
        return len(expired_commands)
    
    def _cleanup_oldest_commands(self, count: Optional[int] = None) -> int:
        """Limpia los comandos más antiguos cuando se alcanza el límite"""
        # //optimizado de "límite duro" a "eviction LRU" porque mantiene comandos más recientes
        if not self._processed_commands:
            return 0
        
        # Calcular cuántos comandos eliminar
        if count is None:
            count = max(1, len(self._processed_commands) - self._max_records + 100)  # Dejar margen
        
        # Ordenar por timestamp (más antiguo primero)
        sorted_commands = sorted(
            self._processed_commands.items(),
            key=lambda x: x[1].timestamp
        )
        
        commands_to_remove = sorted_commands[:count]
        for cmd_id, _ in commands_to_remove:
            del self._processed_commands[cmd_id]
        
        logger.debug(f"Cleaned up {len(commands_to_remove)} oldest commands")
        return len(commands_to_remove)
    
    def _periodic_cleanup(self) -> None:
        """Limpieza periódica de comandos expirados"""
        import time
        
        while True:
            try:
                time.sleep(300)  # Limpiar cada 5 minutos
                with self._lock:
                    expired_count = self._cleanup_expired_commands()
                    self._stats['cleanup_runs'] += 1
                    
                    if expired_count > 0:
                        logger.debug(f"Periodic cleanup removed {expired_count} expired commands")
                        
            except Exception as e:
                logger.error(f"Error in periodic command cleanup: {e}")
    
    def get_stats(self) -> Dict[str, Any]:
        """Obtiene estadísticas del procesador"""
        with self._lock:
            total_commands = self._stats['total_commands']
            
            return {
                'total_commands': total_commands,
                'duplicate_commands': self._stats['duplicate_commands'],
                'successful_commands': self._stats['successful_commands'],
                'failed_commands': self._stats['failed_commands'],
                'duplicate_rate_percent': round(
                    (self._stats['duplicate_commands'] / total_commands * 100) if total_commands > 0 else 0, 2
                ),
                'success_rate_percent': round(
                    (self._stats['successful_commands'] / total_commands * 100) if total_commands > 0 else 0, 2
                ),
                'active_records': len(self._processed_commands),
                'max_records': self._max_records,
                'ttl_minutes': self._ttl.total_seconds() / 60,
                'cleanup_runs': self._stats['cleanup_runs']
            }
    
    def get_recent_commands(self, limit: int = 50) -> List[CommandRecord]:
        """Obtiene comandos recientes"""
        with self._lock:
            sorted_commands = sorted(
                self._processed_commands.values(),
                key=lambda x: x.timestamp,
                reverse=True
            )
            return sorted_commands[:limit]
    
    def clear_all_commands(self) -> int:
        """Limpia todos los comandos (usar con cuidado)"""
        with self._lock:
            count = len(self._processed_commands)
            self._processed_commands.clear()
            logger.warning(f"Cleared all {count} command records")
            return count

# Instancia global del procesador
_global_processor: Optional[IdempotentCommandProcessor] = None

def get_command_processor() -> IdempotentCommandProcessor:
    """Obtiene la instancia global del procesador de comandos"""
    global _global_processor
    if _global_processor is None:
        _global_processor = IdempotentCommandProcessor()
    return _global_processor

def idempotent_command(operation: str):
    """Decorador para hacer funciones idempotentes"""
    def decorator(func: Callable) -> Callable:
        def wrapper(*args, **kwargs):
            # Generar ID basado en función y argumentos
            data = {
                'function': func.__name__,
                'args': args,
                'kwargs': kwargs
            }
            
            processor = get_command_processor()
            command_id = processor.generate_command_id(operation, data)
            
            # Ejecutar de forma idempotente
            is_new, result, error = processor.process_command(
                command_id,
                lambda: func(*args, **kwargs)
            )
            
            if error:
                raise Exception(error)
            
            return result
        
        return wrapper
    return decorator