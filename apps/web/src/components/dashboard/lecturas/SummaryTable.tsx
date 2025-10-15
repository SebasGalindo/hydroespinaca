'use client';

import React, { useMemo } from 'react';
import Table from '@/components/ui/Table';
import type { SensorSummary } from '@hydroespinaca/shared';

interface SummaryTableProps {
  sensorSummary: SensorSummary[];
}

// Función auxiliar para renderizar números de forma segura
const renderNumericValue = (value: unknown, unit: string) => {
  const numericValue = parseFloat(value as string);

  if (isNaN(numericValue)) {
    return <span className="font-medium text-gray-500">{value as string}</span>;
  }

  // Puedes ajustar la cantidad de decimales según necesites. 
  // Por ejemplo, .toFixed(1) para un decimal.
  return <span className="font-medium text-green-600">{numericValue.toFixed(1)} {unit}</span>;
};


export default function SummaryTable({ sensorSummary }: SummaryTableProps) {
  const columns = useMemo(() => [
    {
      key: 'sensor',
      label: 'Sensor',
      render: (value: unknown) => (
        <span className="font-semibold text-gray-900">{value as string}</span>
      )
    },
    {
      key: 'media',
      label: 'Media',
      // ⭐ Usamos la función auxiliar para no repetir código
      render: (value: unknown, row: unknown) => {
        const summary = row as SensorSummary;
        return renderNumericValue(value, summary.unidad);
      }
    },
    {
      key: 'minimo',
      label: 'Valor Mínimo',
      render: (value: unknown, row: unknown) => {
        const summary = row as SensorSummary;
        return renderNumericValue(value, summary.unidad);
      }
    },
    {
      key: 'maximo',
      label: 'Valor Máximo',
      render: (value: unknown, row: unknown) => {
        const summary = row as SensorSummary;
        return renderNumericValue(value, summary.unidad);
      }
    },
    {
      key: 'ultimaLectura',
      label: 'Última Lectura',
      render: (value: unknown) => (
        <span className="font-medium text-gray-700">{value as string}</span>
      )
    }
  ], []);

  const mobileCardRender = (item: unknown) => {
    const summaryItem = item as SensorSummary;
    return (
      <div className="bg-gray-50 rounded-lg p-4 shadow-sm border border-gray-200">
        <h3 className="text-lg font-bold text-green-800 mb-2">{summaryItem.sensor}</h3>
        <div className="grid grid-cols-2 gap-2 text-sm">
          {/* Se aplica la misma lógica de parseo aquí para la vista móvil */}
          <div className="text-gray-600">Media:</div>
          <div>{renderNumericValue(summaryItem.media, summaryItem.unidad)}</div>
          
          <div className="text-gray-600">Mínimo:</div>
          <div>{renderNumericValue(summaryItem.minimo, summaryItem.unidad)}</div>

          <div className="text-gray-600">Máximo:</div>
          <div>{renderNumericValue(summaryItem.maximo, summaryItem.unidad)}</div>
          
          <div className="text-gray-600">Última Lectura:</div>
          <div className="font-medium text-gray-900">{summaryItem.ultimaLectura}</div>
        </div>
      </div>
    );
  };

  return (
    <Table 
      columns={columns}
      data={sensorSummary}
      headerClassName="bg-green-100"
      emptyMessage="No hay datos de resumen disponibles"
      mobileCardRender={mobileCardRender}
    />
  );
}