import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { FuzzyListScreen, FuzzyDetailScreen } from '../../screens';
import type { FuzzyStackParamList } from '../types';
import { semanticColors, colors } from '@hydroespinaca/shared';

const Stack = createNativeStackNavigator<FuzzyStackParamList>();

export function FuzzyStack(): React.ReactElement {
  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'slide_from_right',
        gestureEnabled: true,
      }}
    >
      <Stack.Screen name="FuzzyList" component={FuzzyListScreen} />
      <Stack.Screen
        name="FuzzyDetail"
        component={FuzzyDetailScreen}
        options={({ route }) => ({
          headerShown: true,
          title: route.params.systemName,
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        })}
      />
    </Stack.Navigator>
  );
}
