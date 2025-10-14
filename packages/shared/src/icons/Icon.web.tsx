import React from 'react';
import { IconName } from './types';
import { svgPaths } from './svg-paths';

interface WebIconProps {
  name: IconName;
  size?: number;
  color?: string;
  stroke?: string | undefined;
  strokeWidth?: number;
  fill?: string | undefined;
  className?: string;
  testID?: string;
}

/**
 * Web-only Icon component using standard SVG
 * No React Native dependencies
 */
export const Icon: React.FC<WebIconProps> = ({
  name,
  size = 24,
  color = 'currentColor',
  stroke,
  strokeWidth = 2,
  fill = 'none',
  className,
  testID,
  ...props
}) => {
  const pathData = svgPaths[name];

  if (!pathData) {
    console.warn(`Icon "${name}" not found`);
    return null;
  }

  // Web: Use standard SVG
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill={fill}
      stroke={stroke || color}
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      data-testid={testID}
      {...props}
    >
      <path d={pathData} />
    </svg>
  );
};

export default Icon;
