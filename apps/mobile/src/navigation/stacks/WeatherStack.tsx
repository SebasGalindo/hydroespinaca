import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { WeatherScreen, WeatherAlertDetailScreen } from '../../screens';
import type { WeatherStackParamList } from '../types';
import { semanticColors, colors } from '@hydroespinaca/shared';

const Stack = createNativeStackNavigator<WeatherStackParamList>();

export function WeatherStack(): React.ReactElement {
  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'slide_from_right',
        gestureEnabled: true,
      }}
    >
      <Stack.Screen name="Weather" component={WeatherScreen} />
      <Stack.Screen
        name="WeatherAlertDetail"
        component={WeatherAlertDetailScreen}
        options={{
          headerShown: true,
          title: 'Alerta Meteorológica',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
    </Stack.Navigator>
  );
}
