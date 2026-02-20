/**
 * ErrorBoundary — Captura errores de renderizado en React.
 * Muestra un fallback amigable con opción de reintentar.
 */
import React, { Component, ErrorInfo, ReactNode } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';

export interface ErrorBoundaryProps {
  children: ReactNode;
  /** Fallback personalizado — recibe error y función reset */
  fallback?: (error: Error, resetError: () => void) => ReactNode;
  /** Callback al capturar error */
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('[ErrorBoundary]', error.message, errorInfo.componentStack);
    this.props.onError?.(error, errorInfo);
  }

  resetError = (): void => {
    this.setState({ hasError: false, error: null });
  };

  render(): ReactNode {
    if (this.state.hasError && this.state.error) {
      if (this.props.fallback) {
        return this.props.fallback(this.state.error, this.resetError);
      }

      return (
        <View style={styles.container}>
          <View style={styles.iconContainer}>
            <Icon name="alert-triangle" size={48} color={colors.error[500]} />
          </View>

          <Text variant="h3" color={semanticColors.textPrimary} align="center" style={styles.title}>
            Algo salió mal
          </Text>

          <Text variant="body" color={semanticColors.textSecondary} align="center" style={styles.description}>
            Ha ocurrido un error inesperado. Intenta recargar esta sección.
          </Text>

          <ScrollView style={styles.errorBox} contentContainerStyle={styles.errorBoxContent}>
            <Text variant="caption" color={colors.error[700]} style={styles.errorText}>
              {this.state.error.message}
            </Text>
          </ScrollView>

          <Button variant="primary" onPress={this.resetError} fullWidth>
            Reintentar
          </Button>
        </View>
      );
    }

    return this.props.children;
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    backgroundColor: colors.hidro[50],
    gap: spacing.md,
  },
  iconContainer: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: colors.error[50],
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
  },
  description: {
    maxWidth: 280,
  },
  errorBox: {
    maxHeight: 100,
    width: '100%',
    backgroundColor: colors.error[50],
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.error[200],
  },
  errorBoxContent: {
    padding: spacing.sm,
  },
  errorText: {
    fontFamily: 'monospace',
    fontSize: typography.fontSize.xs,
  },
});
