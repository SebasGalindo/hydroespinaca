import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AppNavigator } from './src/navigation';
import { AuthProvider } from './src/context/AuthProvider';

export default function App() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <StatusBar style="dark" backgroundColor="transparent" translucent={false} />
        <AppNavigator />
      </AuthProvider>
    </SafeAreaProvider>
  );
}
