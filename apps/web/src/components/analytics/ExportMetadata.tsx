'use client';

import React, { useState, useEffect } from 'react';
import { AnalyticsLevel } from './AnalyticsTabs';

interface ExportMetadataProps {
  level: AnalyticsLevel;
  dateFrom: string;
  dateTo: string;
}

const ExportMetadata = React.memo(function ExportMetadata({ level, dateFrom, dateTo }: ExportMetadataProps) {
  const [exportDate, setExportDate] = useState<string>('');

  // Only set the date on client side to avoid hydration mismatch
  useEffect(() => {
    setExportDate(new Date().toLocaleString('es-MX'));
  }, []);

  const levelNames = {
    environmental: 'Condiciones Ambientales',
    actuators: 'Actividad de Actuadores',
    correlations: 'Correlaciones',
  };

  return (
    <div className="hidden print:block mb-4 p-4 bg-gray-50 border border-gray-200 rounded">
      <h2 className="text-lg font-semibold">HydroEspinaca - Análisis de Datos</h2>
      <p className="text-sm text-gray-600">
        Nivel: {levelNames[level]}
      </p>
      <p className="text-sm text-gray-600">
        Período: {dateFrom} a {dateTo}
      </p>
      {exportDate && (
        <p className="text-sm text-gray-600" suppressHydrationWarning>
          Fecha de exportación: {exportDate}
        </p>
      )}
    </div>
  );
});

export default ExportMetadata;
