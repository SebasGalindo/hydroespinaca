'use client';

import React from 'react';
import { EditIcon, XIcon } from '@/components/ui/icons/Icons';
import { SensorData } from './SensorsConfigForm';
import Table from '@/components/ui/Table';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';

interface SensorsTableProps {
  sensors: SensorData[];
  onEdit: (sensorId: string) => void;
  onDelete: (sensorId: string) => void;
}

const SensorsTable: React.FC<SensorsTableProps> = ({ sensors, onEdit, onDelete }) => {
  const columns = [
    {
      key: 'id',
      label: 'Código',
      render: (value: unknown) => (
        <span className="font-semibold text-gray-900">{value as string}</span>
      )
    },
    {
      key: 'physicalId',
      label: 'ID Físico',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    },
    {
      key: 'location',
      label: 'Ubicación',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    },
    {
      key: 'esp32Id',
      label: 'ESP32 ID',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    },
    {
      key: 'readFrequency',
      label: 'Frecuencia (s)',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as number}</span>
      )
    },
    {
      key: 'variables',
      label: 'Variables',
      render: (value: unknown) => (
        <div className="flex flex-wrap gap-1">
          {(value as string[]).map((variable, index) => (
            <span key={index} className="inline-flex items-center justify-center rounded-full bg-gray-100 px-2.5 py-0.5 text-gray-700 font-medium">
              {variable}
            </span>
          ))}
        </div>
      )
    },
    {
      key: 'status',
      label: 'Estado',
      render: (value: unknown) => (
        <Badge 
          variant={(value as string) === 'active' ? 'success' : 'error'}
          size="sm"
        >
          {(value as string) === 'active' ? 'Activo' : 'Inactivo'}
        </Badge>
      )
    },
    {
      key: 'actions',
      label: 'Acciones',
      render: (_: unknown, sensor: unknown) => (
        <div className="flex items-center space-x-3">
          <button 
            onClick={() => onEdit((sensor as SensorData).id)} 
            className="text-gray-500 hover:text-gray-700 p-1"
            aria-label={`Editar sensor ${(sensor as SensorData).id}`}
          >
            <EditIcon className="h-4 w-4" />
          </button>
          <button 
            onClick={() => onDelete((sensor as SensorData).id)} 
            className="text-red-500 hover:text-red-700 p-1"
            aria-label={`Eliminar sensor ${(sensor as SensorData).id}`}
          >
            <XIcon className="h-4 w-4" />
          </button>
        </div>
      )
    }
  ];

  const mobileCardRender = (sensor: unknown) => {
    const sensorData = sensor as SensorData;
    return (
      <div className="p-6">
        <div className="flex justify-between items-start mb-3">
          <div>
            <h3 className="font-semibold text-gray-900 text-lg">{sensorData.id}</h3>
            <p className="text-sm text-gray-600 mt-1">
              <span className="font-medium">ID Físico:</span> {sensorData.physicalId}
            </p>
            <p className="text-sm text-gray-600">
              <span className="font-medium">Ubicación:</span> {sensorData.location}
            </p>
            <p className="text-sm text-gray-600">
              <span className="font-medium">Variables:</span> {sensorData.variables.join(', ')}
            </p>
          </div>
          <Badge 
            variant={sensorData.status === 'active' ? 'success' : 'error'}
            size="sm"
          >
            {sensorData.status === 'active' ? 'Activo' : 'Inactivo'}
          </Badge>
        </div>
        <div className="flex justify-end space-x-2">
          <Button
            onClick={() => onEdit(sensorData.id)}
            size="sm"
            variant="outline"
            className="flex items-center gap-2"
          >
            <EditIcon className="h-4 w-4" />
            Editar
          </Button>
          <Button
            onClick={() => onDelete(sensorData.id)}
            size="sm"
            variant="outline"
            className="flex items-center gap-2 text-red-600 hover:text-red-700"
          >
            <XIcon className="h-4 w-4" />
            Eliminar
          </Button>
        </div>
      </div>
    );
  };

  return (
    <Table 
      columns={columns}
      data={sensors}
      headerClassName="bg-gray-50"
      emptyMessage="No hay sensores configurados"
      mobileCardRender={mobileCardRender}
    />
  );
};

export default SensorsTable;