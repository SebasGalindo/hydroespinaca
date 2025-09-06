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
