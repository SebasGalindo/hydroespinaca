import React, { useEffect } from 'react';
import { View, StyleSheet, ImageBackground } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { semanticColors, spacing, colors } from '@hydroespinaca/shared';
import { LoginForm } from '../components/organisms/LoginForm';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useAuth } from '../context/AuthProvider';
import type { RootStackParamList } from '../navigation/types';

type LoginScreenNavigationProp = NativeStackNavigationProp<RootStackParamList, 'Login'>;

export function LoginScreen(): React.ReactElement {
  const navigation = useNavigation<LoginScreenNavigationProp>();
  const { isAuthenticated, isLoading, session } = useAuth();

  // Redirect to dashboard only if authenticated with a valid session
  useEffect(() => {
    if (isAuthenticated && !isLoading && session) {
      navigation.replace('MainTabs');
    }
  }, [isAuthenticated, isLoading, session, navigation]);

  const handleLoginSuccess = () => {
    // Navigate directly after successful login
    navigation.replace('MainTabs');
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
    backgroundColor: semanticColors.overlayMedium,
    pointerEvents: 'none',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    padding: spacing.lg,
  },
});