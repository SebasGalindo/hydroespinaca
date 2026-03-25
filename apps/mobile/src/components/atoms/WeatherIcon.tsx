import React from 'react';
import { Image, View, StyleSheet, ViewStyle } from 'react-native';

export interface WeatherIconProps {
  /** OpenWeather icon code (e.g. "01d", "10n") */
  icon: string;
  size?: number;
  style?: ViewStyle;
  testID?: string;
}

const ICON_BASE_URL = 'https://openweathermap.org/img/wn';

export const WeatherIcon = React.memo(function WeatherIcon({
  icon,
  size = 48,
  style,
  testID,
}: WeatherIconProps): React.ReactElement {
  const uri = `${ICON_BASE_URL}/${icon}@2x.png`;

  return (
    <View style={[styles.container, { width: size, height: size }, style]} testID={testID}>
      <Image
        source={{ uri }}
        style={{ width: size, height: size }}
        resizeMode="contain"
        accessibilityLabel={`Weather icon ${icon}`}
      />
    </View>
  );
});

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    justifyContent: 'center',
  },
});
