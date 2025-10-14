'use client';

import React, { useEffect } from 'react';
import { useSensorStore } from '@hidroespinaca/shared';
import MetricsGrid from './MetricsGrid';
import TimeSeriesChart from './charts/TimeSeriesChart';
import HeatmapChart from './charts/HeatmapChart';
import ScatterChart from './charts/ScatterChart';
import CustomChart from './charts/CustomChart';
import TimeRangeSelector from './TimeRangeSelector';
import ExportControls from './ExportControls';

// Re-export types for backward compatibility
export type { SensorData, MetricData } from '@hidroespinaca/shared';

const DashboardMonitoreo: React.FC = () => {
  const {
    sensorData,
    currentMetrics,
    timeRange,
    isRealTime,
    loading,
    setTimeRange,
    setIsRealTime,
    generateMockData,
    addSensorDataPoint
  } = useSensorStore();

  // Inicializar datos al montar el componente
  useEffect(() => {
    generateMockData();
  }, [generateMockData]);

  // Actualización en tiempo real
  useEffect(() => {
    if (!isRealTime) return;

    const interval = setInterval(() => {
      const newDataPoint = {
        timestamp: new Date().toISOString(),
        temperature: 24 + Math.sin(Date.now() / 100000) * 2 + Math.random() * 0.5,
        humidity: 65 + Math.cos(Date.now() / 80000) * 10 + Math.random() * 2,
        ph: 6.5 + Math.sin(Date.now() / 120000) * 0.3 + Math.random() * 0.1,
        light: 800 + Math.sin(Date.now() / 60000) * 200 + Math.random() * 50,
        conductivity: 1.2 + Math.sin(Date.now() / 90000) * 0.2 + Math.random() * 0.05
      };

      addSensorDataPoint(newDataPoint);
    }, 30000); // Actualizar cada 30 segundos

    return () => clearInterval(interval);
  }, [isRealTime, addSensorDataPoint]);

  // currentMetrics is now managed by the store

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-green-600 mx-auto mb-4"></div>
          <p className="text-gray-600 font-inter">Cargando datos del dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <main className="space-y-6" role="main" aria-label="Visualización de datos">
      {/* Header */}
      <div className="text-center">
        <h1 className="text-2xl font-bold text-gray-900 font-inter">Visualización de Datos</h1>
        <p className="text-gray-600 mt-2 font-inter">Explora el entorno del invernadero a través de gráficos y diagramas interactivos. Selecciona variables y aplica filtros para analizar puntos de datos específicos.</p>
      </div>

      {/* Controles superiores */}
      <section className="flex flex-wrap flex-col lg:flex-row justify-between items-start lg:items-center gap-4 bg-white p-4 sm:p-6 rounded-lg shadow-sm border border-gray-200">
        <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center">
          <TimeRangeSelector 
            value={timeRange} 
            onChange={setTimeRange}
          />
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-2 text-xs sm:text-sm font-medium text-gray-700 font-inter">
              <input
                type="checkbox"
                checked={isRealTime}
                onChange={(e) => setIsRealTime(e.target.checked)}
                className="rounded border-gray-300 text-green-600 focus:ring-green-500"
              />
              Tiempo real
            </label>
          </div>
        </div>
        <ExportControls data={sensorData} timeRange={timeRange} />
      </section>

      {/* Gráficos principales */}
      <section className="grid grid-cols-1 xl:grid-cols-2 gap-6" aria-labelledby="charts-heading">
        <h2 id="charts-heading" className="sr-only">Gráficos de variables del sistema</h2>
        
        <article className="hidro-card">
          <header className="mb-4">
            <h3 className="text-lg font-semibold text-gray-900 font-inter">Serie de Tiempo</h3>
          </header>
          <TimeSeriesChart data={sensorData} timeRange={timeRange} />
        </article>

        <article className="hidro-card">
          <header className="mb-4">
            <h3 className="text-lg font-semibold text-gray-900 font-inter">Correlación en Mapa de Calor</h3>
          </header>
          <HeatmapChart data={sensorData} timeRange={timeRange} />
        </article>

        <article className="hidro-card">
          <header className="mb-4">
            <h3 className="text-lg font-semibold text-gray-900 font-inter">Gráfico de Dispersión</h3>
          </header>
          <ScatterChart data={sensorData} timeRange={timeRange} />
        </article>

        <article className="hidro-card">
          <header className="mb-4">
            <h3 className="text-lg font-semibold text-gray-900 font-inter">Otro Gráfico</h3>
          </header>
          <CustomChart data={sensorData} timeRange={timeRange} />
        </article>
      </section>
    </main>
  );
};

export default DashboardMonitoreo;