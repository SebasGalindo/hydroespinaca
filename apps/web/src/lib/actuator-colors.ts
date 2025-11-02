/**
 * Utility for generating dynamic colors for actuators
 */

// Extended color palette for actuators
const COLOR_PALETTE = [
  '#3b82f6', // blue
  '#ef4444', // red
  '#10b981', // green
  '#f59e0b', // amber
  '#06b6d4', // cyan
  '#8b5cf6', // violet
  '#ec4899', // pink
  '#14b8a6', // teal
  '#f97316', // orange
  '#6366f1', // indigo
  '#84cc16', // lime
  '#f43f5e', // rose
  '#0ea5e9', // sky
  '#a855f7', // purple
  '#22c55e', // green-500
  '#eab308', // yellow
];

/**
 * Generates a deterministic color for an actuator based on its ID
 * @param actuatorId - The unique identifier of the actuator
 * @returns A hex color string
 */
export function getActuatorColor(actuatorId: string): string {
  // Simple hash function to convert string to number
  let hash = 0;
  for (let i = 0; i < actuatorId.length; i++) {
    const char = actuatorId.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash = hash & hash; // Convert to 32-bit integer
  }

  // Use absolute value and modulo to get index
  const index = Math.abs(hash) % COLOR_PALETTE.length;
  return COLOR_PALETTE[index];
}

/**
 * Generates colors for multiple actuators
 * @param actuatorIds - Array of actuator IDs
 * @returns Object mapping actuator IDs to colors
 */
export function getActuatorColors(actuatorIds: string[]): Record<string, string> {
  const colors: Record<string, string> = {};
  actuatorIds.forEach((id) => {
    colors[id] = getActuatorColor(id);
  });
  return colors;
}

/**
 * Formats a numeric value for display in tooltips
 * @param value - The numeric value to format
 * @param decimals - Number of decimal places (default: 2)
 * @returns Formatted string
 */
export function formatTooltipValue(value: number, decimals: number = 2): string {
  return value.toFixed(decimals);
}
