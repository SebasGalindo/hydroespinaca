import React, { useState } from 'react';
import { View, StyleSheet, ScrollView, KeyboardAvoidingView, Platform } from 'react-native';
import { semanticColors, spacing, borderRadius, typography, useLoginForm, colors } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Heading } from '../atoms/Heading';
import { Input } from '../atoms/Input';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { Pressable } from '../atoms/Pressable';

export interface LoginFormProps {
  onLoginSuccess?: (email: string, password: string) => void;
  style?: any;
  testID?: string;
}

export function LoginForm({
  onLoginSuccess,
  style,
  testID,
}: LoginFormProps): React.ReactElement {
  const { formState, isLoading, error, handleChange, handleSubmit, clearError } = useLoginForm();
  const [showPassword, setShowPassword] = useState(false);

  const handleFormSubmit = async () => {
    try {
      await handleSubmit({ preventDefault: () => {} });
      // Only navigate on actual success (login throws on failure)
      onLoginSuccess?.(formState.email, formState.password);
    } catch (err) {
      // Error is already set in authStore state and displayed in the form
    }
  };

  const handleEmailChange = (text: string) => {
    handleChange({ target: { name: 'email', value: text } });
  };

  const handlePasswordChange = (text: string) => {
    handleChange({ target: { name: 'password', value: text } });
  };

  return (
    <ScrollView
      contentContainerStyle={styles.scrollContainer}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
      bounces={false}
      style={styles.scrollView}
    >
      <View style={[styles.container, style]} testID={testID}>
          {/* Logo y Header */}
          <View style={styles.header}>
            <View style={styles.logoContainer}>
              <Icon 
                name="user" 
                size={32} 
                color={colors.white}
              />
            </View>
            <Heading
              level={3}
              color={semanticColors.textPrimary}
              style={styles.title}
            >
              HydroEspinaca
            </Heading>
            <Text 
              variant="body" 
              color={semanticColors.textSecondary}
              style={styles.subtitle}
            >
              Bienvenido de nuevo! Ingresa tus credenciales.
            </Text>
          </View>

          {/* Error Message */}
          {error && (
            <View style={styles.errorContainer}>
              <View style={styles.errorContent}>
                <Icon 
                  name="warning" 
                  size={16} 
                  color={semanticColors.errorText}
                />
                <Text 
                  variant="caption" 
                  color={semanticColors.errorText}
                  style={styles.errorText}
                >
                  {error}
                </Text>
              </View>
              <Pressable 
                onPress={clearError}
                style={styles.errorCloseButton}
                accessibilityLabel="Cerrar mensaje de error"
              >
                <Icon 
                  name="close" 
                  size={16} 
                  color={semanticColors.errorText}
                />
              </Pressable>
            </View>
          )}

          {/* Form Fields */}
          <View style={styles.form}>
            <View style={styles.fieldContainer}>
              <Text 
                variant="label" 
                color={semanticColors.textPrimary}
                style={styles.fieldLabel}
              >
                Correo electrónico
              </Text>
              <Input
                value={formState.email}
                onChangeText={handleEmailChange}
                placeholder="correo@ejemplo.com"
                keyboardType="email-address"
                autoCapitalize="none"
                autoCorrect={false}
                autoComplete="email"
                textContentType="emailAddress"
                leftIcon={
                  <Icon 
                    name="mail" 
                    size={20} 
                    color={semanticColors.textSecondary}
                  />
                }
                disabled={isLoading}
                testID={`${testID}-email-input`}
              />
            </View>

            <View style={styles.fieldContainer}>
              <Text 
                variant="label" 
                color={semanticColors.textPrimary}
                style={styles.fieldLabel}
              >
                Contraseña
              </Text>
              <Input
                value={formState.password}
                onChangeText={handlePasswordChange}
                placeholder="********"
                secureTextEntry={!showPassword}
                autoCapitalize="none"
                autoCorrect={false}
                autoComplete="password"
                textContentType="password"
                leftIcon={
                  <Icon 
                    name="lock" 
                    size={20} 
                    color={semanticColors.textSecondary}
                  />
                }
                rightIcon={
                  <Pressable 
                    onPress={() => setShowPassword(!showPassword)}
                    style={styles.passwordToggle}
                    accessibilityLabel={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                  >
                    <Icon 
                      name={showPassword ? "eye-off" : "eye"} 
                      size={20} 
                      color={semanticColors.textSecondary}
                    />
                  </Pressable>
                }
                disabled={isLoading}
                testID={`${testID}-password-input`}
              />
            </View>

            {/* Login Button */}
            <Button
              onPress={handleFormSubmit}
              variant="primary"
              size="lg"
              fullWidth
              loading={isLoading}
              disabled={isLoading || !formState.email || !formState.password}
              style={styles.loginButton}
              testID={`${testID}-login-button`}
            >
              {isLoading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
            </Button>
          </View>
        </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollView: {
    flex: 1,
  },
  scrollContainer: {
    flexGrow: 1,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.xl,
    minHeight: '100%',
  },
  container: {
    backgroundColor: semanticColors.background,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    shadowColor: colors.black,
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 4,
    maxWidth: 500,
    width: '100%',
    alignSelf: 'center',
  },
  header: {
    alignItems: 'center',
    marginBottom: spacing.lg,
  },
  logoContainer: {
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: semanticColors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.sm,
  },
  title: {
    marginBottom: spacing.xs,
    textAlign: 'center',
  },
  subtitle: {
    textAlign: 'center',
    lineHeight: 18,
    fontSize: typography.fontSize.sm,
  },
  errorContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: semanticColors.errorBg,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.lg,
  },
  errorContent: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  errorText: {
    marginLeft: spacing.sm,
    flex: 1,
  },
  errorCloseButton: {
    padding: spacing.xs,
  },
  form: {
    marginBottom: spacing.md,
  },
  fieldContainer: {
    marginBottom: spacing.md,
  },
  fieldLabel: {
    marginBottom: spacing.xs,
    fontWeight: typography.fontWeight.medium,
    fontSize: typography.fontSize.sm,
  },
  passwordToggle: {
    padding: spacing.xs,
  },
  loginButton: {
    marginTop: spacing.md,
  },
});