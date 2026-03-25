import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { BottomSheetModalProvider } from '@gorhom/bottom-sheet';
import { RootNavigator } from './src/navigation';
import { AuthProvider } from './src/context/AuthProvider';
import { ToastNotification } from './src/components/molecules/ToastNotification';
import { ErrorBoundary } from './src/components/organisms/ErrorBoundary';

export default function App() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <BottomSheetModalProvider>
          <ErrorBoundary>
            <AuthProvider>
              <StatusBar style="dark" backgroundColor="transparent" translucent={false} />
              <RootNavigator />
              <ToastNotification />
            </AuthProvider>
          </ErrorBoundary>
        </BottomSheetModalProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
