import React, { useCallback } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { LoginForm } from '@hydroespinaca/shared-ui';
import { useAuth } from '../context/AuthProvider';
import { RootStackParamList } from '../navigation/AppNavigator';

type Props = NativeStackScreenProps<RootStackParamList, 'Login'>;

export const LoginScreen: React.FC<Props> = ({ navigation }) => {
  const { login, isLoading, error, clearError } = useAuth();

  const handleLogin = useCallback(async (email: string, password: string) => {
    try {
      await login(email, password);
      // Navigation will be handled automatically by AppNavigator when auth state changes
    } catch (err) {
      // Error is already handled by the hook
    }
  }, [login]);

  return (
    <ScrollView style={styles.container}>
      <View style={styles.loginContainer}>
        <LoginForm
          onSubmit={handleLogin}
          isLoading={isLoading}
          error={error}
          onClearError={clearError}
        />
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  loginContainer: {
    margin: 20,
    padding: 20,
    backgroundColor: '#fff',
    borderRadius: 12,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#2E7D32',
    textAlign: 'center',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    marginBottom: 32,
  },
  placeholder: {
    backgroundColor: '#FFF3E0',
    padding: 20,
    borderRadius: 8,
    borderLeftWidth: 4,
    borderLeftColor: '#FF9800',
    alignItems: 'center',
  },
  placeholderText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#F57C00',
    marginBottom: 8,
  },
  placeholderSubtext: {
    fontSize: 14,
    color: '#E65100',
    textAlign: 'center',
    marginBottom: 4,
  },
});