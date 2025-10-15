'use client';

// ⭐ 1. Importa useMemo para la optimización de rendimiento.
import React, { useMemo } from 'react';
import { EditIcon, TrashIcon } from '@/components/ui/icons/Icons';
import type { ActuadorData } from '@hydroespinaca/shared'; 
import Table from '@/components/ui/Table';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';

interface ActuadoresTableProps {
  actuadores: ActuadorData[];
  onEdit: (actuadorId: string) => void;
  onDelete: (actuadorId: string) => void;
}

// Estas funciones auxiliares están bien, las movemos fuera para mayor limpieza.
const getStatusColor = (status: ActuadorData['status']): 'success' | 'default' | 'warning' | 'error' => {
  const colorMap: Record<ActuadorData['status'], 'success' | 'default' | 'warning' | 'error'> = {
    active: 'success',
    inactive: 'default',
    maintenance: 'warning',
    error: 'error',
  };
  return colorMap[status];
};

const getStatusText = (status: ActuadorData['status']) => {
  const textMap = { active: 'Activo', inactive: 'Inactivo', maintenance: 'Mantenimiento', error: 'Error' };
  return textMap[status] || 'Desconocido';
};

const getTypeText = (type: ActuadorData['type']) => {
  const typeMap = { pump: 'Bomba', valve: 'Válvula', fan: 'Ventilador', heater: 'Calefactor', light: 'Iluminación', motor: 'Motor' };
  return typeMap[type] || 'Desconocido';
};

const ActuadoresTable: React.FC<ActuadoresTableProps> = ({ actuadores, onEdit, onDelete }) => {
  
  // ⭐ 2. Envuelve la definición de columnas en useMemo.
  // Esto evita que el array se vuelva a crear en cada renderizado.
  const columns = useMemo(() => [
    {
      key: 'id',
      label: 'ID',
      // ⭐ 3. Acepta 'unknown' y usa una aserción de tipo 'as string'.
      render: (value: unknown) => (
        <span className="font-semibold text-gray-900">{value as string}</span>
      )
    },
    {
      key: 'name',
      label: 'Nombre',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    },
    {
      key: 'type',
      label: 'Tipo',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{getTypeText(value as ActuadorData['type'])}</span>
      )
    },
    {
      key: 'location',
      label: 'Ubicación',
      render: (value: unknown) => (
        <span className="text-gray-600 text-sm">{value as string}</span>
      )
    },
    {
      key: 'pin',
      label: 'Pin',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">GPIO {value as number}</span>
      )
    },
    {
      key: 'esp32Id',
      label: 'ESP32 ID',
      render: (value: unknown) => (
        <span className="font-mono text-xs text-gray-500">{value as string}</span>
      )
    },
    {
      key: 'status',
      label: 'Estado',
      render: (value: unknown) => {
        const status = value as ActuadorData['status'];
        return (
          <Badge variant={getStatusColor(status)}>
            {getStatusText(status)}
          </Badge>
        );
      }
    },
    {
      key: 'createdAt',
      label: 'Fecha Creación',
      render: (value: unknown) => (
        <span className="text-gray-600 text-sm">{value as string}</span>
      )
    },
    {
      key: 'actions',
      label: 'Acciones',
      // ⭐ 4. Usa el segundo parámetro 'row' para obtener datos de toda la fila.
      render: (_: unknown, row: unknown) => {
        const actuador = row as ActuadorData; // Afirmamos que la fila es de tipo ActuadorData
        return (
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => onEdit(actuador.id)} className="p-2 hover:bg-green-50">
              <EditIcon size={16} className="text-green-600" />
            </Button>
            <Button variant="ghost" size="sm" onClick={() => onDelete(actuador.id)} className="p-2 hover:bg-red-50">
              <TrashIcon size={16} className="text-red-600" />
            </Button>
          </div>
        );
      }
    }
  ], [onEdit, onDelete]); // Las dependencias de useMemo son las props que usa.

  return (
    <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
      {/* Ahora el prop 'columns' cumple con el tipo que espera la Tabla */}
      <Table
        data={actuadores}
        columns={columns}
        className="w-full"
      />
    </div>
  );
};

export default ActuadoresTable;