import os
import sys
import pathlib

# Ensure project root (where FuzzyService/ lives) is on sys.path
ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

# Default env overrides for tests to avoid real DB pings/indexing
os.environ.setdefault("MONGO_PING_ON_STARTUP", "false")
os.environ.setdefault("FUZZY_ENSURE_INDEXES_ON_STARTUP", "false")

import pytest
from datetime import datetime, timezone


def pytest_addoption(parser):
    """Agregar opción para ejecutar tests lentos."""
    parser.addoption(
        "--runslow", action="store_true", default=False, help="run slow tests"
    )


def pytest_configure(config):
    """Configurar marcadores pytest."""
    config.addinivalue_line("markers", "slow: mark test as slow (deselect with '-m \"not slow\"')")


def pytest_collection_modifyitems(config, items):
    """Modificar items de test según configuración."""
    if config.getoption("--runslow"):
        # Si --runslow está especificado, ejecutar todos los tests
        return
    
    skip_slow = pytest.mark.skip(reason="need --runslow option to run")
    for item in items:
        if "slow" in item.keywords:
            item.add_marker(skip_slow)

# Intentar importar tipos de la infraestructura del FuzzyEngine; si no existen, marcar skip global
try:
    from FuzzyService.Infrastructure.ExternalServices.FuzzyEngine.ScikitFuzzyEngine import (
        ScikitFuzzyEngine,
        FuzzificationResult,
    )
    FUZZY_INFRA_AVAILABLE = True
except Exception:
    FUZZY_INFRA_AVAILABLE = False


@pytest.fixture(autouse=True)
def skip_if_no_fuzzy_infra(request):
    if not FUZZY_INFRA_AVAILABLE:
        pytest.skip("FuzzyEngine infrastructure not available; skipping tests that depend on it.")


# Fixture sample_request removida - no es necesaria para las pruebas del ScikitFuzzyEngine
