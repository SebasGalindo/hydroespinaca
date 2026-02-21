import React from 'react';
import { StyleSheet, Platform, View } from 'react-native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { colors, semanticColors, typography, borderRadius } from '@hydroespinaca/shared';
import { useAuth } from '../context/AuthProvider';
import type { MainTabParamList } from './types';
import { DashboardStack } from './stacks/DashboardStack';
import { WeatherStack } from './stacks/WeatherStack';
import { AnalyticsStack } from './stacks/AnalyticsStack';
import { BiStack } from './stacks/BiStack';
import { FuzzyStack } from './stacks/FuzzyStack';
import { MoreStack } from './stacks/MoreStack';

const Tab = createBottomTabNavigator<MainTabParamList>();

const TAB_ICONS: Record<keyof MainTabParamList, { focused: keyof typeof Ionicons.glyphMap; unfocused: keyof typeof Ionicons.glyphMap }> = {
  DashboardTab: { focused: 'home', unfocused: 'home-outline' },
  WeatherTab: { focused: 'partly-sunny', unfocused: 'partly-sunny-outline' },
  AnalyticsTab: { focused: 'stats-chart', unfocused: 'stats-chart-outline' },
  BiTab: { focused: 'wallet', unfocused: 'wallet-outline' },
  FuzzyTab: { focused: 'git-network', unfocused: 'git-network-outline' },
  MoreTab: { focused: 'ellipsis-horizontal', unfocused: 'ellipsis-horizontal-outline' },
};

const TAB_LABELS: Record<keyof MainTabParamList, string> = {
  DashboardTab: 'Inicio',
  WeatherTab: 'Clima',
  AnalyticsTab: 'Análisis',
  BiTab: 'Consumo',
  FuzzyTab: 'Rutinas',
  MoreTab: 'Más',
};

export function MainTabNavigator(): React.ReactElement {
  const { session } = useAuth();
  const isAdmin =
    session?.role?.toLowerCase() === 'administrador' ||
    session?.role?.toLowerCase() === 'admin';

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarIcon: ({ focused, color, size }) => {
          const icons = TAB_ICONS[route.name];
          const iconName = focused ? icons.focused : icons.unfocused;
          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarLabel: TAB_LABELS[route.name],
        tabBarActiveTintColor: colors.hidro[600],
        tabBarInactiveTintColor: colors.gray[400],
        tabBarStyle: styles.tabBar,
        tabBarLabelStyle: styles.tabBarLabel,
        tabBarHideOnKeyboard: true,
      })}
    >
      <Tab.Screen name="DashboardTab" component={DashboardStack} />
      <Tab.Screen name="WeatherTab" component={WeatherStack} />
      <Tab.Screen name="AnalyticsTab" component={AnalyticsStack} />
      <Tab.Screen name="BiTab" component={BiStack} />
      <Tab.Screen name="FuzzyTab" component={FuzzyStack} />
      <Tab.Screen
        name="MoreTab"
        component={MoreStack}
        options={isAdmin ? { tabBarBadge: '', tabBarBadgeStyle: styles.adminBadge } : undefined}
      />
    </Tab.Navigator>
  );
}

const styles = StyleSheet.create({
  tabBar: {
    backgroundColor: colors.white,
    borderTopWidth: 1,
    borderTopColor: colors.gray[200],
    paddingBottom: Platform.OS === 'ios' ? 20 : 8,
    paddingTop: 8,
    height: Platform.OS === 'ios' ? 88 : 64,
    elevation: 8,
    shadowColor: colors.black,
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.08,
    shadowRadius: 8,
  },
  tabBarLabel: {
    fontSize: 11,
    fontWeight: typography.fontWeight.semibold,
  },
  adminBadge: {
    backgroundColor: colors.warning[500],
    minWidth: 8,
    maxHeight: 8,
    borderRadius: borderRadius.sm,
    fontSize: 0,
    lineHeight: 8,
    top: 2,
    right: -4,
  },
});
