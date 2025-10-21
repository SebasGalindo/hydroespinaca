import React from 'react';
import { Icon as SharedIcon } from '@hydroespinaca/shared/src/icons/Icon';
import { IconName } from '@hydroespinaca/shared';

export interface IconProps {
  name: IconName;
  size?: number;
  color?: string;
  stroke?: string;
  strokeWidth?: number;
  fill?: string;
  style?: any;
  testID?: string;
}

export function Icon({
  name,
  size = 24,
  color,
  stroke,
  strokeWidth = 2,
  fill = 'none',
  style,
  testID,
}: IconProps): React.ReactElement {
  return (
    <SharedIcon
      name={name}
      size={size}
      color={color || '#000000'}
      stroke={stroke}
      strokeWidth={strokeWidth}
      fill={fill}
      style={style}
      platform="mobile"
      {...(testID && { testID })}
    />
  );
}