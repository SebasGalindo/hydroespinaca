export interface ReadingItem {
  name: string;
  value: number;
  unit: string;
  optimalMin: number;
  optimalMax: number;
}

export interface ReadingsSnapshot {
  timestamp: string;
  readings: ReadingItem[];
}

export interface QueueItem {
  commandId: string;
  status: string;
}

export interface JobStatus {
  esp32Id: string;
  queue: QueueItem[];
}

export interface Stats {
  activeCount: number;
  pendingCount: number;
  totalLockedPins: number;
  esp32Ids: string[];
}

export interface InternalRoutine {
  name: string;
  description: string;
  interval: string;
  nextExecutionEstimate: string;
  isActive: boolean;
}

export interface WeatherInfo {
  temperature: number;
  feelsLike: number;
  humidity: number;
  conditions: string;
  conditionsDescription: string;
  icon: string;
  tempMin: number;
  tempMax: number;
  uvi: number;
  pop: number;
  windSpeed: number;
  lastUpdated: number;
  cacheExpiresAt: number;
}

export interface SystemStatusResponse {
  readings: ReadingsSnapshot;
  jobStatus: JobStatus;
  stats: Stats;
  internalRoutines: InternalRoutine[];
  weather?: WeatherInfo;
}
