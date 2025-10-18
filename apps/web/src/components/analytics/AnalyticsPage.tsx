'use client';

import React, { useState, useEffect, useRef, useMemo } from 'react';
import PageLayout from '@/components/layout/PageLayout';
import FiltersBar, { FilterState } from './filters/FiltersBar';
import AnalyticsTabs, { AnalyticsLevel } from './AnalyticsTabs';
import EnvironmentalLevel from './levels/EnvironmentalLevel';
import ActuatorsLevel from './levels/ActuatorsLevel';
import ExportMetadata from './ExportMetadata';
import { ActuatorActivity } from '@/lib/actuator-analytics-mapper';
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
import { mapActuatorAnalytics } from '@/lib/actuator-analytics-mapper';

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
  const [isExporting, setIsExporting] = useState(false);

  // Initialize filters with default values (today only for hourly view)
  const getDefaultFilters = (): FilterState => {
    const today = new Date().toISOString().split('T')[0] || '';
    return {
      dateRange: {
        from: today, // Same day for hourly (max 1 day)
        to: today,
      },
      viewMode: 'hourly' as ViewMode,
    };
  };

  const [appliedFilters, setAppliedFilters] = useState<FilterState>(getDefaultFilters());
  const [environmentalData, setEnvironmentalData] = useState<EnvironmentalVariableAggregate[]>([]);
  const [actuatorData, setActuatorData] = useState<ActuatorActivity[]>([]);
  const [environmentalError, setEnvironmentalError] = useState<string | null>(null);
  const [actuatorError, setActuatorError] = useState<string | null>(null);

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
    setActuatorError(null);

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

        // Handle session errors (401) - these should be caught by authFetch
        // but we handle them here defensively to prevent UI crashes
        if (error instanceof Error && error.message.includes('Sesión inválida')) {
          setEnvironmentalError('Sesión expirada. Redirigiendo al login...');
          // authFetch already handles logout and redirect, so we just show a message
          envData = [];
        } else if (error instanceof AnalyticsApiError) {
          // Handle specific API errors with user-friendly messages
          if (error.status === 400) {
            setEnvironmentalError(`Rango de fechas inválido: ${error.message}`);
          } else if (error.status === 401) {
            // 401 should be caught by authFetch, but handle defensively
            setEnvironmentalError('Sesión expirada. Redirigiendo al login...');
          } else if (error.status === 404) {
            setEnvironmentalError('No hay datos disponibles para el rango seleccionado.');
          } else if (error.status === 0) {
            setEnvironmentalError('Error de conexión. Verifica tu conexión a internet.');
          } else if (error.status >= 500) {
            setEnvironmentalError(`Error del servidor (${error.status}). Por favor, intenta nuevamente.`);
          } else {
            setEnvironmentalError(`Error al cargar datos: ${error.message}`);
          }
          envData = [];
        } else if (error instanceof Error) {
          // Generic error handling
          setEnvironmentalError(`Error: ${error.message}`);
          envData = [];
        } else {
          // Unknown error type
          setEnvironmentalError('Error desconocido al cargar los datos. Por favor, intenta nuevamente.');
          envData = [];
        }
      }

      // Fetch real actuator data from backend
      let actData: ActuatorActivity[] = [];
      try {
        const actResponse = await analyticsService.getActuatorAnalytics({
          startDate: payload.startDate,
          endDate: payload.endDate,
          view: payload.view,
        });
        actData = mapActuatorAnalytics(actResponse);
        console.log('[Analytics] Actuator data loaded:', actData.length, 'actuators');
      } catch (error: unknown) {
        console.error('[Analytics] Error loading actuator data:', error);

        // Handle session errors (401)
        if (error instanceof Error && error.message.includes('Sesión inválida')) {
          setActuatorError('Sesión expirada. Redirigiendo al login...');
          actData = [];
        } else if (error instanceof AnalyticsApiError) {
          // Handle specific API errors with user-friendly messages
          if (error.status === 400) {
            setActuatorError(`Rango de fechas inválido: ${error.message}`);
          } else if (error.status === 401) {
            setActuatorError('Sesión expirada. Redirigiendo al login...');
          } else if (error.status === 404) {
            setActuatorError('No hay datos de actuadores disponibles para el rango seleccionado.');
          } else if (error.status === 0) {
            setActuatorError('Error de conexión. Verifica tu conexión a internet.');
          } else if (error.status >= 500) {
            setActuatorError(`Error del servidor (${error.status}). Por favor, intenta nuevamente.`);
          } else {
            setActuatorError(`Error al cargar datos de actuadores: ${error.message}`);
          }
          actData = [];
        } else if (error instanceof Error) {
          setActuatorError(`Error: ${error.message}`);
          actData = [];
        } else {
          setActuatorError('Error desconocido al cargar los datos de actuadores.');
          actData = [];
        }
      }

      // Guardar en cache solo si hay datos ambientales o de actuadores
      if (envData.length > 0 || actData.length > 0) {
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
      console.error('[Analytics] Unexpected error in loadData:', error);

      // Handle catastrophic errors gracefully
      if (error instanceof Error && error.message.includes('Sesión inválida')) {
        setEnvironmentalError('Sesión expirada. Redirigiendo al login...');
      } else if (error instanceof Error) {
        setEnvironmentalError(`Error inesperado: ${error.message}. Por favor, intenta nuevamente.`);
      } else {
        setEnvironmentalError('Error inesperado al cargar los datos. Por favor, recarga la página.');
      }

      // Set empty data to prevent crashes
      setEnvironmentalData([]);
      setActuatorData([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleApplyFilters = (newFilters: FilterState) => {
    loadData(newFilters);
  };

  const handleExport = async () => {
    try {
      setIsExporting(true);

      // Dynamic import for better bundle size
      const html2canvas = (await import('html2canvas')).default;
      const { jsPDF } = await import('jspdf');
      const { convertDOMColors } = await import('@/utils/colorConverter');

      if (!contentRef.current) return;

      // Capture the content as canvas
      // Note: We use onclone to convert oklch/oklab colors to rgb before rendering
      // This fixes compatibility issues with html2canvas which doesn't support CSS Color Level 4
      const canvas = await html2canvas(contentRef.current, {
        scale: 2,
        useCORS: true,
        allowTaint: true,
        logging: false,
        backgroundColor: '#ffffff',
        onclone: (clonedDoc) => {
          try {
            // Convert all modern color formats (oklch, oklab) to rgb
            convertDOMColors(clonedDoc);
          } catch (error) {
            console.warn('Error converting colors for export:', error);
            // Fallback: try basic conversion
            const elements = clonedDoc.querySelectorAll('*');
            elements.forEach((el) => {
              if (el instanceof HTMLElement) {
                const computed = window.getComputedStyle(el);
                // Copy computed styles as inline to ensure they're rendered
                if (computed.backgroundColor && computed.backgroundColor !== 'rgba(0, 0, 0, 0)') {
                  el.style.backgroundColor = computed.backgroundColor;
                }
                if (computed.color) {
                  el.style.color = computed.color;
                }
                if (computed.borderColor) {
                  el.style.borderColor = computed.borderColor;
                }
              }
            });
          }
        },
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
      };
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `analytics_hydroespinaca_${levelNames[activeTab]}_${timestamp}.pdf`;

      // Download
      pdf.save(filename);
    } catch (error) {
      console.error('Error exporting:', error);
      alert('Hubo un error al exportar. Por favor, intenta nuevamente.');
    } finally {
      setIsExporting(false);
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
        <FiltersBar
          onApplyFilters={handleApplyFilters}
          onExport={handleExport}
          isLoading={isLoading}
          isExporting={isExporting}
          currentFilters={appliedFilters}
        />

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
            <ActuatorsLevel data={actuatorData} isLoading={isLoading} error={actuatorError} />
          )}
        </div>
      </div>
    </PageLayout>
  );
}
