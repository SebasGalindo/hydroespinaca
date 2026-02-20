import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, colors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { useAuth } from '../../context/AuthProvider';

export interface AdminGuardProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export function AdminGuard({
  children,
  fallback,
}: AdminGuardProps): React.ReactElement {
  const { session } = useAuth();
  const isAdmin =
    session?.role?.toLowerCase() === 'administrador' ||
    session?.role?.toLowerCase() === 'admin';

  if (!isAdmin) {
    if (fallback) {
      return <>{fallback}</>;
    }

    return (
      <View style={styles.container}>
        <Icon name="lock" size={56} color={semanticColors.textTertiary} />
        <Text variant="h3" color={semanticColors.textSecondary} align="center" style={styles.title}>
          Acceso restringido
        </Text>
        <Text variant="body" color={semanticColors.textTertiary} align="center">
          Necesitas permisos de administrador para acceder a esta sección.
        </Text>
      </View>
    );
  }

  return <>{children}</>;
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
    gap: spacing.md,
    backgroundColor: colors.hidro[50],
  },
  title: {
    fontWeight: typography.fontWeight.semibold,
  },
});
