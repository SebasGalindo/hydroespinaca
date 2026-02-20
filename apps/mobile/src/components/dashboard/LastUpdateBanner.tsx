import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { semanticColors, spacing, colors, borderRadius, typography } from '@hydroespinaca/shared';

export interface LastUpdateBannerProps {
  timestamp: string;
}

function formatColombiaDateTime(isoString: string): string {
  const date = new Date(isoString);
  const options: Intl.DateTimeFormatOptions = {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
    timeZone: 'America/Bogota',
  };
  return new Intl.DateTimeFormat('es-CO', options).format(date);
}

export function LastUpdateBanner({
  timestamp,
}: LastUpdateBannerProps): React.ReactElement {
  return (
    <View style={styles.container} accessibilityRole="text" accessibilityLabel={`Última actualización: ${formatColombiaDateTime(timestamp)}`}>
      <Text style={styles.emoji}>🔄</Text>
      <View style={styles.textContainer}>
        <Text variant="caption" color={colors.hidro[700]} style={styles.text}>
          Última actualización: {formatColombiaDateTime(timestamp)}
        </Text>
        <Text variant="caption" color={colors.hidro[700]} style={styles.subtext}>
          Actualiza cada 2 minutos
        </Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    marginHorizontal: spacing.lg,
    marginBottom: spacing.lg,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.success[50],
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.success[300],
  },
  emoji: {
    fontSize: typography.fontSize.xl,
    flexShrink: 0,
  },
  textContainer: {
    flex: 1,
    flexShrink: 1,
  },
  text: {
    fontWeight: typography.fontWeight.medium,
    fontSize: typography.fontSize.xs,
    lineHeight: 16,
    flexWrap: 'wrap',
  },
  subtext: {
    marginTop: 2,
    fontSize: 11,
    lineHeight: 14,
  },
});
