/**
 * ToastNotification — Componente animado para mostrar toasts.
 * Se suscribe al store de toast y renderiza con animación slide-in/out.
 * Montar una sola vez en App.tsx.
 */
import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  View,
  StyleSheet,
  Animated,
  TouchableOpacity,
  Platform,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { colors, borderRadius, spacing, typography } from '@hydroespinaca/shared';
import type { IconName } from '@hydroespinaca/shared';
import { subscribeToast, hideToast } from '../../utils/toast';

type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastData {
  id: string;
  type: ToastType;
  message: string;
}

const TOAST_CONFIG: Record<ToastType, { icon: IconName; bg: string; border: string; text: string }> = {
  success: {
    icon: 'check-circle',
    bg: colors.hidro[50],
    border: colors.hidro[300],
    text: colors.hidro[800],
  },
  error: {
    icon: 'x-circle',
    bg: colors.error[50],
    border: colors.error[300],
    text: colors.error[800],
  },
  warning: {
    icon: 'alert-triangle',
    bg: colors.warning[50],
    border: colors.warning[300],
    text: colors.warning[800],
  },
  info: {
    icon: 'info',
    bg: colors.info[50],
    border: colors.info[300],
    text: colors.info[800],
  },
};

export function ToastNotification(): React.ReactElement | null {
  const insets = useSafeAreaInsets();
  const [toast, setToast] = useState<ToastData | null>(null);
  const translateY = useRef(new Animated.Value(-120)).current;
  const opacity = useRef(new Animated.Value(0)).current;

  const animateIn = useCallback(() => {
    Animated.parallel([
      Animated.spring(translateY, {
        toValue: 0,
        useNativeDriver: true,
        tension: 80,
        friction: 12,
      }),
      Animated.timing(opacity, {
        toValue: 1,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start();
  }, [translateY, opacity]);

  const animateOut = useCallback(() => {
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: -120,
        duration: 250,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start(() => {
      setToast(null);
    });
  }, [translateY, opacity]);

  useEffect(() => {
    const unsubscribe = subscribeToast((incoming) => {
      if (incoming) {
        setToast({ id: incoming.id, type: incoming.type, message: incoming.message });
        animateIn();
      } else {
        animateOut();
      }
    });
    return unsubscribe;
  }, [animateIn, animateOut]);

  if (!toast) return null;

  const config = TOAST_CONFIG[toast.type];

  return (
    <Animated.View
      style={[
        styles.container,
        {
          top: insets.top + spacing.sm,
          transform: [{ translateY }],
          opacity,
        },
      ]}
      pointerEvents="box-none"
    >
      <TouchableOpacity
        activeOpacity={0.9}
        onPress={() => hideToast()}
        style={[
          styles.toast,
          {
            backgroundColor: config.bg,
            borderColor: config.border,
          },
        ]}
        accessibilityRole="alert"
        accessibilityLiveRegion="assertive"
      >
        <Icon name={config.icon} size={20} color={config.text} />
        <Text
          variant="body"
          color={config.text}
          style={styles.message}
          numberOfLines={3}
        >
          {toast.message}
        </Text>
        <Icon name="x" size={16} color={config.text} />
      </TouchableOpacity>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    left: spacing.md,
    right: spacing.md,
    zIndex: 9999,
    elevation: 9999,
  },
  toast: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    gap: spacing.sm,
    ...Platform.select({
      ios: {
        shadowColor: colors.black,
        shadowOffset: { width: 0, height: 4 },
        shadowOpacity: 0.15,
        shadowRadius: 8,
      },
      android: {
        elevation: 6,
      },
    }),
  },
  message: {
    flex: 1,
    fontSize: typography.fontSize.sm,
  },
});
