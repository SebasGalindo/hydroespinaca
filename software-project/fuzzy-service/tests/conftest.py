import os
import sys
import pathlib
import warnings
from dotenv import load_dotenv

# Ensure project root (where FuzzyService/ lives) is on sys.path
ROOT = pathlib.Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

# Cargar variables desde .env.test para todos los tests
load_dotenv(dotenv_path=ROOT / ".env.test")

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

# Suprimir warnings específicos de MQTT que aparecen en Windows
warnings.filterwarnings("ignore", category=pytest.PytestUnraisableExceptionWarning)
warnings.filterwarnings("ignore", message=".*on_socket_unregister_write.*")
warnings.filterwarnings("ignore", message=".*Client.__del__.*")

# Capturar y filtrar output de stderr para suprimir mensajes de socket MQTT
import io
from contextlib import redirect_stderr

class MQTTWarningFilter:
    """Filtro para suprimir mensajes específicos de MQTT."""
    
    def __init__(self, original_stderr):
        self.original_stderr = original_stderr
        self.buffer = io.StringIO()
        self.suppress_next = False
        
    def write(self, text):
        # Filtrar mensajes específicos de MQTT y líneas relacionadas
        if ('Caught exception in on_socket_unregister_write' in text or
            'on_socket_unregister_write' in text or 
            'Client.__del__' in text or
            'NotImplementedError' in text or
            '_reset_sockets' in text or
            '_sock_close' in text or
            'aiomqtt' in text or
            'paho.mqtt' in text):
            return  # Suprimir completamente
            
        self.original_stderr.write(text)
            
    def flush(self):
        self.original_stderr.flush()

# Aplicar el filtro globalmente
original_stderr = sys.stderr
sys.stderr = MQTTWarningFilter(sys.stderr)

@pytest.fixture(autouse=True)
def suppress_mqtt_stderr_output():
    """Fixture que suprime completamente el output de stderr durante tests E2E."""
    import os
    
    # Solo aplicar en tests E2E
    if 'e2e' in os.environ.get('PYTEST_CURRENT_TEST', ''):
        # Redirigir stderr a devnull durante el test
        with open(os.devnull, 'w') as devnull:
            old_stderr = sys.stderr
            sys.stderr = devnull
            try:
                yield
            finally:
                sys.stderr = old_stderr
    else:
        yield
