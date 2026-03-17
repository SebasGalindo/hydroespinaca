/**
 * ProfileScreen — Perfil del usuario.
 * Avatar con icono de rol, datos de cuenta, botón logout.
 */
import React, { useState, useMemo, useCallback } from 'react';
import { View, ScrollView, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import Constants from 'expo-constants';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { Button } from '../../components/atoms/Button';
import { Card } from '../../components/molecules/Card';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import { useAuth } from '../../context/AuthProvider';
import { semanticColors, spacing, colors, borderRadius, typography, isUserAdmin } from '@hydroespinaca/shared';
import type { IconName } from '@hydroespinaca/shared';

export function ProfileScreen(): React.ReactElement {
  const navigation = useNavigation();
  const { session, logout } = useAuth();
  const [logoutVisible, setLogoutVisible] = useState(false);

  const isAdmin = isUserAdmin(session);

  const handleLogout = useCallback(async () => {
    setLogoutVisible(false);
    if (__DEV__) console.log('[Profile] handleLogout — starting');
    try {
      await logout();
      if (__DEV__) console.log('[Profile] handleLogout — logout resolved');
    } catch (e) {
      if (__DEV__) console.warn('[Profile] handleLogout — logout error (navigating anyway):', e);
    }
    // Navigate back to Login — walk up to root navigator
    let nav: any = navigation;
    while (nav.getParent()) nav = nav.getParent();
    if (__DEV__) console.log('[Profile] handleLogout — resetting to Login');
    nav.reset({ index: 0, routes: [{ name: 'Login' }] });
  }, [logout, navigation]);

  const appVersion = Constants.expoConfig?.version ?? '1.0.0';

  const infoRows = useMemo((): { label: string; value: string; icon: IconName }[] => [
    { label: 'Nombre', value: session?.username ?? 'No disponible', icon: 'user' },
    { label: 'Email', value: session?.email ?? 'No disponible', icon: 'mail' },
    { label: 'Rol', value: session?.role ?? 'No disponible', icon: isAdmin ? 'lock' : 'user' },
  ], [session?.username, session?.email, session?.role, isAdmin]);

  return (
    <SafeAreaView style={styles.container} edges={['bottom']}>
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {/* Avatar */}
        <View style={styles.avatarSection}>
          <View style={[styles.avatar, isAdmin && styles.avatarAdmin]}>
            <Icon
              name={isAdmin ? 'lock' : 'user'}
              size={48}
              color="#FFFFFF"
            />
          </View>
          <Text variant="h2" color={semanticColors.textPrimary} style={styles.name}>
            {session?.username ?? 'Usuario'}
          </Text>
          <View style={[styles.roleBadge, isAdmin && styles.roleBadgeAdmin]}>
            <Text
              variant="caption"
              color={isAdmin ? colors.warning[700] : colors.hidro[700]}
              style={styles.roleText}
            >
              {session?.role ?? 'Usuario'}
            </Text>
          </View>
        </View>

        {/* Info card */}
        <Card variant="outlined" padding="none">
          {infoRows.map((row, idx) => (
            <React.Fragment key={row.label}>
              {idx > 0 && <View style={styles.divider} />}
              <View style={styles.infoRow}>
                <View style={styles.infoIconContainer}>
                  <Icon name={row.icon} size={18} color={semanticColors.textTertiary} />
                </View>
                <View style={styles.infoTextContainer}>
                  <Text variant="caption" color={semanticColors.textTertiary}>
                    {row.label}
                  </Text>
                  <Text variant="body" color={semanticColors.textPrimary} numberOfLines={1}>
                    {row.value}
                  </Text>
                </View>
              </View>
            </React.Fragment>
          ))}
        </Card>

        {/* Session info */}
        {session?.sessionId && (
          <Card variant="outlined" padding="md">
            <View style={styles.sessionRow}>
              <Icon name="activity" size={16} color={semanticColors.textTertiary} />
              <Text variant="caption" color={semanticColors.textTertiary}>
                Sesión activa
              </Text>
            </View>
            <Text variant="caption" color={semanticColors.textTertiary} numberOfLines={1} style={styles.sessionId}>
              ID: {session.sessionId.substring(0, 16)}…
            </Text>
          </Card>
        )}

        {/* Logout button */}
        <Button
          variant="outline"
          onPress={() => setLogoutVisible(true)}
          fullWidth
          leftIcon={<Icon name="log-out" size={18} color={colors.error[600]} />}
          style={styles.logoutButton}
        >
          <Text variant="body" color={colors.error[600]} style={styles.logoutText}>
            Cerrar Sesión
          </Text>
        </Button>

        {/* Version */}
        <Text variant="caption" color={semanticColors.textTertiary} align="center" style={styles.version}>
          HydroEspinaca Mobile v{appVersion}
        </Text>
      </ScrollView>

      {/* Logout confirmation */}
      <ConfirmationSheet
        isOpen={logoutVisible}
        title="Cerrar Sesión"
        message="¿Estás seguro de que deseas cerrar sesión?"
        confirmLabel="Cerrar Sesión"
        destructive
        onConfirm={handleLogout}
        onCancel={() => setLogoutVisible(false)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro[50],
  },
  scrollContent: {
    padding: spacing.lg,
    gap: spacing.lg,
  },
  // Avatar section
  avatarSection: {
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.md,
  },
  avatar: {
    width: 96,
    height: 96,
    borderRadius: 48,
    backgroundColor: colors.hidro[500],
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarAdmin: {
    backgroundColor: colors.warning[500],
  },
  name: {
    fontWeight: typography.fontWeight.bold,
  },
  roleBadge: {
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.hidro[100],
    borderRadius: borderRadius.xl,
  },
  roleBadgeAdmin: {
    backgroundColor: colors.warning[100],
  },
  roleText: {
    fontWeight: typography.fontWeight.semibold,
  },
  // Info card
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: spacing.md,
    gap: spacing.md,
  },
  infoIconContainer: {
    width: 32,
    height: 32,
    borderRadius: borderRadius.md,
    backgroundColor: colors.gray[100],
    justifyContent: 'center',
    alignItems: 'center',
  },
  infoTextContainer: {
    flex: 1,
    gap: 2,
  },
  divider: {
    height: 1,
    backgroundColor: colors.gray[200],
    marginHorizontal: spacing.md,
  },
  // Session info
  sessionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  sessionId: {
    marginTop: spacing.xs,
    fontFamily: 'monospace',
  },
  // Logout
  logoutButton: {
    borderColor: colors.error[300],
  },
  logoutText: {
    fontWeight: typography.fontWeight.semibold,
  },
  // Version
  version: {
    marginTop: spacing.sm,
  },
});
