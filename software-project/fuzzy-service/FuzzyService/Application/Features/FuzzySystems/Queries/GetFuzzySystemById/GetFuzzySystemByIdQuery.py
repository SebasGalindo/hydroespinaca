from __future__ import annotations

from pydantic import BaseModel, Field
from medyator import Query


class GetFuzzySystemByIdQuery(BaseModel, Query):
    id: str = Field(..., min_length=1)
