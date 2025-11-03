/**
 * Icon component placeholder
 * This is a basic implementation that should be replaced with actual icon components
 *
 * NOTE: This is a stub component. For React Native apps, you should use the Icon
 * component from apps/mobile/src/components/atoms/Icon.tsx instead.
 */

import React from 'react';
import { IconProps } from './types';

export const Icon: React.FC<IconProps> = ({
  name,
  size = 24,
  color = '#000000',
  testID,
}) => {
  // This is a placeholder stub
  // The actual Icon implementation should be in the mobile app
  if (process.env.NODE_ENV === 'development') {
    console.warn('Using stub Icon component. Please use the mobile app Icon component instead.');
  }

  return React.createElement('span', {
    'data-testid': testID,
    'data-icon': name,
    style: {
      display: 'inline-block',
      width: size,
      height: size,
      backgroundColor: color,
      borderRadius: '50%',
    },
  });
};
