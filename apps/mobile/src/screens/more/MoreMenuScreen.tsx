/**
 * MoreMenuScreen — Menú "Más" con perfil rápido,
 * opciones de cuenta y administración (solo admin).
 */
import React, { useState, useMemo } from 'react';
import { View, StyleSheet, SectionList, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Text } from '../../components/atoms/Text';
import { Icon } from '../../components/atoms/Icon';
import { ConfirmationSheet } from '../../components/organisms/ConfirmationSheet';
import type { MoreStackParamList } from '../../navigation/types';
import type { IconName } from '@hydroespinaca/shared';
import { useAuth } from '../../context/AuthProvider';
import { semanticColors, spacing, colors, borderRadius, typography, isUserAdmin } from '@hydroespinaca/shared';

type MoreNavProp = NativeStackNavigationProp<MoreStackParamList, 'MoreMenu'>;

interface MenuItem {
  id: string;
  label: string;
  subtitle?: string;
  icon: IconName;
  screen?: keyof MoreStackParamList;
  action?: () => void;
  destructive?: boolean;
}

interface MenuSection {
  title: string;
  data: MenuItem[];
}

export function MoreMenuScreen(): React.ReactElement {
  const navigation = useNavigation<MoreNavProp>();
  const { session, logout } = useAuth();
  const [logoutVisible, setLogoutVisible] = useState(false);

  const isAdmin = isUserAdmin(session);

  const sections: MenuSection[] = useMemo(() => {
    const result: MenuSection[] = [
      {
        title: 'Cuenta',
        data: [
          {
            id: 'profile',
            label: 'Mi Perfil',
            subtitle: 'Información de tu cuenta',
            icon: 'user',
            screen: 'Profile',
          },
          {
            id: 'notifications',
            label: 'Notificaciones',
            subtitle: 'Canales y preferencias',
            icon: 'bell',
            screen: 'NotificationSettings',
          },
          {
            id: 'notification-history',
            label: 'Historial',
            subtitle: 'Notificaciones enviadas',
            icon: 'mail',
            screen: 'NotificationHistory',
          },
          {
            id: 'chat',
            label: 'Asistente IA',
            subtitle: 'Chat con RAG inteligente',
            icon: 'zap',
            screen: 'Chat',
          },
        ],
      },
    ];

    if (isAdmin) {
      result.push({
        title: 'Administración',
        data: [
          {
            id: 'access',
            label: 'Gestión de Acceso',
            subtitle: 'Usuarios y roles',
            icon: 'lock',
            screen: 'AdminAccess',
          },
          {
            id: 'sessions',
            label: 'Sesiones Activas',
            subtitle: 'Monitoreo de sesiones',
            icon: 'activity',
            screen: 'AdminSessions',
          },
        ],
      });
    }

    result.push({
      title: '',
      data: [
        {
          id: 'logout',
          label: 'Cerrar Sesión',
          icon: 'log-out',
          action: () => setLogoutVisible(true),
          destructive: true,
        },
      ],
    });

    return result;
  }, [isAdmin]);

  const handleLogout = async () => {
    setLogoutVisible(false);
    if (__DEV__) console.log('[MoreMenu] handleLogout — starting');
    try {
      await logout();
      if (__DEV__) console.log('[MoreMenu] handleLogout — logout resolved');
    } catch (e) {
      if (__DEV__) console.warn('[MoreMenu] handleLogout — logout error (navigating anyway):', e);
    }
    // Navigate back to Login — walk up to root navigator
    let nav: any = navigation;
    while (nav.getParent()) nav = nav.getParent();
    if (__DEV__) console.log('[MoreMenu] handleLogout — resetting to Login');
    nav.reset({ index: 0, routes: [{ name: 'Login' }] });
  };

  return (
    <SafeAreaView style={styles.container}>
      {/* User header */}
      <View style={styles.header}>
        <Text variant="h2" color={semanticColors.primary} style={styles.title}>
          Más
        </Text>
      </View>

      {/* User quick card */}
      <TouchableOpacity
        style={styles.userCard}
        activeOpacity={0.7}
        onPress={() => navigation.navigate('Profile')}
        accessibilityRole="button"
        accessibilityLabel="Ver perfil"
      >
        <View style={styles.userAvatar}>
          <Icon
            name={isAdmin ? 'lock' : 'user'}
            size={28}
            color={colors.hidro[600]}
          />
        </View>
        <View style={styles.userInfo}>
          <Text variant="body" color={semanticColors.textPrimary} style={styles.userName}>
            {session?.username ?? 'Usuario'}
          </Text>
          <Text variant="caption" color={semanticColors.textTertiary}>
            {session?.email ?? ''}
          </Text>
        </View>
        <Icon name="chevron-right" size={18} color={semanticColors.textTertiary} />
      </TouchableOpacity>

      {/* Menu sections */}
      <SectionList
        sections={sections}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContent}
        stickySectionHeadersEnabled={false}
        renderSectionHeader={({ section }) =>
          section.title ? (
            <Text variant="overline" color={semanticColors.textTertiary} style={styles.sectionHeader}>
              {section.title}
            </Text>
          ) : (
            <View style={styles.sectionSpacer} />
          )
        }
        renderItem={({ item }) => (
          <TouchableOpacity
            style={[styles.menuItem, item.destructive && styles.menuItemDestructive]}
            activeOpacity={0.7}
            onPress={() => {
              if (item.action) {
                item.action();
              } else if (item.screen) {
                navigation.navigate(item.screen);
              }
            }}
            accessibilityRole="button"
            accessibilityLabel={item.label}
          >
            <View style={[
              styles.menuIcon,
              { backgroundColor: item.destructive ? `${colors.error[600]}12` : `${colors.hidro[600]}12` },
            ]}>
              <Icon
                name={item.icon}
                size={20}
                color={item.destructive ? colors.error[600] : colors.hidro[600]}
              />
            </View>
            <View style={styles.menuTextContainer}>
              <Text
                variant="body"
                color={item.destructive ? colors.error[600] : semanticColors.textPrimary}
                style={styles.menuLabel}
              >
                {item.label}
              </Text>
              {item.subtitle && (
                <Text variant="caption" color={semanticColors.textTertiary}>
                  {item.subtitle}
                </Text>
              )}
            </View>
            {!item.destructive && (
              <Icon name="chevron-right" size={16} color={semanticColors.textTertiary} />
            )}
          </TouchableOpacity>
        )}
      />

      {/* Logout confirmation */}
      <ConfirmationSheet
        isOpen={logoutVisible}
        title="Cerrar Sesión"
        message="¿Estás seguro de que deseas cerrar sesión? Tendrás que iniciar sesión de nuevo."
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
  header: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.sm,
  },
  title: {
    fontWeight: typography.fontWeight.bold,
  },
  // User card
  userCard: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: semanticColors.surface,
    marginHorizontal: spacing.lg,
    marginBottom: spacing.md,
    padding: spacing.md,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.gray[200],
    gap: spacing.md,
  },
  userAvatar: {
    width: 52,
    height: 52,
    borderRadius: 26,
    backgroundColor: colors.hidro[100],
    justifyContent: 'center',
    alignItems: 'center',
  },
  userInfo: {
    flex: 1,
    gap: 2,
  },
  userName: {
    fontWeight: typography.fontWeight.semibold,
  },
  // List
  listContent: {
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl,
  },
  sectionHeader: {
    marginTop: spacing.lg,
    marginBottom: spacing.sm,
    textTransform: 'uppercase',
    letterSpacing: 1,
    fontWeight: typography.fontWeight.semibold,
  },
  sectionSpacer: {
    height: spacing.md,
  },
  // Menu item
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: semanticColors.surface,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius.md,
    marginBottom: spacing.xs,
    borderWidth: 1,
    borderColor: colors.gray[200],
    gap: spacing.md,
  },
  menuItemDestructive: {
    borderColor: `${colors.error[600]}20`,
  },
  menuIcon: {
    width: 36,
    height: 36,
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
  },
  menuTextContainer: {
    flex: 1,
    gap: 1,
  },
  menuLabel: {
    fontWeight: typography.fontWeight.medium,
  },
});
