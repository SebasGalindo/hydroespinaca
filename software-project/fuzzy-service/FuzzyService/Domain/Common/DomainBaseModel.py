from __future__ import annotations
from typing import Any, Mapping, Iterable
from pydantic import BaseModel, ConfigDict


class DomainBaseModel(BaseModel):
    """
    BaseModel común para el dominio, pensado para integrarse con FastAPI y
    ofrecer validaciones consistentes.

    Además, provee compatibilidad de argumentos posicionales para escenarios
    legacy de tests o llamadas internas donde se instancian modelos con
    argumentos en orden de declaración de campos.
    """

    model_config = ConfigDict(
        validate_assignment=True,
        populate_by_name=True,
        use_enum_values=True,
        arbitrary_types_allowed=True,
        extra="forbid",
    )

    # Compatibilidad con argumentos posicionales (pydantic v2 solo acepta kwargs)
    def __init__(self, *args: Any, **kwargs: Any) -> None:  # type: ignore[override]
        if args:
            # Caso 1: un único arg posicional que es un mapping -> tratar como kwargs
            if len(args) == 1 and isinstance(args[0], Mapping):
                merged = dict(args[0])
                merged.update(kwargs)
                kwargs = merged
            else:
                # Caso 2: args posicionales por orden de campos declarados
                field_names: list[str] = list(self.__class__.__pydantic_fields__.keys())  # type: ignore[attr-defined]
                if len(args) > len(field_names):
                    raise TypeError(
                        f"Too many positional arguments for {self.__class__.__name__}: "
                        f"expected at most {len(field_names)}, got {len(args)}"
                    )
                # Construir kwargs asignando por orden
                ordered_kwargs = {name: value for name, value in zip(field_names, args)}
                # kwargs explícitos tienen precedencia sobre posicionales
                ordered_kwargs.update(kwargs)
                kwargs = ordered_kwargs
        # Llamar al init de pydantic
        super().__init__(**kwargs)
