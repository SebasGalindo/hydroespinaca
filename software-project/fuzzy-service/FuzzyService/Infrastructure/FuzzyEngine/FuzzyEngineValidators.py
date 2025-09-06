from typing import Dict, List, Optional, Any, Union, Tuple, Set
from dataclasses import dataclass
from enum import Enum
import logging
import sys
import psutil
import time
from datetime import datetime, timezone
import re
import math
import numpy as np

from .FuzzyEngineExceptions import (
    ValidationException,
    PerformanceException,
    MembershipFunctionException,
    FuzzificationException,
    RuleEvaluationException,
    AggregationException,
    DefuzzificationException
)
from .FuzzyEngineConfiguration import (
    FuzzyEngineConfiguration,
    DefuzzificationMethod,
    AggregationMethod,
    LogicalOperator
)


class ValidationSeverity(Enum):
    """
    Niveles de severidad para las validaciones.
    
    - INFO: Información que no afecta el funcionamiento
    - WARNING: Advertencias que podrían causar problemas menores
    - ERROR: Errores que impiden el funcionamiento correcto
    - CRITICAL: Errores críticos que pueden causar fallos del sistema
    """
    INFO = "info"
    WARNING = "warning"
    ERROR = "error"
    CRITICAL = "critical"


@dataclass
class ValidationResult:
    """
    Resultado de una validación específica.
    
    Attributes:
        is_valid: Indica si la validación fue exitosa
        severity: Nivel de severidad del problema encontrado
        message: Descripción detallada del resultado
        field_name: Nombre del campo que se validó (opcional)
        expected_value: Valor esperado (opcional)
        actual_value: Valor actual encontrado (opcional)
        suggestion: Sugerencia para corregir el problema (opcional)
    """
    is_valid: bool
    severity: ValidationSeverity
    message: str
    field_name: Optional[str] = None
    expected_value: Optional[Any] = None
    actual_value: Optional[Any] = None
    suggestion: Optional[str] = None
    
    def __str__(self) -> str:
        """Representación en string del resultado de validación."""
        parts = [f"[{self.severity.value.upper()}] {self.message}"]
        
        if self.field_name:
            parts.append(f"Campo: {self.field_name}")
        
        if self.actual_value is not None:
            parts.append(f"Valor actual: {self.actual_value}")
        
        if self.expected_value is not None:
            parts.append(f"Valor esperado: {self.expected_value}")
        
        if self.suggestion:
            parts.append(f"Sugerencia: {self.suggestion}")
        
        return " | ".join(parts)


@dataclass
class ValidationReport:
    """
    Reporte completo de validaciones realizadas.
    
    Attributes:
        validation_results: Lista de todos los resultados de validación
        is_valid: Indica si todas las validaciones críticas pasaron
        total_validations: Número total de validaciones realizadas
        errors_count: Número de errores encontrados
        warnings_count: Número de advertencias encontradas
        validation_time_ms: Tiempo total de validación en milisegundos
        timestamp: Momento en que se realizó la validación
    """
    validation_results: List[ValidationResult]
    is_valid: bool
    total_validations: int
    errors_count: int
    warnings_count: int
    validation_time_ms: float
    timestamp: datetime
    
    def get_errors(self) -> List[ValidationResult]:
        """Obtiene solo los resultados que son errores o críticos."""
        return [
            result for result in self.validation_results 
            if result.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL]
        ]
    
    def get_warnings(self) -> List[ValidationResult]:
        """Obtiene solo los resultados que son advertencias."""
        return [
            result for result in self.validation_results 
            if result.severity == ValidationSeverity.WARNING
        ]
    
    def get_summary(self) -> str:
        """Genera un resumen textual del reporte de validación."""
        summary_parts = [
            f"Validación {'EXITOSA' if self.is_valid else 'FALLIDA'}",
            f"Total: {self.total_validations} validaciones",
            f"Errores: {self.errors_count}",
            f"Advertencias: {self.warnings_count}",
            f"Tiempo: {self.validation_time_ms:.2f}ms"
        ]
        return " | ".join(summary_parts)


class DataTypeValidator:
    """
    Validador especializado en tipos de datos y estructuras.
    
    Este validador se encarga de verificar que los datos tengan los tipos
    correctos y las estructuras esperadas para el motor fuzzy.
    """
    
    def __init__(self, config: FuzzyEngineConfiguration):
        """
        Inicializa el validador de tipos de datos.
        
        Args:
            config: Configuración del motor fuzzy con límites y restricciones
        """
        self.config = config
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
    
    def validate_sensor_data(self, sensor_data: Dict[str, float]) -> List[ValidationResult]:
        """
        Valida los datos de sensores de entrada.
        
        Verifica:
        - Que sea un diccionario
        - Que las claves sean strings válidos (IDs de sensores)
        - Que los valores sean números válidos
        - Que no haya valores infinitos o NaN
        - Que esté dentro de los límites de cantidad de sensores
        
        Args:
            sensor_data: Diccionario con datos de sensores {sensor_id: value}
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        
        # Validar que sea un diccionario
        if not isinstance(sensor_data, dict):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.CRITICAL,
                message="Los datos de sensores deben ser un diccionario",
                field_name="sensor_data",
                expected_value="dict",
                actual_value=type(sensor_data).__name__,
                suggestion="Proporcione un diccionario con formato {sensor_id: value}"
            ))
            return results
        
        # Validar que no esté vacío
        if not sensor_data:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="Los datos de sensores no pueden estar vacíos",
                field_name="sensor_data",
                suggestion="Proporcione al menos un sensor con su valor"
            ))
            return results
        
        # Validar límites de cantidad
        sensor_count = len(sensor_data)
        max_sensors = self.config.performance_limits.max_sensors_per_evaluation
        
        if sensor_count > max_sensors:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"Demasiados sensores en una evaluación",
                field_name="sensor_data",
                expected_value=f"<= {max_sensors}",
                actual_value=sensor_count,
                suggestion=f"Reduzca el número de sensores a {max_sensors} o menos"
            ))
        
        # Validar cada sensor individualmente
        for sensor_id, value in sensor_data.items():
            sensor_results = self._validate_single_sensor(sensor_id, value)
            results.extend(sensor_results)
        
        return results
    
    def _validate_single_sensor(self, sensor_id: str, value: float) -> List[ValidationResult]:
        """
        Valida un sensor individual.
        
        Args:
            sensor_id: Identificador del sensor
            value: Valor del sensor
        
        Returns:
            Lista de resultados de validación para este sensor
        """
        results = []
        
        # Validar ID del sensor
        if not isinstance(sensor_id, str):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"El ID del sensor debe ser una cadena de texto",
                field_name=f"sensor_id",
                expected_value="str",
                actual_value=type(sensor_id).__name__,
                suggestion="Use un string como identificador del sensor"
            ))
        elif not sensor_id.strip():
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"El ID del sensor no puede estar vacío",
                field_name=f"sensor_id",
                suggestion="Proporcione un identificador válido para el sensor"
            ))
        elif not re.match(r'^[a-zA-Z0-9_-]+$', sensor_id):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.WARNING,
                message=f"El ID del sensor contiene caracteres no recomendados",
                field_name=f"sensor_id",
                actual_value=sensor_id,
                suggestion="Use solo letras, números, guiones y guiones bajos"
            ))
        
        # Validar valor del sensor
        if not isinstance(value, (int, float)):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"El valor del sensor '{sensor_id}' debe ser numérico",
                field_name=f"sensor_data[{sensor_id}]",
                expected_value="int o float",
                actual_value=type(value).__name__,
                suggestion="Proporcione un valor numérico para el sensor"
            ))
        else:
            # Validar que no sea infinito o NaN
            if math.isinf(value):
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"El valor del sensor '{sensor_id}' no puede ser infinito",
                    field_name=f"sensor_data[{sensor_id}]",
                    actual_value=value,
                    suggestion="Proporcione un valor finito para el sensor"
                ))
            elif math.isnan(value):
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"El valor del sensor '{sensor_id}' no puede ser NaN",
                    field_name=f"sensor_data[{sensor_id}]",
                    actual_value=value,
                    suggestion="Proporcione un valor numérico válido para el sensor"
                ))
            
            # Validar rangos razonables (advertencia)
            if abs(value) > 1e6:
                results.append(ValidationResult(
                    is_valid=True,
                    severity=ValidationSeverity.WARNING,
                    message=f"El valor del sensor '{sensor_id}' es muy grande",
                    field_name=f"sensor_data[{sensor_id}]",
                    actual_value=value,
                    suggestion="Verifique que el valor esté en el rango esperado"
                ))
        
        return results
    
    def validate_membership_functions(self, membership_functions: Dict[str, Dict[str, Any]]) -> List[ValidationResult]:
        """
        Valida la estructura de las funciones de membresía.
        
        Verifica:
        - Estructura correcta del diccionario anidado
        - Presencia de campos requeridos en cada función
        - Tipos de datos correctos
        - Parámetros válidos para cada tipo de función
        
        Args:
            membership_functions: Diccionario {variable_name: {term_name: function_def}}
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        
        # Validar estructura principal
        if not isinstance(membership_functions, dict):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.CRITICAL,
                message="Las funciones de membresía deben ser un diccionario",
                field_name="membership_functions",
                expected_value="dict",
                actual_value=type(membership_functions).__name__,
                suggestion="Use formato {variable_name: {term_name: function_definition}}"
            ))
            return results
        
        if not membership_functions:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="Debe definir al menos una función de membresía",
                field_name="membership_functions",
                suggestion="Agregue funciones de membresía para las variables fuzzy"
            ))
            return results
        
        # Validar cada variable
        for variable_name, terms in membership_functions.items():
            variable_results = self._validate_variable_membership_functions(variable_name, terms)
            results.extend(variable_results)
        
        return results
    
    def _validate_variable_membership_functions(self, variable_name: str, terms: Dict[str, Any]) -> List[ValidationResult]:
        """
        Valida las funciones de membresía de una variable específica.
        
        Args:
            variable_name: Nombre de la variable fuzzy
            terms: Diccionario con los términos y sus definiciones
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        
        # Validar nombre de variable
        if not isinstance(variable_name, str) or not variable_name.strip():
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="El nombre de la variable debe ser una cadena no vacía",
                field_name="variable_name",
                actual_value=variable_name,
                suggestion="Use un nombre descriptivo para la variable"
            ))
        
        # Validar estructura de términos
        if not isinstance(terms, dict):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"Los términos de '{variable_name}' deben ser un diccionario",
                field_name=f"membership_functions[{variable_name}]",
                expected_value="dict",
                actual_value=type(terms).__name__,
                suggestion="Use formato {term_name: function_definition}"
            ))
            return results
        
        if not terms:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"La variable '{variable_name}' debe tener al menos un término",
                field_name=f"membership_functions[{variable_name}]",
                suggestion="Defina al menos un término fuzzy para la variable"
            ))
            return results
        
        # Validar cada término
        for term_name, function_def in terms.items():
            term_results = self._validate_membership_function_definition(variable_name, term_name, function_def)
            results.extend(term_results)
        
        return results
    
    def _validate_membership_function_definition(self, variable_name: str, term_name: str, function_def: Any) -> List[ValidationResult]:
        """
        Valida la definición de una función de membresía específica.
        
        Args:
            variable_name: Nombre de la variable
            term_name: Nombre del término
            function_def: Definición de la función de membresía
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        field_prefix = f"membership_functions[{variable_name}][{term_name}]"
        
        # Validar que sea un diccionario
        if not isinstance(function_def, dict):
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message=f"La definición del término '{term_name}' debe ser un diccionario",
                field_name=field_prefix,
                expected_value="dict",
                actual_value=type(function_def).__name__,
                suggestion="Use formato {type: 'triangular', params: [a, b, c]}"
            ))
            return results
        
        # Validar campos requeridos
        required_fields = ['type', 'params']
        for field in required_fields:
            if field not in function_def:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"Falta el campo '{field}' en la definición del término '{term_name}'",
                    field_name=f"{field_prefix}.{field}",
                    suggestion=f"Agregue el campo '{field}' a la definición"
                ))
        
        # Validar tipo de función
        if 'type' in function_def:
            function_type = function_def['type']
            valid_types = ['triangular', 'trapezoidal', 'gaussian', 'sigmoid', 'bell', 'pi', 'z', 's']
            
            if not isinstance(function_type, str):
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"El tipo de función debe ser una cadena",
                    field_name=f"{field_prefix}.type",
                    expected_value="str",
                    actual_value=type(function_type).__name__,
                    suggestion=f"Use uno de: {', '.join(valid_types)}"
                ))
            elif function_type not in valid_types:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"Tipo de función no válido: '{function_type}'",
                    field_name=f"{field_prefix}.type",
                    actual_value=function_type,
                    suggestion=f"Use uno de: {', '.join(valid_types)}"
                ))
        
        # Validar parámetros
        if 'params' in function_def:
            params = function_def['params']
            if not isinstance(params, list):
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message=f"Los parámetros deben ser una lista",
                    field_name=f"{field_prefix}.params",
                    expected_value="list",
                    actual_value=type(params).__name__,
                    suggestion="Use una lista de números como parámetros"
                ))
            else:
                # Validar que todos los parámetros sean numéricos
                for i, param in enumerate(params):
                    if not isinstance(param, (int, float)):
                        results.append(ValidationResult(
                            is_valid=False,
                            severity=ValidationSeverity.ERROR,
                            message=f"El parámetro {i} debe ser numérico",
                            field_name=f"{field_prefix}.params[{i}]",
                            expected_value="int o float",
                            actual_value=type(param).__name__,
                            suggestion="Use solo valores numéricos en los parámetros"
                        ))
                    elif math.isinf(param) or math.isnan(param):
                        results.append(ValidationResult(
                            is_valid=False,
                            severity=ValidationSeverity.ERROR,
                            message=f"El parámetro {i} no puede ser infinito o NaN",
                            field_name=f"{field_prefix}.params[{i}]",
                            actual_value=param,
                            suggestion="Use valores finitos en los parámetros"
                        ))
        
        return results


class PerformanceValidator:
    """
    Validador especializado en aspectos de rendimiento y recursos del sistema.
    
    Este validador se encarga de verificar que el sistema tenga suficientes
    recursos para procesar las solicitudes y que no se excedan los límites
    de rendimiento configurados.
    """
    
    def __init__(self, config: FuzzyEngineConfiguration):
        """
        Inicializa el validador de rendimiento.
        
        Args:
            config: Configuración del motor fuzzy con límites de rendimiento
        """
        self.config = config
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
    
    def validate_system_resources(self) -> List[ValidationResult]:
        """
        Valida que el sistema tenga suficientes recursos disponibles.
        
        Verifica:
        - Memoria RAM disponible
        - Uso de CPU
        - Espacio en disco (si es necesario)
        - Límites del sistema operativo
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        
        try:
            # Validar memoria RAM
            memory_results = self._validate_memory_usage()
            results.extend(memory_results)
            
            # Validar uso de CPU
            cpu_results = self._validate_cpu_usage()
            results.extend(cpu_results)
            
            # Validar límites del sistema
            system_results = self._validate_system_limits()
            results.extend(system_results)
            
        except Exception as e:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.WARNING,
                message=f"Error validando recursos del sistema: {e}",
                suggestion="Verifique manualmente los recursos del sistema"
            ))
        
        return results
    
    def _validate_memory_usage(self) -> List[ValidationResult]:
        """
        Valida el uso de memoria del sistema.
        
        Returns:
            Lista de resultados de validación de memoria
        """
        results = []
        
        try:
            # Obtener información de memoria
            memory = psutil.virtual_memory()
            available_mb = memory.available / (1024 * 1024)
            usage_percent = memory.percent
            
            # Verificar memoria disponible mínima
            min_memory_mb = self.config.performance_limits.min_available_memory_mb
            if available_mb < min_memory_mb:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="Memoria insuficiente para operaciones fuzzy",
                    field_name="system_memory",
                    expected_value=f">= {min_memory_mb} MB",
                    actual_value=f"{available_mb:.1f} MB",
                    suggestion="Libere memoria o aumente los recursos del sistema"
                ))
            
            # Advertir sobre uso alto de memoria
            if usage_percent > 85:
                results.append(ValidationResult(
                    is_valid=True,
                    severity=ValidationSeverity.WARNING,
                    message="Uso de memoria del sistema es alto",
                    field_name="memory_usage",
                    actual_value=f"{usage_percent:.1f}%",
                    suggestion="Considere liberar memoria antes de operaciones intensivas"
                ))
            
            # Información sobre memoria disponible
            results.append(ValidationResult(
                is_valid=True,
                severity=ValidationSeverity.INFO,
                message=f"Memoria disponible: {available_mb:.1f} MB ({100-usage_percent:.1f}% libre)",
                field_name="memory_status"
            ))
            
        except Exception as e:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.WARNING,
                message=f"No se pudo verificar el uso de memoria: {e}",
                suggestion="Verifique manualmente la memoria disponible"
            ))
        
        return results
    
    def _validate_cpu_usage(self) -> List[ValidationResult]:
        """
        Valida el uso de CPU del sistema.
        
        Returns:
            Lista de resultados de validación de CPU
        """
        results = []
        
        try:
            # Obtener uso de CPU (promedio de 1 segundo)
            cpu_percent = psutil.cpu_percent(interval=1)
            cpu_count = psutil.cpu_count()
            
            # Advertir sobre uso alto de CPU
            if cpu_percent > 90:
                results.append(ValidationResult(
                    is_valid=True,
                    severity=ValidationSeverity.WARNING,
                    message="Uso de CPU del sistema es muy alto",
                    field_name="cpu_usage",
                    actual_value=f"{cpu_percent:.1f}%",
                    suggestion="Espere a que disminuya la carga de CPU o use menos paralelismo"
                ))
            elif cpu_percent > 75:
                results.append(ValidationResult(
                    is_valid=True,
                    severity=ValidationSeverity.INFO,
                    message="Uso de CPU del sistema es moderadamente alto",
                    field_name="cpu_usage",
                    actual_value=f"{cpu_percent:.1f}%",
                    suggestion="Considere reducir el paralelismo si el rendimiento se degrada"
                ))
            
            # Información sobre CPU
            results.append(ValidationResult(
                is_valid=True,
                severity=ValidationSeverity.INFO,
                message=f"CPU: {cpu_count} núcleos, uso actual {cpu_percent:.1f}%",
                field_name="cpu_status"
            ))
            
        except Exception as e:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.WARNING,
                message=f"No se pudo verificar el uso de CPU: {e}",
                suggestion="Verifique manualmente la carga de CPU"
            ))
        
        return results
    
    def _validate_system_limits(self) -> List[ValidationResult]:
        """
        Valida los límites del sistema operativo.
        
        Returns:
            Lista de resultados de validación de límites del sistema
        """
        results = []
        
        try:
            # Verificar límite de recursión de Python
            recursion_limit = sys.getrecursionlimit()
            if recursion_limit < 1000:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.WARNING,
                    message="Límite de recursión de Python es bajo",
                    field_name="recursion_limit",
                    expected_value=">= 1000",
                    actual_value=recursion_limit,
                    suggestion="Aumente el límite con sys.setrecursionlimit()"
                ))
            
            # Información sobre la versión de Python
            python_version = sys.version_info
            if python_version < (3, 8):
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="Versión de Python no soportada",
                    field_name="python_version",
                    expected_value=">= 3.8",
                    actual_value=f"{python_version.major}.{python_version.minor}",
                    suggestion="Actualice a Python 3.8 o superior"
                ))
            
        except Exception as e:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.WARNING,
                message=f"Error validando límites del sistema: {e}",
                suggestion="Verifique manualmente la configuración del sistema"
            ))
        
        return results
    
    def validate_processing_limits(self, 
                                 sensor_count: int, 
                                 rule_count: int, 
                                 concurrent_requests: int) -> List[ValidationResult]:
        """
        Valida que los límites de procesamiento no se excedan.
        
        Args:
            sensor_count: Número de sensores a procesar
            rule_count: Número de reglas a evaluar
            concurrent_requests: Número de solicitudes concurrentes
        
        Returns:
            Lista de resultados de validación
        """
        results = []
        
        # Validar límite de sensores
        max_sensors = self.config.performance_limits.max_sensors_per_evaluation
        if sensor_count > max_sensors:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="Excede el límite máximo de sensores por evaluación",
                field_name="sensor_count",
                expected_value=f"<= {max_sensors}",
                actual_value=sensor_count,
                suggestion=f"Reduzca el número de sensores a {max_sensors} o menos"
            ))
        
        # Validar límite de reglas
        max_rules = self.config.performance_limits.max_rules_per_evaluation
        if rule_count > max_rules:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="Excede el límite máximo de reglas por evaluación",
                field_name="rule_count",
                expected_value=f"<= {max_rules}",
                actual_value=rule_count,
                suggestion=f"Reduzca el número de reglas a {max_rules} o menos"
            ))
        
        # Validar límite de concurrencia
        max_concurrent = self.config.performance_limits.max_concurrent_evaluations
        if concurrent_requests >= max_concurrent:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.ERROR,
                message="Excede el límite máximo de evaluaciones concurrentes",
                field_name="concurrent_requests",
                expected_value=f"< {max_concurrent}",
                actual_value=concurrent_requests,
                suggestion="Espere a que terminen algunas evaluaciones antes de iniciar nuevas"
            ))
        
        # Advertencias sobre carga alta
        if sensor_count > max_sensors * 0.8:
            results.append(ValidationResult(
                is_valid=True,
                severity=ValidationSeverity.WARNING,
                message="Número de sensores cercano al límite máximo",
                field_name="sensor_count",
                actual_value=sensor_count,
                suggestion="Considere optimizar el número de sensores"
            ))
        
        if rule_count > max_rules * 0.8:
            results.append(ValidationResult(
                is_valid=True,
                severity=ValidationSeverity.WARNING,
                message="Número de reglas cercano al límite máximo",
                field_name="rule_count",
                actual_value=rule_count,
                suggestion="Considere optimizar el número de reglas"
            ))
        
        return results


class FuzzyEngineValidator:
    """
    Validador principal del motor fuzzy que coordina todas las validaciones.
    
    Este es el punto de entrada principal para todas las validaciones del motor fuzzy.
    Coordina los diferentes validadores especializados y genera reportes completos.
    """
    
    def __init__(self, config: FuzzyEngineConfiguration, metrics=None):
        """
        Inicializa el validador principal del motor fuzzy.
        
        Args:
            config: Configuración del motor fuzzy
            metrics: Métricas del motor fuzzy (opcional)
        """
        self.config = config
        self.metrics = metrics
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        
        # Inicializar validadores especializados
        self.data_type_validator = DataTypeValidator(config)
        self.performance_validator = PerformanceValidator(config)
    
    def validate_evaluation_request(self, 
                                  sensor_data: Dict[str, float],
                                  membership_functions: Dict[str, Dict[str, Any]],
                                  rules: List[Any],
                                  concurrent_requests: int = 0) -> ValidationReport:
        """
        Valida completamente una solicitud de evaluación fuzzy.
        
        Esta es la función principal que debe llamarse antes de procesar
        cualquier solicitud de evaluación fuzzy. Realiza todas las validaciones
        necesarias y genera un reporte completo.
        
        Args:
            sensor_data: Datos de sensores a procesar
            membership_functions: Definiciones de funciones de membresía
            rules: Lista de reglas fuzzy a evaluar
            concurrent_requests: Número actual de solicitudes concurrentes
        
        Returns:
            Reporte completo de validación con todos los resultados
        
        Raises:
            ValidationException: Si hay errores críticos que impiden el procesamiento
        """
        start_time = time.perf_counter()
        all_results = []
        
        self.logger.debug("Iniciando validación completa de solicitud de evaluación fuzzy")
        
        try:
            # 1. Validar datos de sensores
            self.logger.debug("Validando datos de sensores")
            sensor_results = self.data_type_validator.validate_sensor_data(sensor_data)
            all_results.extend(sensor_results)
            
            # 2. Validar funciones de membresía
            self.logger.debug("Validando funciones de membresía")
            membership_results = self.data_type_validator.validate_membership_functions(membership_functions)
            all_results.extend(membership_results)
            
            # 3. Validar límites de procesamiento
            self.logger.debug("Validando límites de procesamiento")
            processing_results = self.performance_validator.validate_processing_limits(
                len(sensor_data),
                len(rules),
                concurrent_requests
            )
            all_results.extend(processing_results)
            
            # 4. Validar recursos del sistema (siempre habilitado por defecto)
            self.logger.debug("Validando recursos del sistema")
            system_results = self.performance_validator.validate_system_resources()
            all_results.extend(system_results)
            
            # 5. Calcular estadísticas del reporte
            validation_time = (time.perf_counter() - start_time) * 1000
            
            errors = [r for r in all_results if r.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL]]
            warnings = [r for r in all_results if r.severity == ValidationSeverity.WARNING]
            
            # Determinar si la validación es exitosa
            # Solo falla si hay errores críticos o errores que impiden el funcionamiento
            is_valid = len(errors) == 0
            
            # Crear reporte
            report = ValidationReport(
                validation_results=all_results,
                is_valid=is_valid,
                total_validations=len(all_results),
                errors_count=len(errors),
                warnings_count=len(warnings),
                validation_time_ms=validation_time,
                timestamp=datetime.now(timezone.utc)
            )
            
            # Log del resultado
            if is_valid:
                self.logger.info(f"Validación exitosa: {report.get_summary()}")
            else:
                self.logger.warning(f"Validación fallida: {report.get_summary()}")
                for error in errors:
                    self.logger.error(f"Error de validación: {error}")
            
            # Si hay errores críticos, lanzar excepción
            critical_errors = [r for r in errors if r.severity == ValidationSeverity.CRITICAL]
            if critical_errors:
                error_messages = [r.message for r in critical_errors]
                raise ValidationException(
                    f"Errores críticos de validación: {'; '.join(error_messages)}"
                )
            
            return report
            
        except Exception as e:
            validation_time = (time.perf_counter() - start_time) * 1000
            
            # Crear reporte de error
            error_result = ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.CRITICAL,
                message=f"Error interno durante la validación: {e}",
                suggestion="Verifique la configuración y los datos de entrada"
            )
            
            report = ValidationReport(
                validation_results=[error_result],
                is_valid=False,
                total_validations=1,
                errors_count=1,
                warnings_count=0,
                validation_time_ms=validation_time,
                timestamp=datetime.now(timezone.utc)
            )
            
            self.logger.error(f"Error durante validación: {e}")
            
            # Re-lanzar la excepción si es de validación, sino crear una nueva
            if isinstance(e, ValidationException):
                raise
            else:
                raise ValidationException(f"Error interno de validación: {e}") from e
    
    def validate_configuration(self) -> ValidationReport:
        """
        Valida la configuración del motor fuzzy.
        
        Returns:
            Reporte de validación de la configuración
        """
        start_time = time.perf_counter()
        results = []
        
        try:
            # Validar configuración de rendimiento
            perf_config = self.config.performance_limits
            
            if perf_config.max_sensors_per_evaluation <= 0:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="El límite máximo de sensores debe ser positivo",
                    field_name="performance.max_sensors_per_evaluation",
                    actual_value=perf_config.max_sensors_per_evaluation,
                    suggestion="Configure un valor positivo"
                ))
            
            if perf_config.max_rules_per_evaluation <= 0:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="El límite máximo de reglas debe ser positivo",
                    field_name="performance.max_rules_per_evaluation",
                    actual_value=perf_config.max_rules_per_evaluation,
                    suggestion="Configure un valor positivo"
                ))
            
            if perf_config.max_concurrent_evaluations <= 0:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="El límite de evaluaciones concurrentes debe ser positivo",
                    field_name="performance.max_concurrent_evaluations",
                    actual_value=perf_config.max_concurrent_evaluations,
                    suggestion="Configure un valor positivo"
                ))
            
            # Validar timeouts
            if perf_config.max_total_processing_time_ms <= 0:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.ERROR,
                    message="El timeout de procesamiento debe ser positivo",
                    field_name="performance.max_total_processing_time_ms",
                    actual_value=perf_config.max_total_processing_time_ms,
                    suggestion="Configure un valor positivo en milisegundos"
                ))
            
            # Validar configuración de cache
            cache_config = self.config.cache
            
            if cache_config.max_cache_size <= 0:
                results.append(ValidationResult(
                    is_valid=False,
                    severity=ValidationSeverity.WARNING,
                    message="El tamaño máximo de cache debe ser positivo",
                    field_name="cache.max_cache_size",
                    actual_value=cache_config.max_cache_size,
                    suggestion="Configure un valor positivo o deshabilite el cache"
                ))
            
            # Si no hay errores, agregar confirmación
            if not any(r.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL] for r in results):
                results.append(ValidationResult(
                    is_valid=True,
                    severity=ValidationSeverity.INFO,
                    message="Configuración del motor fuzzy es válida",
                    field_name="configuration"
                ))
            
        except Exception as e:
            results.append(ValidationResult(
                is_valid=False,
                severity=ValidationSeverity.CRITICAL,
                message=f"Error validando configuración: {e}",
                suggestion="Verifique la configuración del motor fuzzy"
            ))
        
        validation_time = (time.perf_counter() - start_time) * 1000
        errors = [r for r in results if r.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL]]
        warnings = [r for r in results if r.severity == ValidationSeverity.WARNING]
        
        return ValidationReport(
            validation_results=results,
            is_valid=len(errors) == 0,
            total_validations=len(results),
            errors_count=len(errors),
            warnings_count=len(warnings),
            validation_time_ms=validation_time,
            timestamp=datetime.now(timezone.utc)
        )
    
    def health_check(self) -> Dict[str, Any]:
        """
        Realiza un chequeo de salud completo del validador.
        
        Returns:
            Diccionario con el estado de salud del validador
        """
        try:
            # Validar configuración
            config_report = self.validate_configuration()
            
            # Validar recursos del sistema
            system_results = self.performance_validator.validate_system_resources()
            
            # Determinar estado general
            config_healthy = config_report.is_valid
            system_healthy = not any(
                r.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL] 
                for r in system_results
            )
            
            overall_healthy = config_healthy and system_healthy
            
            return {
                'status': 'healthy' if overall_healthy else 'unhealthy',
                'timestamp': datetime.now(timezone.utc).isoformat(),
                'configuration': {
                    'valid': config_healthy,
                    'errors': config_report.errors_count,
                    'warnings': config_report.warnings_count
                },
                'system_resources': {
                    'healthy': system_healthy,
                    'checks_performed': len(system_results),
                    'issues': [r.message for r in system_results if r.severity in [ValidationSeverity.ERROR, ValidationSeverity.CRITICAL]]
                },
                'validator_components': {
                    'data_type_validator': 'operational',
                    'performance_validator': 'operational'
                }
            }
            
        except Exception as e:
            return {
                'status': 'unhealthy',
                'timestamp': datetime.now(timezone.utc).isoformat(),
                'error': str(e),
                'message': 'Error durante health check del validador'
            }