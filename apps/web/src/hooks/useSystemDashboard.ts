'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import {
  systemStatusService,
  useAuthStore,
} from '@hydroespinaca/shared';
import type { SystemStatusResponse, ReadingItem, WeatherSummary } from '@hydroespinaca/shared';
import { useRouter } from 'next/navigation';

// ─── Polling constants ──────────────────────────────────────
const READING_INTERVAL_MS = 2 * 60 * 1000;   // 2 min from READING
const BUFFER_MS = 20 * 1000;                  // +20 s margin
const FOLLOW_UP_INTERVAL_MS = 30 * 1000;      // 30 s base follow-up
const MAX_FOLLOW_UP_MS = 90 * 1000;           // 90 s max follow-up
const MAX_CONSECUTIVE_EMPTY = 3;
const MAX_CONSECUTIVE_UNCHANGED = 3;
const MAX_BACKOFF_MS = 2 * 60 * 1000;         // 2 min max backoff

// ─── Pure helpers (module-level) ────────────────────────────

export function formatColombiaDateTime(isoString: string): string {
  const date = new Date(isoString);
  const options: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
    timeZone: 'America/Bogota',
  };
  return new Intl.DateTimeFormat('es-CO', options).format(date);
}

import { IconType } from '@hydroespinaca/shared/types/common';

const ICON_TYPE_MAP: Array<{ match: (name: string) => boolean; icon: IconType }> = [
  { match: (n) => n.includes('temperatura') && n.includes('agua'), icon: 'water' },
  { match: (n) => n.includes('temperatura'), icon: 'temperature' },
  { match: (n) => n.includes('humedad'), icon: 'humidity' },
  { match: (n) => n.includes('luz') || n.includes('luminosidad'), icon: 'sun' },
  { match: (n) => n.includes('conductividad') || n.includes('ec'), icon: 'electric' },
  { match: (n) => n.includes('nivel'), icon: 'ruler' },
  { match: (n) => n.includes('ph'), icon: 'ph' },
];

export function getIconType(name: string): IconType {
  const lower = name.toLowerCase();
  return ICON_TYPE_MAP.find(({ match }) => match(lower))?.icon ?? 'temperature';
}

// ─── Hook ────────────────────────────────────────────────────

export interface SystemDashboardState {
  systemStatus: SystemStatusResponse | null;
  previousReadings: ReadingItem[];
  lastUpdateTimestamp: string | null;
  isLoading: boolean;
  error: string | null;
  isPollingPaused: boolean;
  hasReadingsData: boolean;
  weather: WeatherSummary | null;
  weatherLoading: boolean;
  weatherError: string | null;
  handleRetryPolling: () => void;
  isArtificialLightActive: boolean;
  getPreviousValue: (variableName: string) => number | null;
}

export function useSystemDashboard(): SystemDashboardState {
  const router = useRouter();
  const routerRef = useRef(router);
  const { logout } = useAuthStore();

  const [systemStatus, setSystemStatus] = useState<SystemStatusResponse | null>(null);
  const [previousReadings, setPreviousReadings] = useState<ReadingItem[]>([]);
  const [lastUpdateTimestamp, setLastUpdateTimestamp] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isPollingPaused, setIsPollingPaused] = useState(false);
  const [hasReadingsData, setHasReadingsData] = useState(true);
  const [shouldStartPolling, setShouldStartPolling] = useState(false);

  const [weather, setWeather] = useState<WeatherSummary | null>(null);
  const [weatherLoading] = useState(false);
  const [weatherError, setWeatherError] = useState<string | null>(null);

  // Polling refs
  const lastTimestampRef = useRef<string | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const followUpStartTimeRef = useRef<number | null>(null);
  const consecutiveEmptyResponsesRef = useRef<number>(0);
  const consecutiveUnchangedRef = useRef<number>(0);

  useEffect(() => {
    routerRef.current = router;
  }, [router]);

  const fetchSystemStatus = useCallback(async (): Promise<string | null> => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await systemStatusService.getSystemStatus();

      const hasValidReadings =
        data.readings.readings &&
        data.readings.readings.length > 0 &&
        data.readings.timestamp;

      if (!hasValidReadings) {
        setHasReadingsData(false);
        setSystemStatus(data);
        if (data.weather) {
          setWeather(data.weather);
          setWeatherError(null);
        }
        return null;
      }

      setHasReadingsData(true);
      if (systemStatus?.readings.readings) {
        setPreviousReadings(systemStatus.readings.readings);
      }

      setSystemStatus(data);
      setLastUpdateTimestamp(data.readings.timestamp);
      if (data.weather) {
        setWeather(data.weather);
        setWeatherError(null);
      }

      return data.readings.timestamp;
    } catch (err: any) {
      console.error('Error fetching system status:', err);
      setError(err.message || 'Error al cargar los datos del sistema');
      if (err.response?.status === 401 || err.message?.includes('Unauthorized')) {
        await logout();
        routerRef.current.push('/login');
      }
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [systemStatus, logout]);

  // Initial fetch
  useEffect(() => {
    fetchSystemStatus().then(() => setShouldStartPolling(true));
  }, []);

  const handleRetryPolling = useCallback(() => {
    consecutiveEmptyResponsesRef.current = 0;
    consecutiveUnchangedRef.current = 0;
    followUpStartTimeRef.current = null;
    setIsPollingPaused(false);
    setError(null);
    setHasReadingsData(true);
    fetchSystemStatus().then(() => setShouldStartPolling(true));
  }, [fetchSystemStatus]);

  // Adaptive polling effect
  useEffect(() => {
    if (!shouldStartPolling) return;

    const calculateNextFetchDelay = (readingTimestamp: string): number => {
      const readingTime = new Date(readingTimestamp).getTime();
      const nextReadingTime = readingTime + READING_INTERVAL_MS;
      const now = Date.now();
      const timeUntilNext = nextReadingTime - now;
      return timeUntilNext <= 0 ? 0 : timeUntilNext + BUFFER_MS;
    };

    const calculateBackoffDelay = (retryCount: number): number => {
      const exponential = FOLLOW_UP_INTERVAL_MS * Math.pow(2, Math.min(retryCount, 5));
      return Math.min(exponential, MAX_BACKOFF_MS);
    };

    const scheduleNext = (timestamp: string | null, isFollowUp = false) => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);

      if (consecutiveEmptyResponsesRef.current >= MAX_CONSECUTIVE_EMPTY) {
        setError('Sistema desconectado: No se detectan lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      if (consecutiveUnchangedRef.current >= MAX_CONSECUTIVE_UNCHANGED) {
        setError('No se detectan nuevas lecturas de sensores. Las peticiones automáticas se han detenido.');
        setIsPollingPaused(true);
        return;
      }

      const delay = isFollowUp
        ? calculateBackoffDelay(consecutiveUnchangedRef.current)
        : timestamp
          ? calculateNextFetchDelay(timestamp)
          : FOLLOW_UP_INTERVAL_MS;

      timeoutRef.current = setTimeout(async () => {
        const oldTimestamp = lastTimestampRef.current;
        const newTimestamp = await fetchSystemStatus();

        if (!newTimestamp) {
          consecutiveEmptyResponsesRef.current++;
          consecutiveUnchangedRef.current++;
          if (consecutiveEmptyResponsesRef.current < MAX_CONSECUTIVE_EMPTY) {
            scheduleNext(oldTimestamp || lastUpdateTimestamp, true);
          }
          return;
        }

        consecutiveEmptyResponsesRef.current = 0;
        const changed = oldTimestamp !== newTimestamp;

        if (changed) {
          consecutiveUnchangedRef.current = 0;
          followUpStartTimeRef.current = null;
          lastTimestampRef.current = newTimestamp;
          scheduleNext(newTimestamp, false);
        } else {
          consecutiveUnchangedRef.current++;
          if (!followUpStartTimeRef.current) followUpStartTimeRef.current = Date.now();
          const elapsed = Date.now() - followUpStartTimeRef.current;

          if (elapsed < MAX_FOLLOW_UP_MS && consecutiveUnchangedRef.current < MAX_CONSECUTIVE_UNCHANGED) {
            scheduleNext(newTimestamp, true);
          } else if (consecutiveUnchangedRef.current < MAX_CONSECUTIVE_UNCHANGED) {
            followUpStartTimeRef.current = null;
            scheduleNext(newTimestamp, false);
          }
        }
      }, delay);
    };

    const currentTimestamp = lastUpdateTimestamp || lastTimestampRef.current;
    lastTimestampRef.current = currentTimestamp;
    scheduleNext(currentTimestamp, false);

    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, [shouldStartPolling, lastUpdateTimestamp, fetchSystemStatus]);

  // Derived values
  const isArtificialLightActive = (() => {
    if (!systemStatus?.jobStatus.queue) return false;
    return systemStatus.jobStatus.queue.some(
      (job) =>
        job.commandId.toLowerCase().includes('luz') ||
        job.commandId.toLowerCase().includes('light') ||
        job.commandId.toLowerCase().includes('amplio-espectro'),
    );
  })();

  const getPreviousValue = useCallback(
    (variableName: string): number | null => {
      const prev = previousReadings.find(
        (r) => r.name.toLowerCase() === variableName.toLowerCase(),
      );
      return prev?.value || null;
    },
    [previousReadings],
  );

  return {
    systemStatus,
    previousReadings,
    lastUpdateTimestamp,
    isLoading,
    error,
    isPollingPaused,
    hasReadingsData,
    weather,
    weatherLoading,
    weatherError,
    handleRetryPolling,
    isArtificialLightActive,
    getPreviousValue,
  };
}
