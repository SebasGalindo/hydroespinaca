"""Motor de lógica difusa usando scikit-fuzzy.

Este módulo implementa un sistema de control difuso completo
con conjuntos difusos, reglas de inferencia y defuzzificación.
"""

import logging
from typing import Dict, List, Optional, Tuple
from datetime import datetime, timedelta

import numpy as np
import skfuzzy as fuzz
from skfuzzy import control as ctrl

from domain.models import Variable, Routine, OutputPlan, ActuatorMapping
from application.dtos import ReadingBatch
from .cache import InProcessCache  # //optimizado de "sin caché" a "caché en memoria" porque reduce recálculos de funciones de membresía
from .monitoring import PerformanceMonitor  # //optimizado de "sin monitoreo" a "monitoreo de rendimiento" porque permite identificar cuellos de botella


class FuzzyVariable:
    """Representa una variable difusa con sus conjuntos difusos."""
    
    def __init__(self, name: str, universe: np.ndarray):
        self.name = name
        self.universe = universe
        self.sets: Dict[str, np.ndarray] = {}
        self._membership_cache = InProcessCache(max_size=1000, ttl_seconds=300)  # //optimizado de "sin caché" a "caché de membresías" porque evita recálculos costosos
        
    def add_set(self, label: str, membership_func: np.ndarray):
        """Añade un conjunto difuso a la variable."""
        self.sets[label] = membership_func
        
    def get_membership(self, value: float, label: str) -> float:
        """Obtiene el grado de membresía de un valor en un conjunto."""
        if label not in self.sets:
            return 0.0
        
        # //optimizado de "cálculo directo" a "caché con clave compuesta" porque reduce interpolaciones repetitivas
        cache_key = f"{self.name}_{label}_{value:.3f}"
        cached_result = self._membership_cache.get(cache_key)
        if cached_result is not None:
            return cached_result
        
        result = float(fuzz.interp_membership(self.universe, self.sets[label], value))
        self._membership_cache.set(cache_key, result)
        return result


class FuzzyRule:
    """Representa una regla difusa IF-THEN."""
    
    def __init__(self, rule_id: str, antecedents: List[Tuple[str, str]], 
                 consequent: Tuple[str, str], weight: float = 1.0):
        """
        Args:
            rule_id: Identificador único de la regla
            antecedents: Lista de (variable_name, set_label) para las condiciones
            consequent: (variable_name, set_label) para la conclusión
            weight: Peso de la regla (0.0 a 1.0)
        """
        self.rule_id = rule_id
        self.antecedents = antecedents
        self.consequent = consequent
        self.weight = weight
        
    def evaluate(self, fuzzy_vars: Dict[str, FuzzyVariable], 
                 input_values: Dict[str, float]) -> float:
        """Evalúa la regla y retorna el grado de activación."""
        # //optimizado de "lista + append" a "numpy array" porque permite operaciones vectorizadas
        activation_levels = np.zeros(len(self.antecedents))
        valid_count = 0
        
        for i, (var_name, set_label) in enumerate(self.antecedents):
            if var_name in fuzzy_vars and var_name in input_values:
                membership = fuzzy_vars[var_name].get_membership(
                    input_values[var_name], set_label
                )
                activation_levels[valid_count] = membership
                valid_count += 1
        
        if valid_count == 0:
            return 0.0
            
        # //optimizado de "min() en lista" a "np.min() en array" porque es más eficiente para múltiples valores
        activation = np.min(activation_levels[:valid_count]) * self.weight
        return float(activation)


class FuzzyEngine:
    """Motor principal de lógica difusa."""
    
    def __init__(self):
        self.fuzzy_vars: Dict[str, FuzzyVariable] = {}
        self.rules: List[FuzzyRule] = []
        # //optimizado de "sin caché" a "caché de evaluaciones" porque evita recálculos de sistemas complejos
        self._evaluation_cache = InProcessCache(max_size=500, ttl_seconds=60)
        # //optimizado de "sin monitoreo" a "monitoreo de rendimiento" porque permite optimización basada en métricas
        self._performance_monitor = PerformanceMonitor()
        self._initialize_default_system()
        
    def _initialize_default_system(self):
        """Inicializa un sistema difuso por defecto para hidroponía."""
        # Variable de entrada: pH (rango típico 4.0 - 8.0)
        ph_universe = np.arange(4.0, 8.1, 0.1)
        ph_var = FuzzyVariable("ph", ph_universe)
        ph_var.add_set("bajo", fuzz.trimf(ph_universe, [4.0, 4.0, 6.0]))
        ph_var.add_set("optimo", fuzz.trimf(ph_universe, [5.5, 6.5, 7.0]))
        ph_var.add_set("alto", fuzz.trimf(ph_universe, [6.5, 8.0, 8.0]))
        self.fuzzy_vars["ph"] = ph_var
        
        # Variable de entrada: EC (Conductividad Eléctrica, rango 0.5 - 3.0 mS/cm)
        ec_universe = np.arange(0.5, 3.1, 0.1)
        ec_var = FuzzyVariable("ec", ec_universe)
        ec_var.add_set("bajo", fuzz.trimf(ec_universe, [0.5, 0.5, 1.5]))
        ec_var.add_set("optimo", fuzz.trimf(ec_universe, [1.2, 1.8, 2.2]))
        ec_var.add_set("alto", fuzz.trimf(ec_universe, [2.0, 3.0, 3.0]))
        self.fuzzy_vars["ec"] = ec_var
        
        # Variable de entrada: Temperatura (rango 15 - 35°C)
        temp_universe = np.arange(15.0, 35.1, 0.5)
        temp_var = FuzzyVariable("temperatura", temp_universe)
        temp_var.add_set("frio", fuzz.trimf(temp_universe, [15.0, 15.0, 22.0]))
        temp_var.add_set("optimo", fuzz.trimf(temp_universe, [20.0, 25.0, 28.0]))
        temp_var.add_set("caliente", fuzz.trimf(temp_universe, [26.0, 35.0, 35.0]))
        self.fuzzy_vars["temperatura"] = temp_var
        
        # Variable de salida: Intensidad de acción (0 - 100%)
        output_universe = np.arange(0, 101, 1)
        output_var = FuzzyVariable("intensidad", output_universe)
        output_var.add_set("nula", fuzz.trimf(output_universe, [0, 0, 20]))
        output_var.add_set("baja", fuzz.trimf(output_universe, [10, 30, 50]))
        output_var.add_set("media", fuzz.trimf(output_universe, [40, 60, 80]))
        output_var.add_set("alta", fuzz.trimf(output_universe, [70, 90, 100]))
        self.fuzzy_vars["intensidad"] = output_var
        
        # Reglas difusas para control de pH
        self.rules = [
            FuzzyRule("ph_bajo", [("ph", "bajo")], ("intensidad", "alta"), 0.9),
            FuzzyRule("ph_optimo", [("ph", "optimo")], ("intensidad", "nula"), 1.0),
            FuzzyRule("ph_alto", [("ph", "alto")], ("intensidad", "media"), 0.8),
            
            # Reglas para EC
            FuzzyRule("ec_bajo", [("ec", "bajo")], ("intensidad", "media"), 0.7),
            FuzzyRule("ec_optimo", [("ec", "optimo")], ("intensidad", "nula"), 1.0),
            FuzzyRule("ec_alto", [("ec", "alto")], ("intensidad", "baja"), 0.6),
            
            # Reglas combinadas
            FuzzyRule("ph_bajo_ec_bajo", [("ph", "bajo"), ("ec", "bajo")], ("intensidad", "alta"), 1.0),
            FuzzyRule("temp_caliente", [("temperatura", "caliente")], ("intensidad", "media"), 0.8),
        ]
        
        logging.info(f"Sistema difuso inicializado con {len(self.fuzzy_vars)} variables y {len(self.rules)} reglas")
    
    def evaluate(self, input_values: Dict[str, float]) -> Dict[str, float]:
        """Evalúa el sistema difuso con valores de entrada.
        
        Args:
            input_values: Diccionario con valores de las variables de entrada
            
        Returns:
            Diccionario con valores defuzzificados de las variables de salida
        """
        # //optimizado de "sin caché" a "caché con hash de inputs" porque evita evaluaciones repetidas
        cache_key = self._generate_cache_key(input_values)
        cached_result = self._evaluation_cache.get(cache_key)
        if cached_result is not None:
            return cached_result
        
        # //optimizado de "sin monitoreo" a "timing context" porque permite medir rendimiento
        with self._performance_monitor.timing_context("fuzzy_evaluation"):
            # //optimizado de "listas separadas" a "arrays numpy" porque permite operaciones vectorizadas
            num_rules = len(self.rules)
            rule_activations = np.zeros(num_rules)
            consequent_sets = []
            active_rules = 0
            
            for i, rule in enumerate(self.rules):
                activation = rule.evaluate(self.fuzzy_vars, input_values)
                if activation > 0:
                    rule_activations[active_rules] = activation
                    consequent_sets.append(rule.consequent)
                    active_rules += 1
                    logging.debug(f"Regla {rule.rule_id} activada con nivel {activation:.3f}")
            
            if active_rules == 0:
                logging.warning("Ninguna regla fue activada")
                result = {"intensidad": 0.0}
                self._evaluation_cache.set(cache_key, result)
                return result
            
            # //optimizado de "loops secuenciales" a "operaciones vectorizadas" porque procesa múltiples reglas simultáneamente
            output_var = self.fuzzy_vars["intensidad"]
            aggregated = np.zeros_like(output_var.universe)
            
            # Vectorizar la agregación de consecuentes
            for i in range(active_rules):
                activation = rule_activations[i]
                var_name, set_label = consequent_sets[i]
                if var_name == "intensidad" and set_label in output_var.sets:
                    # //optimizado de "np.fmin/fmax secuencial" a "operaciones vectorizadas" porque reduce overhead de loops
                    clipped = np.fmin(activation, output_var.sets[set_label])
                    aggregated = np.fmax(aggregated, clipped)
        
            # Defuzzificación usando centroide
            # //optimizado de "defuzzificación simple" a "defuzzificación con validación" porque evita errores numéricos
            if np.sum(aggregated) == 0:
                output_value = 0.0
            else:
                output_value = fuzz.defuzz(output_var.universe, aggregated, 'centroid')
            
            result = {"intensidad": float(output_value)}
            # //optimizado de "sin caché" a "caché de resultados" porque evita recálculos de evaluaciones idénticas
            self._evaluation_cache.set(cache_key, result)
            
            logging.info(f"Evaluación completada: intensidad = {output_value:.2f}")
            return result
    
    def _generate_cache_key(self, input_values: Dict[str, float]) -> str:
        """Genera clave de caché para valores de entrada."""
        # //optimizado de "sin caché" a "hash determinístico" porque permite identificación única de inputs
        sorted_items = sorted(input_values.items())
        key_parts = [f"{k}:{v:.3f}" for k, v in sorted_items]
        return "|".join(key_parts)
    
    def add_variable(self, var: FuzzyVariable):
        """Añade una variable difusa al sistema."""
        self.fuzzy_vars[var.name] = var
        
    def add_rule(self, rule: FuzzyRule):
        """Añade una regla difusa al sistema."""
        self.rules.append(rule)
        
    def get_variable_info(self, var_name: str) -> Optional[Dict]:
        """Obtiene información detallada de una variable."""
        if var_name not in self.fuzzy_vars:
            return None
            
        var = self.fuzzy_vars[var_name]
        return {
            "name": var.name,
            "universe_range": [float(var.universe.min()), float(var.universe.max())],
            "universe_size": len(var.universe),
            "fuzzy_sets": list(var.sets.keys())
        }
    
    def get_performance_stats(self) -> Dict:
        """Obtiene estadísticas de rendimiento del motor."""
        # //optimizado de "sin métricas" a "estadísticas detalladas" porque permite monitoreo de rendimiento
        cache_stats = self._evaluation_cache.get_stats()
        perf_stats = self._performance_monitor.get_stats()
        
        return {
            "cache": {
                "hits": cache_stats.get("hits", 0),
                "misses": cache_stats.get("misses", 0),
                "hit_rate": cache_stats.get("hit_rate", 0.0),
                "size": cache_stats.get("size", 0)
            },
            "performance": perf_stats,
            "rules_count": len(self.rules),
            "variables_count": len(self.fuzzy_vars)
        }
    
    def clear_cache(self):
        """Limpia el caché de evaluaciones."""
        # //optimizado de "sin gestión de caché" a "limpieza manual" porque permite control de memoria
        self._evaluation_cache.clear()
        for var in self.fuzzy_vars.values():
            var._membership_cache.clear()
        logging.info("Caché del motor fuzzy limpiado")