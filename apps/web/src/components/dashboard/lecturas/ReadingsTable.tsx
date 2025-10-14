'use client';

import React, { useMemo } from 'react';
import Table from '@/components/ui/Table';
import { IndividualReading } from '@hidroespinaca/shared';

interface ReadingsTableProps {
  individualReadings: IndividualReading[];
}

export default function ReadingsTable({ individualReadings }: ReadingsTableProps) {
  // Envolvemos la definición de columnas en useMemo para optimizar el rendimiento
  const columns = useMemo(() => [
    {
      key: 'fecha',
      label: 'Fecha y Hora',
      // Para las columnas sin render, no hay problema
    },
    {
      key: 'sensor',
      label: 'Sensor',
      // ⭐ SOLUCIÓN: La firma del render ahora acepta 'unknown'
      render: (value: unknown) => (
        // Usamos 'as string' para decirle a TS que sabemos el tipo
        <span className="font-semibold text-gray-900">{value as string}</span>
      )
    },
    {
      key: 'valor',
      label: 'Valor',
      // ⭐ SOLUCIÓN: Aceptamos 'unknown' para ambos parámetros
      render: (value: unknown, row: unknown) => {
        // Hacemos la aserción de tipo para la fila completa
        const reading = row as IndividualReading;
        return (
          // Ahora usamos los tipos correctos
          <span className="font-bold text-green-600">{value as number} {reading.unidad}</span>
        );
      }
    }
  ], []); 

  return (
    <Table 
      columns={columns}
      data={individualReadings}
      headerClassName="bg-green-100"
      emptyMessage="No hay lecturas disponibles"
    />
  );
}