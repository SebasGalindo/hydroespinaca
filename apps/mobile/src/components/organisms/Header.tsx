import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { IconButton } from '../atoms/IconButton';
import { Avatar } from '../atoms/Avatar';

export function Header(): React.ReactElement {
  return (
    <View style={styles.container}>
      {/* Logo y nombre de la app */}
      <View style={styles.leftSection}>
        <View style={styles.logo}>
          <Text style={styles.logoText}>H</Text>
        </View>
        <Text 
          variant="h3" 
          color={semanticColors.primary}
        >
          HydroEspinaca
        </Text>
      </View>

      {/* Iconos de la derecha */}
      <View style={styles.rightSection}>
        <IconButton
          icon="bell"
          size="md"
          variant="ghost"
          color={semanticColors.textSecondary}
          onPress={() => {
            // TODO: Implementar navegación a notificaciones
          }}
          accessibilityLabel="Notificaciones"
        />
        <Avatar
          size="sm"
          fallback="U"
          backgroundColor={semanticColors.textPrimary}
          textColor={semanticColors.backgroundPrimary}
          style={styles.avatar}
          onPress={() => {
            // TODO: Implementar menú de usuario
          }}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    backgroundColor: semanticColors.backgroundPrimary,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  leftSection: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  logo: {
    width: 40,
    height: 40,
    borderRadius: 8,
    backgroundColor: semanticColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: spacing.sm,
  },
  logoText: {
    color: semanticColors.backgroundPrimary,
    fontSize: 20,
    fontWeight: 'bold',
  },
  rightSection: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  avatar: {
    marginLeft: spacing.xs,
  },
});