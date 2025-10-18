// Mock data for Analytics Dashboard
// TODO: Replace with real API calls to sensor-service, actuator-service, and fuzzy-service via BFF

export interface EnvironmentalDataPoint {
  timestamp: string;
  temperature: number;
  humidity: number;
  ph: number;
  conductivity: number;
  light: number;
}

export interface ActuatorActivity {
  actuatorId: string;
  actuatorName: string;
  activations: {
    startTime: string;
    endTime: string;
    duration: number; // minutes
  }[];
  totalDuration: number; // minutes
  activationCount: number;
}

// Generate mock environmental data for a date range
export function generateEnvironmentalData(
  startDate: Date,
  endDate: Date,
  intervalMinutes: number = 30
): EnvironmentalDataPoint[] {
  const data: EnvironmentalDataPoint[] = [];
  const current = new Date(startDate);

  while (current <= endDate) {
    // Generate realistic values with some variation
    const hourOfDay = current.getHours();
    const dayProgress = hourOfDay / 24;

    data.push({
      timestamp: current.toISOString(),
      temperature: 20 + Math.sin(dayProgress * Math.PI * 2) * 5 + (Math.random() - 0.5) * 2,
      humidity: 65 + Math.sin(dayProgress * Math.PI * 2) * 10 + (Math.random() - 0.5) * 5,
      ph: 6.0 + (Math.random() - 0.5) * 0.4,
      conductivity: 1.2 + (Math.random() - 0.5) * 0.3,
      light: Math.max(0, 300 + Math.sin(dayProgress * Math.PI * 2) * 400 + (Math.random() - 0.5) * 100),
    });

    current.setMinutes(current.getMinutes() + intervalMinutes);
  }

  return data;
}

// Generate mock actuator activity data
export function generateActuatorData(
  startDate: Date,
  endDate: Date
): ActuatorActivity[] {
  const actuators = [
    { id: 'pump-1', name: 'Bomba de Nutrientes' },
    { id: 'heater-1', name: 'Calefactor' },
    { id: 'fan-1', name: 'Ventilador' },
    { id: 'light-1', name: 'Luz LED' },
    { id: 'cooler-1', name: 'Sistema de Enfriamiento' },
  ];

  return actuators.map(actuator => {
    const activations = [];
    let current = new Date(startDate);
    let totalDuration = 0;

    // Generate random activations
    while (current < endDate) {
      // Random decision to activate (30% chance each hour)
      if (Math.random() > 0.7) {
        const startTime = new Date(current);
        const duration = Math.floor(Math.random() * 120) + 10; // 10-130 minutes
        const endTimeValue = new Date(startTime.getTime() + duration * 60000);

        activations.push({
          startTime: startTime.toISOString(),
          endTime: endTimeValue.toISOString(),
          duration,
        });

        totalDuration += duration;
        current = endTimeValue;
      }

      // Move forward 1-3 hours
      current.setHours(current.getHours() + Math.floor(Math.random() * 3) + 1);
    }

    return {
      actuatorId: actuator.id,
      actuatorName: actuator.name,
      activations,
      totalDuration,
      activationCount: activations.length,
    };
  });
}

// Calculate basic statistics for boxplot
export function calculateDailyStatistics(data: EnvironmentalDataPoint[]) {
  const dailyData: { [date: string]: { [variable: string]: number[] } } = {};

  data.forEach(point => {
    const date = point.timestamp.split('T')[0];
    if (!date) return;

    if (!dailyData[date]) {
      dailyData[date] = {
        temperature: [],
        humidity: [],
        ph: [],
        conductivity: [],
        light: [],
      };
    }

    const dayData = dailyData[date];
    if (dayData) {
      dayData.temperature?.push(point.temperature);
      dayData.humidity?.push(point.humidity);
      dayData.ph?.push(point.ph);
      dayData.conductivity?.push(point.conductivity);
      dayData.light?.push(point.light);
    }
  });

  // Calculate quartiles for each day
  const result = Object.entries(dailyData).map(([date, variables]) => {
    const stats: any = { date };

    Object.entries(variables).forEach(([varName, values]) => {
      const sorted = [...values].sort((a, b) => a - b);
      const q1 = sorted[Math.floor(sorted.length * 0.25)] || 0;
      const median = sorted[Math.floor(sorted.length * 0.5)] || 0;
      const q3 = sorted[Math.floor(sorted.length * 0.75)] || 0;
      const min = sorted[0] || 0;
      const max = sorted[sorted.length - 1] || 0;

      stats[varName] = {
        min,
        q1,
        median,
        q3,
        max,
        values: sorted,
      };
    });

    return stats;
  });

  return result;
}

// Variable display names
export const variableDisplayNames: { [key: string]: string } = {
  temperature: 'Temperatura (°C)',
  humidity: 'Humedad (%)',
  ph: 'pH',
  conductivity: 'Conductividad (mS/cm)',
  light: 'Luz (lux)',
};

// Actuator display names
export const actuatorDisplayNames: { [key: string]: string } = {
  'pump-1': 'Bomba de Nutrientes',
  'heater-1': 'Calefactor',
  'fan-1': 'Ventilador',
  'light-1': 'Luz LED',
  'cooler-1': 'Sistema de Enfriamiento',
};
