'use client';

import React from 'react';
import { EditIcon, XIcon } from '@/components/ui/icons/Icons';
import type { VariableData } from '@hidroespinaca/shared';
import Table from '@/components/ui/Table';
import Badge from '@/components/ui/Badge';
import Button from '@/components/ui/Button';

interface VariablesTableProps {
  variables: VariableData[];
  onEdit: (variableId: string) => void;
  onDelete: (variableId: string) => void;
}

const VariablesTable: React.FC<VariablesTableProps> = ({ variables, onEdit, onDelete }) => {
  const getStatusColor = (status: VariableData['status']) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'inactive':
        return 'default';
      case 'deprecated':
        return 'error';
      default:
        return 'default';
    }
  };

  const getStatusText = (status: VariableData['status']) => {
    switch (status) {
      case 'active':
        return 'Activa';
      case 'inactive':
        return 'Inactiva';
      case 'deprecated':
        return 'Obsoleta';
      default:
        return 'Desconocido';
    }
  };

  const getTypeText = (type: VariableData['type']) => {
    switch (type) {
      case 'input':
        return 'Entrada';
      case 'output':
        return 'Salida';
      case 'calculated':
        return 'Calculada';
      default:
        return 'Desconocido';
    }
  };

  const getCategoryText = (category: VariableData['category']) => {
    switch (category) {
      case 'environmental':
        return 'Ambiental';
      case 'control':
        return 'Control';
      case 'system':
        return 'Sistema';
      case 'user':
        return 'Usuario';
      default:
        return 'Otro';
    }
  };

  const columns = [
    {
      key: 'id',
      label: 'ID',
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
      key: 'description',
      label: 'Descripción',
      render: (value: unknown) => (
        <span className="text-gray-600 text-sm max-w-xs truncate" title={value as string}>
          {value as string}
        </span>
      )
    },
    {
      key: 'type',
      label: 'Tipo',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{getTypeText(value as VariableData['type'])}</span>
      )
    },
    {
      key: 'category',
      label: 'Categoría',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{getCategoryText(value as VariableData['category'])}</span>
      )
    },
    {
      key: 'unit',
      label: 'Unidad',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    },
    {
      key: 'minValue',
      label: 'Valor Mín.',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{(value as number) ?? 'N/A'}</span>
      )
    },
    {
      key: 'maxValue',
      label: 'Valor Máx.',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{(value as number) ?? 'N/A'}</span>
      )
    },

    {
      key: 'status',
      label: 'Estado',
      render: (value: unknown) => (
        <Badge variant={getStatusColor(value as VariableData['status'])}>
          {getStatusText(value as VariableData['status'])}
        </Badge>
      )
    },
    {
      key: 'lastModified',
      label: 'Última Modificación',
      render: (value: unknown) => (
        <span className="text-gray-600 text-sm">{value as string}</span>
      )
    },
    {
      key: 'actions',
      label: 'Acciones',
      render: (_: unknown, row: unknown) => {
        const variable = row as VariableData;
        return (
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onEdit(variable.id)}
              className="p-2 hover:bg-green-50"
            >
              <EditIcon size={16} className="text-green-600" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onDelete(variable.id)}
              className="p-2 hover:bg-red-50"
            >
              <XIcon size={16} className="text-red-600" />
            </Button>
          </div>
        );
      }
    }
  ];

  return (
    <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
      <Table
        data={variables}
        columns={columns}
        className="w-full"
      />
    </div>
  );
};

export default VariablesTable;