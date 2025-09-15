from enum import Enum

class PowerRange(Enum):
    """
    Rango configurado para potencia de salida (en porcentaje 0-100 por defecto).
    Permite seleccionar distintas políticas sin hardcodear números en entidades.
    """
    PERCENT_0_100 = (0.0, 100.0)

    @property
    def min(self) -> float:
        return float(self.value[0])

    @property
    def max(self) -> float:
        return float(self.value[1])


class DurationRange(Enum):
    """
    Rango configurado para duración (segundos). Por defecto 5-60.
    """
    SECONDS_5_60 = (0.5, 10000.0)

    @property
    def min(self) -> float:
        return float(self.value[0])

    @property
    def max(self) -> float:
        return float(self.value[1])
