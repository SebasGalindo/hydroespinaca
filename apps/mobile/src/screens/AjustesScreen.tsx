import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';

export function AjustesScreen(): React.ReactElement {
  return (
    <View style={styles.content}>
      <Text variant="h1" color={semanticColors.primary} style={styles.title}>
        Ajustes
      </Text>
      <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
        Configuración y ajustes del sistema hidropónico
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  content: {
    flex: 1,
    padding: spacing.lg,
    justifyContent: 'center',
    alignItems: 'center',
  },
  title: {
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  description: {
    textAlign: 'center',
    lineHeight: 24,
  },
});