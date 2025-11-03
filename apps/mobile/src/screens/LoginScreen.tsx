import React, { useEffect } from 'react';
import { View, StyleSheet, ImageBackground } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { semanticColors, spacing, colors } from '@hydroespinaca/shared';
import { LoginForm } from '../components/organisms/LoginForm';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../context/AuthProvider';

type RootStackParamList = {
  Login: undefined;
  Dashboard: undefined;
};

type LoginScreenNavigationProp = NativeStackNavigationProp<RootStackParamList, 'Login'>;

export function LoginScreen(): React.ReactElement {
  const navigation = useNavigation<LoginScreenNavigationProp>();
  const { isAuthenticated, isLoading } = useAuth();

  // Redirect to dashboard if already authenticated
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      navigation.replace('Dashboard');
    }
  }, [isAuthenticated, isLoading, navigation]);

  const handleLoginSuccess = () => {
    console.log('Login successful!');
    // Navigation handled automatically by useEffect
  };

  return (
    <SafeAreaView style={styles.container}>
      <ImageBackground
        source={{ uri: 'https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=800&q=80' }}
        style={styles.backgroundImage}
        resizeMode="cover"
      >
        <View style={styles.overlay} />
        <View style={styles.content}>
          <LoginForm
            onLoginSuccess={handleLoginSuccess}
            testID="login-form"
          />
        </View>
      </ImageBackground>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  backgroundImage: {
    flex: 1,
    width: '100%',
    height: '100%',
  },
  overlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0, 0, 0, 0.4)',
    pointerEvents: 'none',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: spacing.lg,
  },
});