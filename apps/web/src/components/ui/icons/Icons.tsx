import React from 'react';

interface IconProps {
  className?: string;
  size?: number;
  color?: string;
}

// ── Factory helpers ──────────────────────────────────────────────

/** Creates a stroke-based icon (fill="none", stroke={color}). */
function createStrokeIcon(children: React.ReactNode, displayName: string) {
  const Icon: React.FC<IconProps> = ({ className = '', size = 24, color = 'currentColor' }) => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color}
      className={className}
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {children}
    </svg>
  );
  Icon.displayName = displayName;
  return Icon;
}

/** Creates a fill-based icon (fill={color}). */
function createFillIcon(children: React.ReactNode, displayName: string, viewBox = '0 0 20 20') {
  const Icon: React.FC<IconProps> = ({ className = '', size = 24, color = 'currentColor' }) => (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox={viewBox}
      fill={color}
      className={className}
    >
      {children}
    </svg>
  );
  Icon.displayName = displayName;
  return Icon;
}

// ── Stroke icons (64) ────────────────────────────────────────────

export const UserIcon = createStrokeIcon(
  <><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></>,
  'UserIcon',
);

export const ShieldIcon = createStrokeIcon(
  <><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /></>,
  'ShieldIcon',
);

export const SproutIcon = createStrokeIcon(
  <>
    <path d="M7 20a5 5 0 0 1 10 0" />
    <path d="M12 20V10" />
    <path d="M9 10c0-3.3 2.7-6 6-6 0 3.3-2.7 6-6 6z" />
    <path d="M15 10c0-3.3-2.7-6-6-6 0 3.3 2.7 6 6 6z" />
  </>,
  'SproutIcon',
);

export const CheckIcon = createStrokeIcon(
  <><path d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></>,
  'CheckIcon',
);

export const LightningIcon = createStrokeIcon(
  <><path d="M13 10V3L4 14h7v7l9-11h-7z" /></>,
  'LightningIcon',
);

export const ChartIcon = createStrokeIcon(
  <><path d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" /></>,
  'ChartIcon',
);

export const TemperatureIcon = createStrokeIcon(
  <>
    <path d="M14 4v10.54a4 4 0 1 1-4 0V4a2 2 0 0 1 4 0Z" />
    <path d="M8 12h8" />
    <path d="M8 8h8" />
  </>,
  'TemperatureIcon',
);

export const HumidityIcon = createStrokeIcon(
  <><path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z" /></>,
  'HumidityIcon',
);

export const PhIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="10" />
    <path d="M8 12h8" />
    <path d="M12 8v8" />
    <path d="M16 8l-8 8" />
  </>,
  'PhIcon',
);

export const SunIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="4" />
    <path d="M12 2v2" />
    <path d="M12 20v2" />
    <path d="M4.93 4.93l1.41 1.41" />
    <path d="M17.66 17.66l1.41 1.41" />
    <path d="M2 12h2" />
    <path d="M20 12h2" />
    <path d="M6.34 17.66l-1.41 1.41" />
    <path d="M19.07 4.93l-1.41 1.41" />
  </>,
  'SunIcon',
);

export const ElectricIcon = createStrokeIcon(
  <><path d="M13 10V3L4 14h7v7l9-11h-7z" /></>,
  'ElectricIcon',
);

export const RulerIcon = createStrokeIcon(
  <>
    <path d="M21.3 8.7l-9.6 9.6c-.4.4-1 .4-1.4 0l-2.6-2.6c-.4-.4-.4-1 0-1.4l9.6-9.6" />
    <path d="M7 17l5-5" />
    <path d="M12 12l5-5" />
    <path d="M17 7l-1.4-1.4" />
  </>,
  'RulerIcon',
);

export const WaterIcon = createStrokeIcon(
  <>
    <path d="M7 16.3c2.2 0 4-1.83 4-4.05 0-1.16-.57-2.26-1.71-3.19S7.29 6.75 7 5.3c-.29 1.45-1.14 2.84-2.29 3.76S3 11.1 3 12.25c0 2.22 1.8 4.05 4 4.05z" />
    <path d="M12.56 6.6A10.97 10.97 0 0 0 14 3.02c.5 2.5 2.26 4.89 4.56 6.68a7.58 7.58 0 0 1 2.79 5.98c0 2.9-2.18 5.32-5 5.32s-5-2.42-5-5.32c0-.28.02-.56.05-.83" />
  </>,
  'WaterIcon',
);

export const SensorIcon = createStrokeIcon(
  <>
    <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
    <line x1="8" y1="21" x2="16" y2="21" />
    <line x1="12" y1="17" x2="12" y2="21" />
    <circle cx="7" cy="9" r="2" />
    <circle cx="17" cy="9" r="2" />
    <path d="M12 7v4" />
  </>,
  'SensorIcon',
);

export const BrainIcon = createStrokeIcon(
  <>
    <path d="M9.5 2A2.5 2.5 0 0 1 12 4.5v15a2.5 2.5 0 0 1-4.96.44 2.5 2.5 0 0 1-2.96-3.08 3 3 0 0 1-.34-5.58 2.5 2.5 0 0 1 1.32-4.24 2.5 2.5 0 0 1 1.98-3A2.5 2.5 0 0 1 9.5 2Z" />
    <path d="M14.5 2A2.5 2.5 0 0 0 12 4.5v15a2.5 2.5 0 0 0 4.96.44 2.5 2.5 0 0 0 2.96-3.08 3 3 0 0 0 .34-5.58 2.5 2.5 0 0 0-1.32-4.24 2.5 2.5 0 0 0-1.98-3A2.5 2.5 0 0 0 14.5 2Z" />
  </>,
  'BrainIcon',
);

export const PlusIcon = createStrokeIcon(
  <>
    <path d="M12 5v14" />
    <path d="M5 12h14" />
  </>,
  'PlusIcon',
);

export const SettingsIcon = createStrokeIcon(
  <>
    <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.38a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
    <circle cx="12" cy="12" r="3" />
  </>,
  'SettingsIcon',
);

export const HistoryIcon = createStrokeIcon(
  <>
    <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
    <path d="M3 3v5h5" />
    <path d="M12 7v5l4 2" />
  </>,
  'HistoryIcon',
);

export const BookIcon = createStrokeIcon(
  <>
    <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" />
    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" />
  </>,
  'BookIcon',
);

export const LinkIcon = createStrokeIcon(
  <>
    <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
    <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
  </>,
  'LinkIcon',
);

export const PlantIcon = createStrokeIcon(
  <>
    <path d="M7 20h10" />
    <path d="M10 20c5.5-2.5.8-6.4 3-10" />
    <path d="M9.5 9.4c1.1.8 1.8 2.2 2.3 3.7-2 .4-3.5.4-4.8-.3-1.2-.6-2.3-1.9-3-4.2 2.8-.5 4.4 0 5.5.8z" />
    <path d="M14.1 6a7 7 0 0 0-1.1 4c1.9-.1 3.3-.7 4.3-1.4 1-1.1 1.6-2.7 1.7-4.6-2.7.1-4 1-4.9 2z" />
  </>,
  'PlantIcon',
);

export const TrendingUpIcon = createStrokeIcon(
  <>
    <polyline points="23 6 13.5 15.5 8.5 10.5 1 18" />
    <polyline points="17 6 23 6 23 12" />
  </>,
  'TrendingUpIcon',
);

export const AlertTriangleIcon = createStrokeIcon(
  <>
    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
    <line x1="12" y1="9" x2="12" y2="13" />
    <line x1="12" y1="17" x2="12.01" y2="17" />
  </>,
  'AlertTriangleIcon',
);

export const XIcon = createStrokeIcon(
  <>
    <line x1="18" y1="6" x2="6" y2="18" />
    <line x1="6" y1="6" x2="18" y2="18" />
  </>,
  'XIcon',
);

export const BellIcon = createStrokeIcon(
  <>
    <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
    <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
  </>,
  'BellIcon',
);

export const LightBulbIcon = createStrokeIcon(
  <>
    <path d="M9 21h6" />
    <path d="M12 3a6 6 0 0 1 6 6c0 3-2 5.5-2 8h-8c0-2.5-2-5-2-8a6 6 0 0 1 6-6z" />
  </>,
  'LightBulbIcon',
);

export const EditIcon = createStrokeIcon(
  <>
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
  </>,
  'EditIcon',
);

export const FilterIcon = createStrokeIcon(
  <><polygon points="22,3 2,3 10,12.46 10,19 14,21 14,12.46" /></>,
  'FilterIcon',
);

export const CalendarIcon = createStrokeIcon(
  <>
    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
  </>,
  'CalendarIcon',
);

export const ClockIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="10" />
    <polyline points="12 6 12 12 16 14" />
  </>,
  'ClockIcon',
);

export const SaveIcon = createStrokeIcon(
  <>
    <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" />
    <polyline points="17,21 17,13 7,13 7,21" />
    <polyline points="7,3 7,8 15,8" />
  </>,
  'SaveIcon',
);

export const RefreshIcon = createStrokeIcon(
  <>
    <polyline points="23,4 23,10 17,10" />
    <polyline points="1,20 1,14 7,14" />
    <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15" />
  </>,
  'RefreshIcon',
);

export const DatabaseIcon = createStrokeIcon(
  <>
    <ellipse cx="12" cy="5" rx="9" ry="3" />
    <path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5" />
    <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3" />
  </>,
  'DatabaseIcon',
);

export const CogIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="3" />
    <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
  </>,
  'CogIcon',
);

export const CalculatorIcon = createStrokeIcon(
  <>
    <rect x="4" y="2" width="16" height="20" rx="2" />
    <line x1="8" y1="6" x2="16" y2="6" />
    <line x1="16" y1="14" x2="16" y2="18" />
    <path d="M16 10h.01" />
    <path d="M12 10h.01" />
    <path d="M8 10h.01" />
    <path d="M12 14h.01" />
    <path d="M8 14h.01" />
    <path d="M12 18h.01" />
    <path d="M8 18h.01" />
  </>,
  'CalculatorIcon',
);

export const ChartBarIcon = createStrokeIcon(
  <>
    <line x1="12" y1="20" x2="12" y2="10" />
    <line x1="18" y1="20" x2="18" y2="4" />
    <line x1="6" y1="20" x2="6" y2="16" />
  </>,
  'ChartBarIcon',
);

export const FanIcon = createStrokeIcon(
  <>
    <path d="M10.827 16.379a6.082 6.082 0 0 1-8.618-7.002l5.412 1.45a6.082 6.082 0 0 1 7.002-8.618l-1.45 5.412a6.082 6.082 0 0 1 8.618 7.002l-5.412-1.45a6.082 6.082 0 0 1-7.002 8.618l1.45-5.412Z" />
    <path d="M12 12v.01" />
  </>,
  'FanIcon',
);

export const MotorIcon = createStrokeIcon(
  <>
    <rect x="2" y="6" width="20" height="8" rx="1" />
    <path d="M17 14v7" />
    <path d="M7 14v7" />
    <path d="M17 3v3" />
    <path d="M7 3v3" />
    <path d="M10 14l4-4" />
    <path d="M10 6l4 4" />
  </>,
  'MotorIcon',
);

export const BoltIcon = createStrokeIcon(
  <><path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z" /></>,
  'BoltIcon',
);

export const ThermometerIcon = createStrokeIcon(
  <><path d="M14 4v10.54a4 4 0 1 1-4 0V4a2 2 0 0 1 4 0Z" /></>,
  'ThermometerIcon',
);

export const DropletIcon = createStrokeIcon(
  <><path d="M12 22a7 7 0 0 0 7-7c0-2-1-3.9-3-5.5s-3.5-4-4-6.5c-.5 2.5-2 4.9-4 6.5C6 11.1 5 13 5 15a7 7 0 0 0 7 7z" /></>,
  'DropletIcon',
);

export const TrendingDownIcon = createStrokeIcon(
  <>
    <polyline points="22,17 13.5,8.5 8.5,13.5 2,7" />
    <polyline points="16,17 22,17 22,11" />
  </>,
  'TrendingDownIcon',
);

export const MinusIcon = createStrokeIcon(
  <><path d="M5 12h14" /></>,
  'MinusIcon',
);

export const DownloadIcon = createStrokeIcon(
  <>
    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
    <polyline points="7,10 12,15 17,10" />
    <line x1="12" y1="15" x2="12" y2="3" />
  </>,
  'DownloadIcon',
);

export const ShareIcon = createStrokeIcon(
  <>
    <circle cx="18" cy="5" r="3" />
    <circle cx="6" cy="12" r="3" />
    <circle cx="18" cy="19" r="3" />
    <line x1="8.59" y1="13.51" x2="15.42" y2="17.49" />
    <line x1="15.41" y1="6.51" x2="8.59" y2="10.49" />
  </>,
  'ShareIcon',
);

export const CheckCircleIcon = createStrokeIcon(
  <>
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
    <polyline points="22,4 12,14.01 9,11.01" />
  </>,
  'CheckCircleIcon',
);

export const ExclamationTriangleIcon = createStrokeIcon(
  <>
    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
    <line x1="12" y1="9" x2="12" y2="13" />
    <line x1="12" y1="17" x2="12.01" y2="17" />
  </>,
  'ExclamationTriangleIcon',
);

export const XCircleIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="10" />
    <path d="m15 9-6 6" />
    <path d="m9 9 6 6" />
  </>,
  'XCircleIcon',
);

export const BatteryIcon = createStrokeIcon(
  <>
    <rect width="16" height="10" x="2" y="7" rx="2" ry="2" />
    <line x1="22" y1="11" x2="22" y2="13" />
  </>,
  'BatteryIcon',
);

export const CpuChipIcon = createStrokeIcon(
  <>
    <rect width="16" height="16" x="4" y="4" rx="2" />
    <rect width="6" height="6" x="9" y="9" rx="1" />
    <path d="M15 2v2" />
    <path d="M15 20v2" />
    <path d="M2 15h2" />
    <path d="M2 9h2" />
    <path d="M20 15h2" />
    <path d="M20 9h2" />
    <path d="M9 2v2" />
    <path d="M9 20v2" />
  </>,
  'CpuChipIcon',
);

export const InformationCircleIcon = createStrokeIcon(
  <>
    <circle cx="12" cy="12" r="10" />
    <path d="M12 16v-4" />
    <path d="M12 8h.01" />
  </>,
  'InformationCircleIcon',
);

export const XMarkIcon = createStrokeIcon(
  <>
    <path d="M18 6 6 18" />
    <path d="M6 6l12 12" />
  </>,
  'XMarkIcon',
);

export const TrashIcon = createStrokeIcon(
  <>
    <path d="M3 6h18" />
    <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" />
    <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
    <line x1="10" y1="11" x2="10" y2="17" />
    <line x1="14" y1="11" x2="14" y2="17" />
  </>,
  'TrashIcon',
);

export const PowerIcon = createStrokeIcon(
  <>
    <path d="M12 2v10" />
    <path d="M18.4 6.6a9 9 0 1 1-12.77.04" />
  </>,
  'PowerIcon',
);

export const MenuIcon = createStrokeIcon(
  <>
    <line x1="4" y1="6" x2="20" y2="6" />
    <line x1="4" y1="12" x2="20" y2="12" />
    <line x1="4" y1="18" x2="20" y2="18" />
  </>,
  'MenuIcon',
);

export const ChevronDownIcon = createStrokeIcon(
  <><polyline points="6,9 12,15 18,9" /></>,
  'ChevronDownIcon',
);

export const ChevronRightIcon = createStrokeIcon(
  <><polyline points="9,18 15,12 9,6" /></>,
  'ChevronRightIcon',
);

export const ListIcon = createStrokeIcon(
  <>
    <line x1="8" y1="6" x2="21" y2="6" />
    <line x1="8" y1="12" x2="21" y2="12" />
    <line x1="8" y1="18" x2="21" y2="18" />
    <line x1="3" y1="6" x2="3.01" y2="6" />
    <line x1="3" y1="12" x2="3.01" y2="12" />
    <line x1="3" y1="18" x2="3.01" y2="18" />
  </>,
  'ListIcon',
);

export const ExpandIcon = createStrokeIcon(
  <>
    <polyline points="15,3 21,3 21,9" />
    <polyline points="9,21 3,21 3,15" />
    <line x1="21" y1="3" x2="14" y2="10" />
    <line x1="3" y1="21" x2="10" y2="14" />
  </>,
  'ExpandIcon',
);

export const CollapseIcon = createStrokeIcon(
  <>
    <polyline points="4,14 10,14 10,20" />
    <polyline points="20,10 14,10 14,4" />
    <line x1="14" y1="10" x2="21" y2="3" />
    <line x1="3" y1="21" x2="10" y2="14" />
  </>,
  'CollapseIcon',
);

export const WifiIcon = createStrokeIcon(
  <>
    <path d="M5 12.55a11 11 0 0 1 14.08 0" />
    <path d="M1.42 9a16 16 0 0 1 21.16 0" />
    <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
    <line x1="12" y1="20" x2="12.01" y2="20" />
  </>,
  'WifiIcon',
);

export const EyeIcon = createStrokeIcon(
  <>
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
    <circle cx="12" cy="12" r="3" />
  </>,
  'EyeIcon',
);

export const EyeOffIcon = createStrokeIcon(
  <>
    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
    <line x1="1" y1="1" x2="23" y2="23" />
  </>,
  'EyeOffIcon',
);

export const CloudSunIcon = createStrokeIcon(
  <>
    <path d="M12 2v2" />
    <path d="m4.93 4.93 1.41 1.41" />
    <path d="M20 12h2" />
    <path d="m19.07 4.93-1.41 1.41" />
    <path d="M15.947 12.65a4 4 0 0 0-5.925-4.128" />
    <path d="M13 22H7a5 5 0 1 1 4.9-6H13a3 3 0 0 1 0 6Z" />
  </>,
  'CloudSunIcon',
);

// ── Fill icons (2) ───────────────────────────────────────────────

export const EmailIcon = createFillIcon(
  <>
    <path d="M2.003 5.884L10 9.882l7.997-3.998A2 2 0 0016 4H4a2 2 0 00-1.997 1.884z" />
    <path d="M18 8.118l-8 4-8-4V14a2 2 0 002 2h12a2 2 0 002-2V8.118z" />
  </>,
  'EmailIcon',
);

export const LockIcon = createFillIcon(
  <><path fillRule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clipRule="evenodd" /></>,
  'LockIcon',
);
