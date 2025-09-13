"""Comando para procesar lecturas de sensores recibidas por MQTT."""

from __future__ import annotations

from typing import List, Optional, Dict, Any
from datetime import datetime

from pydantic import Field, field_validator
from medyator import Command

from FuzzyService.Domain.Common import DomainBaseModel


class SensorReading(DomainBaseModel):
    """Lectura individual de un sensor."""
    
    sensor_id: str = Field(
        ..., 
        min_length=1, 
        max_length=100, 
        description="ID único del sensor"
    )
    value: float = Field(
        ..., 
        description="Valor leído del sensor"
    )
    timestamp: str = Field(
        ..., 
        description="Timestamp de la lectura en formato ISO"
    )
    metadata: Optional[Dict[str, Any]] = Field(
        default_factory=dict,
        description="Metadatos adicionales de la lectura"
    )
    
    @field_validator("sensor_id")
    @classmethod
    def validate_sensor_id(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El sensor_id no puede estar vacío")
        return v.strip()
    
    @field_validator("value")
    @classmethod
    def validate_value(cls, v: float) -> float:
        if not isinstance(v, (int, float)):
            raise ValueError("El valor debe ser numérico")
        if v != v or v == float('inf') or v == float('-inf'):  # NaN or infinity check
            raise ValueError("El valor debe ser finito")
        return float(v)
    
    @field_validator("timestamp")
    @classmethod
    def validate_timestamp(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El timestamp no puede estar vacío")
        # Validar que sea un timestamp válido
        try:
            datetime.fromisoformat(v.replace('Z', '+00:00'))
        except ValueError:
            raise ValueError("El timestamp debe estar en formato ISO válido")
        return v.strip()


class ProcessSensorReadingsCommand(DomainBaseModel, Command):
    """Comando para procesar un lote de lecturas de sensores.
    
    Este comando se envía cuando se reciben lecturas de sensores vía MQTT
    y necesitan ser procesadas por el motor fuzzy.
    """
    
    readings: List[SensorReading] = Field(
        ...,
        min_length=1,
        description="Lista de lecturas de sensores a procesar"
    )
    batch_timestamp: str = Field(
        ...,
        description="Timestamp del lote de lecturas"
    )
    esp32_id: Optional[str] = Field(
        default=None,
        description="ID del ESP32 que envió las lecturas"
    )
    metadata: Optional[Dict[str, Any]] = Field(
        default_factory=dict,
        description="Metadatos adicionales del lote"
    )
    
    # Campo para almacenar el resultado del procesamiento
    result: Optional[Dict[str, Any]] = Field(
        default=None,
        description="Resultado del procesamiento (uso interno del mediator)"
    )
    
    @field_validator("readings")
    @classmethod
    def validate_readings(cls, v: List[SensorReading]) -> List[SensorReading]:
        if not v:
            raise ValueError("Debe incluir al menos una lectura")
        
        # Validar que no haya sensor_ids duplicados
        sensor_ids = [reading.sensor_id for reading in v]
        if len(sensor_ids) != len(set(sensor_ids)):
            raise ValueError("No puede haber sensor_ids duplicados en el mismo lote")
        
        return v
    
    @field_validator("batch_timestamp")
    @classmethod
    def validate_batch_timestamp(cls, v: str) -> str:
        if not v or not v.strip():
            raise ValueError("El batch_timestamp no puede estar vacío")
        # Validar que sea un timestamp válido
        try:
            datetime.fromisoformat(v.replace('Z', '+00:00'))
        except ValueError:
            raise ValueError("El batch_timestamp debe estar en formato ISO válido")
        return v.strip()
    
    @field_validator("esp32_id")
    @classmethod
    def validate_esp32_id(cls, v: Optional[str]) -> Optional[str]:
        if v is not None:
            if not v.strip():
                raise ValueError("El esp32_id no puede estar vacío si se proporciona")
            return v.strip()
        return v