import React from 'react';
import { Svg, Path } from 'react-native-svg';
import { IconName } from './types';
import { svgPaths } from './svg-paths';

interface NativeIconProps {
  name: IconName;
  size?: number;
  color?: string;
  stroke?: string | undefined;
  strokeWidth?: number;
  fill?: string | undefined;
  style?: any;
  testID?: string;
}

/**
 * React Native Icon component using react-native-svg
 * Only for mobile apps
 */
export const Icon: React.FC<NativeIconProps> = ({
  name,
  size = 24,
  color = 'currentColor',
  stroke,
  strokeWidth = 2,
  fill = 'none',
  style,
  testID,
  ...props
}) => {
  const pathData = svgPaths[name];

  if (!pathData) {
    console.warn(`Icon "${name}" not found`);
    return null;
  }

  // React Native: Use react-native-svg
  return (
    <Svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill={fill}
      style={style}
      testID={testID}
      {...props}
    >
      <Path
        d={pathData}
        stroke={stroke || color}
        strokeWidth={strokeWidth}
        strokeLinecap="round"
        strokeLinejoin="round"
        fill={fill}
      />
    </Svg>
  );
};

export default Icon;
