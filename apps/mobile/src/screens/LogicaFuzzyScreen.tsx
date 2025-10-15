import React from 'react';
import { View, StyleSheet, SafeAreaView } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';

export function LogicaFuzzyScreen(): React.ReactElement {
  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Lógica Fuzzy
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
          Sistema de control inteligente
        </Text>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.backgroundPrimary,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
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