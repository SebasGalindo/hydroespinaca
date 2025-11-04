import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import type { NavigationContainerRef } from '@react-navigation/native';
import { LoginScreen, DashboardScreen } from '../screens';
import { MobileAuthInitializer } from '../components/MobileAuthInitializer';

const Stack = createNativeStackNavigator();

type RootStackParamList = {
  Login: undefined;
  Dashboard: undefined;
};

export function AppNavigator(): React.ReactElement {
  const navigationRef = React.useRef<NavigationContainerRef<RootStackParamList>>(null);

  return (
    <NavigationContainer ref={navigationRef}>
      {/* Initialize auth callbacks and periodic session validation */}
      <MobileAuthInitializer navigationRef={navigationRef} />
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerShown: false,
        }}
      >
        <Stack.Screen
          name="Login"
          component={LoginScreen}
        />
        <Stack.Screen
          name="Dashboard"
          component={DashboardScreen}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
