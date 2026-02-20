import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { AnalyticsScreen } from '../../screens';
import type { AnalyticsStackParamList } from '../types';

const Stack = createNativeStackNavigator<AnalyticsStackParamList>();

export function AnalyticsStack(): React.ReactElement {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Analytics" component={AnalyticsScreen} />
    </Stack.Navigator>
  );
}
