// Mapper to convert backend actuator analytics to frontend format
import type {
  ActuatorAnalyticsResponse,
  ActuatorTimelineItem,
} from '@hydroespinaca/shared';
import type { ActuatorActivity } from './analytics-mocks';

/**
 * Format actuator code for display
 * Capitalizes first letter of each word and replaces hyphens with spaces
 */
function formatActuatorName(code: string): string {
  return code
    .split('-')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
}

/**
 * Group timeline items by actuator code
 */
function groupTimelineByActuator(
  timeline: ActuatorTimelineItem[]
): Map<string, ActuatorTimelineItem[]> {
  const grouped = new Map<string, ActuatorTimelineItem[]>();

  timeline.forEach((item) => {
    const existing = grouped.get(item.actuatorCode) || [];
    existing.push(item);
    grouped.set(item.actuatorCode, existing);
  });

  return grouped;
}

/**
 * Convert timeline items to activation periods
 * Each timeline item represents an aggregated period (hourly, daily, etc.)
 * We create "pseudo-activations" that represent the activity in that period
 */
function timelineToActivations(
  timelineItems: ActuatorTimelineItem[]
): ActuatorActivity['activations'] {
  return timelineItems
    .filter((item) => item.totalDurationSeconds > 0) // Only include periods with activity
    .map((item) => {
      const periodStart = new Date(item.timestamp);
      const durationMinutes = item.totalDurationSeconds / 60;

      // The "activation" spans the duration within the period
      // Note: This is a simplified representation. The actual activations
      // may have been scattered throughout the period.
      const periodEnd = new Date(
        periodStart.getTime() + item.totalDurationSeconds * 1000
      );

      return {
        startTime: periodStart.toISOString(),
        endTime: periodEnd.toISOString(),
        duration: durationMinutes,
      };
    });
}

/**
 * Map backend actuator analytics response to frontend ActuatorActivity format
 *
 * Note: The backend provides aggregated data by time periods (hourly, daily, etc.),
 * not individual activation events. The "activations" array represents aggregated
 * periods where the actuator was active, not discrete on/off cycles.
 */
export function mapActuatorAnalytics(
  response: ActuatorAnalyticsResponse
): ActuatorActivity[] {
  // Group timeline by actuator
  const timelineByActuator = groupTimelineByActuator(response.timeline);

  // Create a map of total durations for quick lookup
  const durationMap = new Map(
    response.totalDurationByActuator.map((item) => [
      item.actuatorCode,
      {
        totalDurationSeconds: item.totalDurationSeconds,
        activationCount: item.activationCount,
      },
    ])
  );

  // Build ActuatorActivity array
  const activities: ActuatorActivity[] = [];

  // Iterate through all actuators that have data
  const allActuatorCodes = new Set([
    ...timelineByActuator.keys(),
    ...response.totalDurationByActuator.map((item) => item.actuatorCode),
  ]);

  allActuatorCodes.forEach((actuatorCode) => {
    const timelineItems = timelineByActuator.get(actuatorCode) || [];
    const durationData = durationMap.get(actuatorCode);

    if (!durationData) {
      // Skip actuators without duration data
      return;
    }

    const activations = timelineToActivations(timelineItems);

    activities.push({
      actuatorId: actuatorCode,
      actuatorName: formatActuatorName(actuatorCode),
      activations,
      totalDuration: durationData.totalDurationSeconds / 60, // Convert to minutes
      activationCount: durationData.activationCount,
    });
  });

  // Sort by actuator name for consistent ordering
  return activities.sort((a, b) => a.actuatorName.localeCompare(b.actuatorName));
}
