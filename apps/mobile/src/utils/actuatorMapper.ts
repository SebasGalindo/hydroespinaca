/**
 * Actuator analytics mapper for mobile.
 * Converts the backend ActuatorAnalyticsResponse into a flat list of ActuatorActivity.
 */
import type {
  ActuatorAnalyticsResponse,
  ActuatorTimelineItem,
} from '@hydroespinaca/shared';
import { chartColors, colors } from '@hydroespinaca/shared';

export interface ActuatorActivation {
  startTime: string;
  endTime: string;
  duration: number; // minutes
}

export interface ActuatorActivity {
  actuatorId: string;
  actuatorName: string;
  activations: ActuatorActivation[];
  totalDuration: number; // minutes
  activationCount: number;
  proportion: number; // percentage 0-100
}

function formatActuatorName(code: string): string {
  return code
    .split('-')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}

function groupTimelineByActuator(
  timeline: ActuatorTimelineItem[],
): Map<string, ActuatorTimelineItem[]> {
  const grouped = new Map<string, ActuatorTimelineItem[]>();
  timeline.forEach((item) => {
    const existing = grouped.get(item.actuatorCode) || [];
    existing.push(item);
    grouped.set(item.actuatorCode, existing);
  });
  return grouped;
}

function timelineToActivations(items: ActuatorTimelineItem[]): ActuatorActivation[] {
  return items
    .filter((item) => item.totalDurationSeconds > 0)
    .map((item) => {
      const start = new Date(item.timestamp);
      const durationMin = item.totalDurationSeconds / 60;
      const end = new Date(start.getTime() + item.totalDurationSeconds * 1000);
      return {
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        duration: durationMin,
      };
    });
}

export function mapActuatorAnalytics(
  response: ActuatorAnalyticsResponse,
): ActuatorActivity[] {
  const timelineByActuator = groupTimelineByActuator(response.timeline);
  const durationMap = new Map(
    response.totalDurationByActuator.map((item) => [
      item.actuatorCode,
      {
        totalDurationSeconds: item.totalDurationSeconds,
        activationCount: item.activationCount,
      },
    ]),
  );
  const proportionMap = new Map(
    response.activeTimeProportion.map((item) => [
      item.actuatorCode,
      item.percentage,
    ]),
  );

  const allCodes = new Set([
    ...timelineByActuator.keys(),
    ...response.totalDurationByActuator.map((item) => item.actuatorCode),
  ]);

  const activities: ActuatorActivity[] = [];

  allCodes.forEach((code) => {
    const timelineItems = timelineByActuator.get(code) || [];
    const durationData = durationMap.get(code);
    if (!durationData) return;

    activities.push({
      actuatorId: code,
      actuatorName: formatActuatorName(code),
      activations: timelineToActivations(timelineItems),
      totalDuration: durationData.totalDurationSeconds / 60,
      activationCount: durationData.activationCount,
      proportion: proportionMap.get(code) || 0,
    });
  });

  return activities.sort((a, b) => a.actuatorName.localeCompare(b.actuatorName));
}

// ─── Color palette for actuators ─────────────────────────────

const ACTUATOR_COLORS = [
  chartColors.blue, chartColors.red, chartColors.green, chartColors.amber, chartColors.cyan, chartColors.violet,
  chartColors.pink, chartColors.teal, chartColors.orange, chartColors.indigo, chartColors.lime, chartColors.rose,
];

export function getActuatorColor(actuatorId: string): string {
  let hash = 0;
  for (let i = 0; i < actuatorId.length; i++) {
    const char = actuatorId.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash;
  }
  return ACTUATOR_COLORS[Math.abs(hash) % ACTUATOR_COLORS.length]!;
}

// ─── Variable colors ─────────────────────────────────────────

export function getVariableColor(variableName: string): string {
  const name = variableName.toLowerCase();
  if (name.includes('ph')) return chartColors.violet;
  if (name.includes('conductividad') || name.includes('ec')) return chartColors.amber;
  if (name.includes('ambiente')) return chartColors.green;
  if (name.includes('agua')) return chartColors.blue;
  if (name.includes('humedad')) return chartColors.blue;
  if (name.includes('luz') || name.includes('luminosidad')) return chartColors.amber;
  if (name.includes('nivel')) return chartColors.indigo;
  if (name.includes('tds')) return chartColors.green;
  return colors.gray[500];
}

export function getVariableUnit(variableName: string): string {
  const name = variableName.toLowerCase();
  if (name.includes('ph')) return '';
  if (name.includes('conductividad') || name.includes('ec')) return 'mS/cm';
  if (name.includes('temperatura')) return '°C';
  if (name.includes('humedad')) return '%';
  if (name.includes('luz') || name.includes('luminosidad')) return 'lux';
  if (name.includes('nivel') && name.includes('agua')) return 'cm';
  if (name.includes('tds')) return 'ppm';
  return '';
}
