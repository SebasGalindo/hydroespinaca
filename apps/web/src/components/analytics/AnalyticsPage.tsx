'use client';

import React, { useState, useEffect, useRef, useMemo } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import FiltersBar, { FilterState } from './filters/FiltersBar';
import AnalyticsTabs, { AnalyticsLevel } from './AnalyticsTabs';
import EnvironmentalLevel from './levels/EnvironmentalLevel';
import ActuatorsLevel from './levels/ActuatorsLevel';
import CorrelationsLevel from './levels/CorrelationsLevel';
import ExportMetadata from './ExportMetadata';
import {
  generateActuatorData,
  ActuatorActivity,
} from '@/lib/analytics-mocks';
import {
  generateBackendPayload,
  getCacheKey,
  type ViewMode,
} from '@/lib/analytics-filters';
import {
  analyticsService,
  AnalyticsApiError,
  type EnvironmentalVariableAggregate,
} from '@hydroespinaca/shared';

// Cache interface
interface DataCache {
  [key: string]: {
    environmental: EnvironmentalVariableAggregate[];
    actuators: ActuatorActivity[];
    timestamp: number;
  };
}

export default function AnalyticsPage() {
  const [activeTab, setActiveTab] = useState<AnalyticsLevel>('environmental');
  const [isLoading, setIsLoading] = useState(false);

  // Initialize filters with default values
  const getDefaultFilters = (): FilterState => ({
    dateRange: {
      from: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0] || '',
      to: new Date().toISOString().split('T')[0] || '',
    },
    viewMode: 'daily' as ViewMode,
  });

  const [appliedFilters, setAppliedFilters] = useState<FilterState>(getDefaultFilters());
  const [environmentalData, setEnvironmentalData] = useState<EnvironmentalVariableAggregate[]>([]);
  const [actuatorData, setActuatorData] = useState<ActuatorActivity[]>([]);
  const [environmentalError, setEnvironmentalError] = useState<string | null>(null);

  // Cache para evitar recargas innecesarias
  const cacheRef = useRef<DataCache>({});
  const contentRef = useRef<HTMLDivElement>(null);

  // TTL del cache: 5 minutos
  const CACHE_TTL = 5 * 60 * 1000;

  // Load initial data
  useEffect(() => {
    loadData(appliedFilters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const loadData = async (newFilters: FilterState) => {
    setIsLoading(true);
    setEnvironmentalError(null);

    try {
      // Generar clave de cache única para estos filtros
      const cacheKey = getCacheKey({
        startDate: newFilters.dateRange.from,
        endDate: newFilters.dateRange.to,
        view: newFilters.viewMode,
      });

      // Verificar si hay datos en cache y si son válidos
      const cachedData = cacheRef.current[cacheKey];
      const now = Date.now();

      if (cachedData && now - cachedData.timestamp < CACHE_TTL) {
        console.log('[Analytics] Using cached data for', cacheKey);
        setEnvironmentalData(cachedData.environmental);
        setActuatorData(cachedData.actuators);
        setAppliedFilters(newFilters);
        setIsLoading(false);
        return;
      }

      console.log('[Analytics] Fetching fresh data for', cacheKey);

      // Generar payload estandarizado para backend
      const payload = generateBackendPayload({
        startDate: newFilters.dateRange.from,
        endDate: newFilters.dateRange.to,
        view: newFilters.viewMode,
      });

      console.log('[Analytics] Backend payload:', payload);

      // Fetch real environmental data from backend
      let envData: EnvironmentalVariableAggregate[] = [];
      try {
        const envResponse = await analyticsService.getEnvironmentalAggregates({
          startDate: payload.startDate,
          endDate: payload.endDate,
          view: payload.view,
        });
        envData = envResponse.variables;
        console.log('[Analytics] Environmental data loaded:', envData.length, 'variables');
      } catch (error: unknown) {
        console.error('[Analytics] Error loading environmental data:', error);

        if (error instanceof AnalyticsApiError) {
          if (error.status === 400) {
            setEnvironmentalError(`Rango de fechas inválido: ${error.message}`);
          } else if (error.status === 404) {
            setEnvironmentalError('No hay datos disponibles para el rango seleccionado.');
          } else if (error.status === 0) {
            setEnvironmentalError('Error de conexión. Verifica tu conexión a internet.');
          } else {
            setEnvironmentalError(`Error del servidor: ${error.message}`);
          }
        } else {
          setEnvironmentalError('Error desconocido al cargar los datos.');
        }

        // Set empty array on error
        envData = [];
      }

      // Generate mock data for actuators (keep mocks for other levels)
      const startDate = new Date(newFilters.dateRange.from);
      const endDate = new Date(newFilters.dateRange.to);
      const actData = generateActuatorData(startDate, endDate);

      // Guardar en cache solo si hay datos ambientales
      if (envData.length > 0) {
        cacheRef.current[cacheKey] = {
          environmental: envData,
          actuators: actData,
          timestamp: now,
        };
      }

      setEnvironmentalData(envData);
      setActuatorData(actData);
      setAppliedFilters(newFilters);
    } catch (error: unknown) {
      console.error('[Analytics] Unexpected error:', error);
      setEnvironmentalError('Error inesperado al cargar los datos. Por favor, intenta nuevamente.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleApplyFilters = (newFilters: FilterState) => {
    loadData(newFilters);
  };

  const handleExport = async () => {
    try {
      // Dynamic import for better bundle size
      const html2canvas = (await import('html2canvas')).default;
      const { jsPDF } = await import('jspdf');

      if (!contentRef.current) return;

      // Show loading state
      const exportButton = document.querySelector('[data-export-button]') as HTMLButtonElement;
      if (exportButton) {
        exportButton.disabled = true;
        exportButton.textContent = 'Exportando...';
      }

      // Capture the content as canvas
      const canvas = await html2canvas(contentRef.current, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff',
      });

      // Create PDF
      const imgData = canvas.toDataURL('image/png');
      const pdf = new jsPDF({
        orientation: canvas.width > canvas.height ? 'landscape' : 'portrait',
        unit: 'px',
        format: [canvas.width, canvas.height],
      });

      pdf.addImage(imgData, 'PNG', 0, 0, canvas.width, canvas.height);

      // Generate filename
      const levelNames = {
        environmental: 'ambiental',
        actuators: 'actuadores',
        correlations: 'correlaciones',
      };
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `analytics_hydroespinaca_${levelNames[activeTab]}_${timestamp}.pdf`;

      // Download
      pdf.save(filename);

      // Restore button state
      if (exportButton) {
        exportButton.disabled = false;
        exportButton.textContent = 'Exportar';
      }
    } catch (error) {
      console.error('Error exporting:', error);
      alert('Hubo un error al exportar. Por favor, intenta nuevamente.');
    }
  };

  return (
    <PageLayout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Análisis de Datos</h1>
          <p className="text-gray-600 mt-2">
            Monitoreo ambiental, control y correlaciones
          </p>
        </div>

        {/* Filters */}
        <div data-export-button>
          <FiltersBar
            onApplyFilters={handleApplyFilters}
            onExport={handleExport}
            isLoading={isLoading}
            currentFilters={appliedFilters}
          />
        </div>

        {/* Tabs */}
        <AnalyticsTabs activeTab={activeTab} onTabChange={setActiveTab} />

        {/* Content - This will be captured for export */}
        <div ref={contentRef}>
          {/* Export Metadata (visible only in export) */}
          <ExportMetadata
            level={activeTab}
            dateFrom={appliedFilters.dateRange.from}
            dateTo={appliedFilters.dateRange.to}
          />

          {/* Dynamic Content */}
          {activeTab === 'environmental' && (
            <EnvironmentalLevel
              variables={environmentalData}
              viewMode={appliedFilters.viewMode}
              isLoading={isLoading}
              error={environmentalError}
            />
          )}
          {activeTab === 'actuators' && (
            <ActuatorsLevel data={actuatorData} isLoading={isLoading} />
          )}
          {activeTab === 'correlations' && (
            <CorrelationsLevel isLoading={isLoading} />
          )}
        </div>
      </div>
    </PageLayout>
  );
}
