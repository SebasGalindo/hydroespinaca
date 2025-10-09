import os
from typing import Dict, Optional

# Ensure infrastructure startup doesn't touch a real Mongo server
os.environ.setdefault("MONGO_PING_ON_STARTUP", "false")
os.environ.setdefault("FUZZY_ENSURE_INDEXES_ON_STARTUP", "false")

from fastapi.testclient import TestClient
from kink import di

from FuzzyService.Api.main import app
from FuzzyService.Application.Configuration import DependencyInjection as app_di
from FuzzyService.Domain.Entities.fuzzy_system import FuzzySystem
from FuzzyService.Domain.Enums import FuzzySystemStatus
from FuzzyService.Domain.ValueObjects.DomainId import FuzzySystemId
from FuzzyService.Domain.Interfaces.IFuzzySystemRepository import IFuzzySystemRepository


class InMemoryFuzzySystemRepo:  # lightweight test double
    def __init__(self) -> None:
        self._items: Dict[str, FuzzySystem] = {}

    async def create(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        new_id = FuzzySystemId()
        fuzzy_system.id = new_id
        self._items[str(new_id)] = fuzzy_system
        return fuzzy_system

    async def get_by_name(self, name: str) -> Optional[FuzzySystem]:
        for it in self._items.values():
            if it.name == name:
                return it
        return None

    async def get_by_id(self, system_id: FuzzySystemId) -> Optional[FuzzySystem]:
        return self._items.get(str(system_id))

    async def update(self, fuzzy_system: FuzzySystem) -> FuzzySystem:
        if fuzzy_system.id is None:
            raise ValueError("System must have id to update")
        key = str(fuzzy_system.id)
        if key not in self._items:
            raise KeyError("System not found")
        self._items[key] = fuzzy_system
        return fuzzy_system

    async def delete(self, system_id: FuzzySystemId) -> bool:
        return self._items.pop(str(system_id), None) is not None

    # Nuevo método requerido por UpdateFuzzySystemStatusHandler
    async def update_status(self, system_id: FuzzySystemId, status: FuzzySystemStatus) -> FuzzySystem:
        key = str(system_id)
        sys = self._items.get(key)
        if sys is None:
            raise KeyError("System not found")
        sys.status = status
        self._items[key] = sys
        return sys


def test_create_patch_status_and_get():
    # Ensure handlers are registered in DI (idempotent)
    app_di.configure_application_di()

    repo = InMemoryFuzzySystemRepo()

    with TestClient(app) as client:
        # Override repository after app startup
        di[IFuzzySystemRepository] = repo

        # Create system (starts as DRAFT by default when isActive=False)
        resp = client.post("/api/fuzzy-systems", json={"name": "Sys Status Demo", "isActive": False})
        
        print("="*40)
        print("Respuesta de creación:")
        print(resp.text)
        
        assert resp.status_code == 201, resp.text
        created = resp.json()
        assert created["name"] == "Sys Status Demo"
        assert created["status"] == "DRAFT"
        sys_id = created["id"]
        assert isinstance(sys_id, str) and len(sys_id) > 0

        # Patch status to ACTIVE
        resp2 = client.patch(f"/api/fuzzy-systems/{sys_id}/status", json={"status": "ACTIVE"})
        assert resp2.status_code == 200, resp2.text
        patched = resp2.json()
        assert patched["id"] == sys_id
        assert patched["status"] == "ACTIVE"

        # Get by ID should reflect ACTIVE
        resp3 = client.get(f"/api/fuzzy-systems/{sys_id}")
        assert resp3.status_code == 200, resp3.text
        fetched = resp3.json()
        assert fetched["id"] == sys_id
        assert fetched["status"] == "ACTIVE"
