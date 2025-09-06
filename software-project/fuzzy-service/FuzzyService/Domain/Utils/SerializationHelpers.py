from __future__ import annotations
from datetime import datetime, timezone
from typing import Any, Optional

_DEF_TZ = timezone.utc


def extract_oid(value: Any) -> Optional[str]:
    """
    Extrae un id desde distintas representaciones estilo Mongo o strings planos.
    Admite: "abc", {"$oid": "abc"}, {"oid": "abc"}.
    Devuelve None si no puede extraerlo.
    """
    if value is None:
        return None
    if isinstance(value, str):
        return value
    if isinstance(value, dict):
        if "$oid" in value and isinstance(value["$oid"], str):
            return value["$oid"]
        if "oid" in value and isinstance(value["oid"], str):
            return value["oid"]
    return None


def parse_timestamp_utc(ts: Any) -> datetime:
    """
    Parsea timestamps ISO-8601 y asegura timezone-aware en UTC.
    Soporta sufijo 'Z' o desplazamientos tipo '+00:00'. Si falla, devuelve now(UTC).
    """
    if isinstance(ts, datetime):
        return ts if ts.tzinfo else ts.replace(tzinfo=_DEF_TZ)
    if isinstance(ts, str) and ts:
        s = ts.strip()
        if s.endswith("Z"):
            s = s[:-1] + "+00:00"
        try:
            dt = datetime.fromisoformat(s)
            return dt if dt.tzinfo else dt.replace(tzinfo=_DEF_TZ)
        except Exception:
            pass
    return datetime.now(_DEF_TZ)
