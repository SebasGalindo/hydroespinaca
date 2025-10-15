import React, { useEffect, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  Animated,
  TouchableOpacity,
  Dimensions,
  Platform,
} from 'react-native';
import { IconName } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

const { width } = Dimensions.get('window');

export interface ToastProps {
  /** Tipo de toast */
  type?: 'success' | 'error' | 'warning' | 'info' | 'default';
  /** Texto principal del toast */
  text1?: string;
  /** Texto secundario del toast */
  text2?: string;
  /** Posición del toast */
  position?: 'top' | 'bottom';
  /** Si el toast es visible */
  visible?: boolean;
  /** Duración en milisegundos antes de auto-ocultar */
  visibilityTime?: number;
  /** Si se oculta automáticamente */
  autoHide?: boolean;
  /** Si se puede deslizar para cerrar */
  swipeable?: boolean;
  /** Offset desde arriba (px) */
  topOffset?: number;
  /** Offset desde abajo (px) */
  bottomOffset?: number;
  /** Callback cuando se muestra */
  onShow?: () => void;
  /** Callback cuando se oculta */
  onHide?: () => void;
  /** Callback cuando se presiona */
  onPress?: () => void;
  /** Variante de estilo */
  variant?: 'default' | 'filled' | 'minimal';
  /** Icono personalizado */
  icon?: IconName;
  /** Si mostrar icono */
  showIcon?: boolean;
  /** Estilo personalizado */
  style?: any;
  /** Estilo del texto principal */
  text1Style?: any;
  /** Estilo del texto secundario */
  text2Style?: any;
}

export const Toast: React.FC<ToastProps> = ({
  type = 'success',
  text1,
  text2,
  position = 'top',
  visible = false,
  visibilityTime = 4000,
  autoHide = true,
  swipeable = true,
  topOffset = 40,
  bottomOffset = 40,
  onShow,
  onHide,
  onPress,
  variant = 'default',
  icon,
  showIcon = true,
  style,
  text1Style,
  text2Style,
}) => {
  const translateY = useRef(new Animated.Value(position === 'top' ? -100 : 100)).current;
  const opacity = useRef(new Animated.Value(0)).current;
  const panY = useRef(new Animated.Value(0)).current;
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (visible) {
      showToast();
    } else {
      hideToast();
    }

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, [visible]);

  const showToast = () => {
    onShow?.();
    
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: 0,
        duration: 300,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      }),
    ]).start();

    if (autoHide && visibilityTime > 0) {
      timeoutRef.current = setTimeout(() => {
        hideToast();
      }, visibilityTime);
    }
  };

  const hideToast = () => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }

    Animated.parallel([
      Animated.timing(translateY, {
        toValue: position === 'top' ? -100 : 100,
        duration: 300,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: 300,
        useNativeDriver: true,
      }),
    ]).start(() => {
      onHide?.();
    });
  };

  const getTypeConfig = () => {
    const configs = {
      success: {
        backgroundColor: '#10b981',
        borderColor: '#059669',
        iconName: 'check' as IconName,
        iconColor: '#ffffff',
      },
      error: {
        backgroundColor: '#ef4444',
        borderColor: '#dc2626',
        iconName: 'close' as IconName,
        iconColor: '#ffffff',
      },
      warning: {
        backgroundColor: '#f59e0b',
        borderColor: '#d97706',
        iconName: 'warning' as IconName,
        iconColor: '#ffffff',
      },
      info: {
        backgroundColor: '#3b82f6',
        borderColor: '#2563eb',
        iconName: 'info' as IconName,
        iconColor: '#ffffff',
      },
      default: {
        backgroundColor: '#1f2937',
        borderColor: '#374151',
        iconName: 'bell' as IconName,
        iconColor: '#ffffff',
      },
    };

    return configs[type];
  };

  const getVariantStyles = () => {
    const typeConfig = getTypeConfig();
    
    const variants = {
      default: {
        backgroundColor: typeConfig.backgroundColor,
        borderWidth: 1,
        borderColor: typeConfig.borderColor,
      },
      filled: {
        backgroundColor: typeConfig.backgroundColor,
        borderWidth: 0,
      },
      minimal: {
        backgroundColor: '#ffffff',
        borderWidth: 1,
        borderColor: typeConfig.borderColor,
      },
    };

    return variants[variant];
  };

  const getTextColor = () => {
    if (variant === 'minimal') {
      return getTypeConfig().backgroundColor;
    }
    return '#ffffff';
  };

  const getIconColor = () => {
    if (variant === 'minimal') {
      return getTypeConfig().backgroundColor;
    }
    return getTypeConfig().iconColor;
  };

  if (!visible) {
    return null;
  }

  const typeConfig = getTypeConfig();
  const variantStyles = getVariantStyles();
  const textColor = getTextColor();
  const iconColor = getIconColor();

  const positionStyles = {
    top: topOffset,
    bottom: bottomOffset,
  };

  return (
    <Animated.View
      style={[
        styles.container,
        {
          [position]: positionStyles[position],
          transform: [{ translateY }],
          opacity,
        },
      ]}
    >
      <TouchableOpacity
        activeOpacity={0.8}
        onPress={onPress}
        style={[
          styles.toast,
          variantStyles,
          style,
        ]}
      >
        <View style={styles.content}>
          {showIcon && (
            <View style={styles.iconContainer}>
              <Icon
                name={icon || typeConfig.iconName}
                size={20}
                color={iconColor}
              />
            </View>
          )}
          
          <View style={styles.textContainer}>
            {text1 && (
              <Text
                style={[
                  styles.text1,
                  { color: textColor },
                  text1Style,
                ]}
                numberOfLines={2}
              >
                {text1}
              </Text>
            )}
            
            {text2 && (
              <Text
                style={[
                  styles.text2,
                  { color: textColor, opacity: 0.8 },
                  text2Style,
                ]}
                numberOfLines={3}
              >
                {text2}
              </Text>
            )}
          </View>
        </View>
      </TouchableOpacity>
    </Animated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    left: 16,
    right: 16,
    zIndex: 9999,
  },
  toast: {
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    shadowColor: '#000000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
  },
  content: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  iconContainer: {
    marginRight: 12,
    marginTop: 2,
  },
  textContainer: {
    flex: 1,
  },
  text1: {
    fontSize: 16,
    fontWeight: '600',
    lineHeight: 20,
  },
  text2: {
    fontSize: 14,
    fontWeight: '400',
    lineHeight: 18,
    marginTop: 4,
  },
});

// Hook para usar Toast de manera imperativa
export const useToast = () => {
  const [toastState, setToastState] = React.useState<{
    visible: boolean;
    props: Partial<ToastProps>;
  }>({
    visible: false,
    props: {},
  });

  const show = React.useCallback((props: Partial<ToastProps>) => {
    setToastState({
      visible: true,
      props,
    });
  }, []);

  const hide = React.useCallback(() => {
    setToastState(prev => ({
      ...prev,
      visible: false,
    }));
  }, []);

  const ToastComponent = React.useCallback(() => {
    return (
      <Toast
        {...toastState.props}
        visible={toastState.visible}
        onHide={hide}
      />
    );
  }, [toastState, hide]);

  return {
    show,
    hide,
    ToastComponent,
  };
};

// Métodos estáticos para uso global
export const ToastManager = {
  _ref: null as any,

  setRef(ref: any) {
    this._ref = ref;
  },

  show(props: Partial<ToastProps>) {
    if (this._ref) {
      this._ref.show(props);
    }
  },

  hide() {
    if (this._ref) {
      this._ref.hide();
    }
  },

  success(text1: string, text2?: string) {
    this.show({ type: 'success', text1, ...(text2 && { text2 }) });
  },

  error(text1: string, text2?: string) {
    this.show({ type: 'error', text1, ...(text2 && { text2 }) });
  },

  warning(text1: string, text2?: string) {
    this.show({ type: 'warning', text1, ...(text2 && { text2 }) });
  },

  info(text1: string, text2?: string) {
    this.show({ type: 'info', text1, ...(text2 && { text2 }) });
  },
};