"""Implementaciones de repositorios.

Por ahora implementamos repositorios en memoria para desarrollo rápido.
Posteriormente se migrarán a MongoDB.
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Dict, List, Optional

from domain.models import ActuatorState, Routine, Variable


# ===== Interfaces (Domain layer) =====
class VariableRepository(ABC):
    """Interfaz del repositorio de variables."""
    
    @abstractmethod
    async def save(self, variable: Variable) -> None:
        pass
    
    @abstractmethod
    async def get_by_id(self, variable_id: str) -> Optional[Variable]:
        pass
    
    @abstractmethod
    async def exists(self, variable_id: str) -> bool:
        pass
    
    @abstractmethod
    async def list_all(self) -> List[Variable]:
        pass


class RoutineRepository(ABC):
    """Interfaz del repositorio de rutinas."""
    
    @abstractmethod
    async def save(self, routine: Routine) -> None:
        pass
    
    @abstractmethod
    async def get_by_id(self, routine_id: str) -> Optional[Routine]:
        pass
    
    @abstractmethod
    async def exists(self, routine_id: str) -> bool:
        pass
    
    @abstractmethod
    async def list_all(self) -> List[Routine]:
        pass
    
    @abstractmethod
    async def list_active(self) -> List[Routine]:
        pass


class ActuatorStateRepository(ABC):
    """Interfaz del repositorio de estados de actuadores."""
    
    @abstractmethod
    async def get_state(self, actuator_id: str) -> Optional[ActuatorState]:
        pass
    
    @abstractmethod
    async def save_state(self, actuator_id: str, state: ActuatorState) -> None:
        pass
    
    @abstractmethod
    async def get_all_states(self) -> Dict[str, ActuatorState]:
        pass


# ===== Implementaciones en memoria =====
class InMemoryVariableRepository(VariableRepository):
    """Repositorio de variables en memoria.
    
    Implementación temporal para desarrollo. Se migrará a MongoDB.
    """
    
    def __init__(self):
        self._variables: Dict[str, Variable] = {}
    
    async def save(self, variable: Variable) -> None:
        self._variables[variable.id] = variable
    
    async def get_by_id(self, variable_id: str) -> Optional[Variable]:
        return self._variables.get(variable_id)
    
    async def exists(self, variable_id: str) -> bool:
        return variable_id in self._variables
    
    async def list_all(self) -> List[Variable]:
        return list(self._variables.values())


class InMemoryRoutineRepository(RoutineRepository):
    """Repositorio de rutinas en memoria.
    
    Implementación temporal para desarrollo. Se migrará a MongoDB.
    """
    
    def __init__(self):
        self._routines: Dict[str, Routine] = {}
    
    async def save(self, routine: Routine) -> None:
        self._routines[routine.id] = routine
    
    async def get_by_id(self, routine_id: str) -> Optional[Routine]:
        return self._routines.get(routine_id)
    
    async def exists(self, routine_id: str) -> bool:
        return routine_id in self._routines
    
    async def list_all(self) -> List[Routine]:
        return list(self._routines.values())
    
    async def list_active(self) -> List[Routine]:
        return [r for r in self._routines.values() if r.active]


class InMemoryActuatorStateRepository(ActuatorStateRepository):
    """Repositorio de estados de actuadores en memoria.
    
    Mantiene el último estado conocido de cada actuador para
    implementar histeresis y cooldown.
    """
    
    def __init__(self):
        self._states: Dict[str, ActuatorState] = {}
    
    async def get_state(self, actuator_id: str) -> Optional[ActuatorState]:
        return self._states.get(actuator_id)
    
    async def save_state(self, actuator_id: str, state: ActuatorState) -> None:
        self._states[actuator_id] = state
    
    async def get_all_states(self) -> Dict[str, ActuatorState]:
        return self._states.copy()