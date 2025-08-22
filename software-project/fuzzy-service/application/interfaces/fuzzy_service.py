"""Interfaz para el servicio de lógica difusa.

Define el contrato para el procesamiento de lógica difusa
y evaluación de rutinas en el sistema hidropónico.
"""

from abc import ABC, abstractmethod
from typing import Dict, List, Optional

from domain.models import OutputPlan, Routine
from application.dtos import ReadingBatch, CreateCommandDto


class IFuzzyService(ABC):
    """Interfaz para el servicio de lógica difusa."""
    
    @abstractmethod
    async def process_readings(self, readings: ReadingBatch) -> List[CreateCommandDto]:
        """Procesa lecturas de sensores y genera comandos para actuadores.
        
        Args:
            readings: Lote de lecturas de sensores
            
        Returns:
            Lista de comandos para enviar a actuadores
        """
        pass
    
    @abstractmethod
    async def evaluate_routines(self, readings: ReadingBatch) -> List[CreateCommandDto]:
        """Evalúa rutinas activas contra lecturas de sensores.
        
        Args:
            readings: Lote de lecturas de sensores
            
        Returns:
            Lista de comandos generados de la evaluación de rutinas
        """
        pass
    
    @abstractmethod
    async def evaluate_readings(self, batch: ReadingBatch, routines: List[Routine]) -> List[OutputPlan]:
        """Evalúa lecturas usando lógica difusa y genera planes de acción.
        
        Args:
            batch: Lote de lecturas de sensores
            routines: Lista de rutinas configuradas
            
        Returns:
            Lista de planes de acción para actuadores
        """
        pass
    
    @abstractmethod
    async def get_fuzzy_variables_info(self) -> Dict[str, Dict]:
        """Obtiene información sobre las variables difusas configuradas.
        
        Returns:
            Diccionario con información de variables difusas
        """
        pass
    
    @abstractmethod
    async def simulate_evaluation(self, inputs: Dict[str, float]) -> Dict[str, float]:
        """Simula una evaluación difusa con valores de entrada específicos.
        
        Args:
            inputs: Diccionario con valores de entrada
            
        Returns:
            Diccionario con valores de salida calculados
        """
        pass
    
    @abstractmethod
    async def add_fuzzy_rule(self, rule_id: str, antecedents: List[tuple], 
                           consequent: tuple, weight: float = 1.0) -> bool:
        """Añade una nueva regla difusa al sistema.
        
        Args:
            rule_id: Identificador único de la regla
            antecedents: Lista de condiciones (variable, conjunto)
            consequent: Consecuente (variable, conjunto)
            weight: Peso de la regla
            
        Returns:
            True si la regla fue añadida exitosamente
        """
        pass
    
    @abstractmethod
    async def remove_fuzzy_rule(self, rule_id: str) -> bool:
        """Elimina una regla difusa del sistema.
        
        Args:
            rule_id: Identificador de la regla a eliminar
            
        Returns:
            True si la regla fue eliminada exitosamente
        """
        pass
    
    @abstractmethod
    async def get_active_rules(self) -> List[Dict]:
        """Obtiene información sobre las reglas difusas activas.
        
        Returns:
            Lista con información de reglas activas
        """
        pass
    
    @abstractmethod
    async def get_actuators_status(self) -> List[Dict]:
        """Obtiene el estado actual de todos los actuadores.
        
        Returns:
            Lista con información del estado de actuadores
        """
        pass
    
    @abstractmethod
    async def get_system_metrics(self) -> Dict:
        """Obtiene métricas del sistema difuso.
        
        Returns:
            Diccionario con métricas del sistema
        """
        pass