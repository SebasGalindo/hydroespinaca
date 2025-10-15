import React, { useEffect } from 'react';
import { View, StyleSheet, ImageBackground, SafeAreaView } from 'react-native';
import { semanticColors, spacing, colors } from '@hydroespinaca/shared';
import { LoginForm } from '../components/organisms/LoginForm';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../context/AuthProvider';

type RootStackParamList = {
  Login: undefined;
  MainTabs: undefined;
};

type LoginScreenNavigationProp = NativeStackNavigationProp<RootStackParamList, 'Login'>;

export function LoginScreen(): React.ReactElement {
  const navigation = useNavigation<LoginScreenNavigationProp>();
  const { isAuthenticated, isLoading } = useAuth();

  // Redirect to main tabs if already authenticated
  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      navigation.replace('MainTabs');
    }
  }, [isAuthenticated, isLoading, navigation]);

  const handleLoginSuccess = () => {
    console.log('Login successful!');
    // Navigation handled automatically by useEffect
  };

  const handleForgotPassword = () => {
    console.log('Forgot password pressed');
    // Aquí se manejaría la navegación a la pantalla de recuperación
  };

  const handleSignUp = () => {
    console.log('Sign up pressed');
    // Aquí se manejaría la navegación a la pantalla de registro
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
            onForgotPassword={handleForgotPassword}
            onSignUp={handleSignUp}
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
    backgroundColor: colors.hidro.bgLight,
  },
  backgroundImage: {
    flex: 1,
    width: '100%',
    height: '100%',
  },
  overlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0, 0, 0, 0.4)',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: spacing.lg,
  },
});