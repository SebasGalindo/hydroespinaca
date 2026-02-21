import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { NavigationContainerRef, LinkingOptions } from '@react-navigation/native';
import { LoginScreen } from '../screens';
import { MobileAuthInitializer } from '../components/MobileAuthInitializer';
import { MainTabNavigator } from './MainTabNavigator';
import type { RootStackParamList } from './types';

const Stack = createNativeStackNavigator<RootStackParamList>();

const linking: LinkingOptions<RootStackParamList> = {
  prefixes: ['hydroespinaca://', 'https://hydroespinaca.app'],
  config: {
    screens: {
      Login: 'login',
      MainTabs: {
        screens: {
          DashboardTab: {
            screens: { Dashboard: 'dashboard' },
          },
          WeatherTab: {
            screens: {
              Weather: 'clima',
              WeatherAlertDetail: 'clima/alerta/:alertId',
            },
          },
          AnalyticsTab: {
            screens: { Analytics: 'analytics' },
          },
          BiTab: {
            screens: { Bi: 'consumo' },
          },
          FuzzyTab: {
            screens: {
              FuzzyList: 'rutinas',
              FuzzyDetail: 'rutinas/:systemId',
            },
          },
          MoreTab: {
            screens: {
              MoreMenu: 'mas',
              Profile: 'perfil',
              NotificationSettings: 'notificaciones',
              NotificationHistory: 'notificaciones/historial',
              AdminAccess: 'admin/acceso',
              AdminSessions: 'admin/sesiones',
            },
          },
        },
      },
    },
  },
};

export function RootNavigator(): React.ReactElement {
  const navigationRef = React.useRef<NavigationContainerRef<RootStackParamList>>(null);

  return (
    <NavigationContainer ref={navigationRef} linking={linking}>
      {/* Initialize auth callbacks and periodic session validation */}
      <MobileAuthInitializer navigationRef={navigationRef} />
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerShown: false,
          animation: 'fade',
        }}
      >
        <Stack.Screen
          name="Login"
          component={LoginScreen}
        />
        <Stack.Screen
          name="MainTabs"
          component={MainTabNavigator}
          options={{ animation: 'fade' }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
