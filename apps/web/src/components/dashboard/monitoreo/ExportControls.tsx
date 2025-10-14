'use client';

import React, { useState } from 'react';
import { SensorData } from './DashboardMonitoreo';
import { DownloadIcon, ShareIcon } from '@/components/ui/icons/Icons';

interface ExportControlsProps {
  data: SensorData[];
  timeRange: string;
}

const ExportControls: React.FC<ExportControlsProps> = ({ data, timeRange }) => {
  const [isExporting, setIsExporting] = useState(false);

  const exportToCSV = async () => {
    setIsExporting(true);
    
    try {
      // Crear encabezados CSV
      const headers = ['Fecha y Hora', 'Temperatura (°C)', 'Humedad (%)', 'pH', 'Luz (lux)', 'Conductividad (mS/cm)'];
      
      // Convertir datos a formato CSV
      const csvContent = [
        headers.join(','),
        ...data.map(row => [
          new Date(row.timestamp).toLocaleString('es-ES'),
          row.temperature.toFixed(2),
          row.humidity.toFixed(2),
          row.ph.toFixed(2),
          Math.round(row.light),
          row.conductivity.toFixed(3)
        ].join(','))
      ].join('\n');

      // Crear y descargar archivo
      const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
      const link = document.createElement('a');
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', `datos_sensores_${timeRange}_${new Date().toISOString().split('T')[0]}.csv`);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (error) {
      console.error('Error al exportar datos:', error);
    } finally {
      setIsExporting(false);
    }
  };

  const shareData = async () => {
    if (navigator.share) {
      try {
        await navigator.share({
          title: 'Datos del Dashboard Hidropónico',
          text: `Datos de sensores de los últimos ${timeRange}`,
          url: window.location.href
        });
      } catch (error) {
        console.error('Error al compartir:', error);
      }
    } else {
      // Fallback: copiar URL al portapapeles
      try {
        await navigator.clipboard.writeText(window.location.href);
        alert('URL copiada al portapapeles');
      } catch (error) {
        console.error('Error al copiar URL:', error);
      }
    }
  };

  return (
    <div className="flex items-center gap-2">
      <button
        onClick={exportToCSV}
        disabled={isExporting || data.length === 0}
        className="hidro-button-secondary flex items-center gap-2 text-sm"
        aria-label="Exportar datos a CSV"
      >
        <DownloadIcon className="w-4 h-4" />
        {isExporting ? 'Exportando...' : 'Exportar CSV'}
      </button>
      
      <button
        onClick={shareData}
        className="hidro-button-secondary flex items-center gap-2 text-sm"
        aria-label="Compartir dashboard"
      >
        <ShareIcon className="w-4 h-4" />
        Compartir
      </button>
    </div>
  );
};

export default ExportControls;