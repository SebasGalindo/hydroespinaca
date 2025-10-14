import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Text } from '../components/atoms/Text';

export function EstadoSistemaScreen(): React.ReactElement {
  return (
    <View style={styles.content}>
      <Text variant="h1" color={semanticColors.primary} style={styles.title}>
        Estado del Sistema
      </Text>
      <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
        Monitoreo del estado general del sistema hidropónico
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