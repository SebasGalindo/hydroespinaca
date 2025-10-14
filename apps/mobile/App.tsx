import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { AppNavigator } from './src/navigation';
import { AuthProvider } from './src/context/AuthProvider';

export default function App() {
  return (
    <AuthProvider>
      <StatusBar style="dark" backgroundColor="transparent" translucent={false} />
      <AppNavigator />
    </AuthProvider>
  );
}
