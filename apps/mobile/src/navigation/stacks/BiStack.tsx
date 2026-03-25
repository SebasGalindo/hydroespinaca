import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { BiScreen } from '../../screens';
import type { BiStackParamList } from '../types';

const Stack = createNativeStackNavigator<BiStackParamList>();

export function BiStack(): React.ReactElement {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Bi" component={BiScreen} />
    </Stack.Navigator>
  );
}
