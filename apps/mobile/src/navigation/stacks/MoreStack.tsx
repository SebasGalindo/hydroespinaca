import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { MoreMenuScreen, ProfileScreen, NotificationSettingsScreen, NotificationHistoryScreen } from '../../screens';
import { AdminAccessScreen, AdminSessionsScreen } from '../../screens';
import type { MoreStackParamList } from '../types';
import { semanticColors, colors } from '@hydroespinaca/shared';

const Stack = createNativeStackNavigator<MoreStackParamList>();

export function MoreStack(): React.ReactElement {
  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'slide_from_right',
        gestureEnabled: true,
      }}
    >
      <Stack.Screen name="MoreMenu" component={MoreMenuScreen} />
      <Stack.Screen
        name="Profile"
        component={ProfileScreen}
        options={{
          headerShown: true,
          title: 'Mi Perfil',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
      <Stack.Screen
        name="NotificationSettings"
        component={NotificationSettingsScreen}
        options={{
          headerShown: true,
          title: 'Notificaciones',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
      <Stack.Screen
        name="NotificationHistory"
        component={NotificationHistoryScreen}
        options={{
          headerShown: true,
          title: 'Historial',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
      <Stack.Screen
        name="AdminAccess"
        component={AdminAccessScreen}
        options={{
          headerShown: true,
          title: 'Gestión de Acceso',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
      <Stack.Screen
        name="AdminSessions"
        component={AdminSessionsScreen}
        options={{
          headerShown: true,
          title: 'Sesiones Activas',
          headerTintColor: semanticColors.primary,
          headerStyle: { backgroundColor: colors.white },
          animation: 'slide_from_right',
        }}
      />
    </Stack.Navigator>
  );
}
