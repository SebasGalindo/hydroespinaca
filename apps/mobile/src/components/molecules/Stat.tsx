import React from 'react';
import { View, StyleSheet, ViewStyle, Text, TouchableOpacity } from 'react-native';
import { IconName } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

export interface StatProps {
  /** Valor principal de la estadística */
  value: string | number;
  /** Etiqueta descriptiva de la estadística */
  label: string;
  /** Descripción adicional opcional */
  description?: string;
  /** Icono opcional para la estadística */
  icon?: IconName;
  /** Color del icono */
  iconColor?: string;
  /** Color de fondo del icono */
  iconBackgroundColor?: string;
  /** Información de tendencia */
  trend?: {
    /** Valor de la tendencia (ej: "+5%", "-2.3%") */
    value: string;
    /** Dirección de la tendencia: 1 para positiva, -1 para negativa, 0 para neutral */
    direction: 1 | -1 | 0;
    /** Descripción de la tendencia */
    description?: string;
  };
  /** Variante del componente */
  variant?: 'default' | 'card' | 'minimal';
  /** Tamaño del componente */
  size?: 'small' | 'medium' | 'large';
  /** Color del valor principal */
  valueColor?: string;
  /** Color de la etiqueta */
  labelColor?: string;
  /** Estilo personalizado del contenedor */
  style?: ViewStyle;
  /** Función callback al presionar */
  onPress?: () => void;
  /** Estado de carga */
  loading?: boolean;
}

export const Stat: React.FC<StatProps> = ({
  value,
  label,
  description,
  icon,
  iconColor = '#6366F1',
  iconBackgroundColor = '#EEF2FF',
  trend,
  variant = 'default',
  size = 'medium',
  valueColor,
  labelColor,
  style,
  onPress,
  loading = false,
}) => {
  const getSizeStyles = () => {
    switch (size) {
      case 'small':
        return {
          valueSize: 'lg' as const,
          labelSize: 'sm' as const,
          trendSize: 'xs' as const,
          iconSize: 20,
          padding: 12,
        };
      case 'large':
        return {
          valueSize: '3xl' as const,
          labelSize: 'lg' as const,
          trendSize: 'sm' as const,
          iconSize: 32,
          padding: 24,
        };
      default:
        return {
          valueSize: '2xl' as const,
          labelSize: 'base' as const,
          trendSize: 'sm' as const,
          iconSize: 24,
          padding: 16,
        };
    }
  };

  const getTrendColor = () => {
    if (!trend) return '#6B7280';
    switch (trend.direction) {
      case 1:
        return '#10B981'; // Verde para positivo
      case -1:
        return '#EF4444'; // Rojo para negativo
      default:
        return '#6B7280'; // Gris para neutral
    }
  };

  const getTrendIcon = (): IconName | null => {
    if (!trend) return null;
    switch (trend.direction) {
      case 1:
        return 'arrow-right'; // Usando arrow-right para tendencia positiva
      case -1:
        return 'arrow-left'; // Usando arrow-left para tendencia negativa
      default:
        return 'minus';
    }
  };

  const sizeStyles = getSizeStyles();

  const getValueFontSize = () => {
    switch (size) {
      case 'small': return 24;
      case 'large': return 48;
      default: return 32;
    }
  };

  const getLabelFontSize = () => {
    switch (size) {
      case 'small': return 12;
      case 'large': return 18;
      default: return 14;
    }
  };

  const renderContent = () => (
    <View style={[styles.container, { padding: sizeStyles.padding }, style]}>
      {/* Header con icono */}
      {icon && (
        <View style={styles.header}>
          <View style={[
            styles.iconContainer,
            {
              backgroundColor: iconBackgroundColor,
              width: sizeStyles.iconSize + 8,
              height: sizeStyles.iconSize + 8,
            }
          ]}>
            <Icon
              name={icon}
              size={sizeStyles.iconSize}
              color={iconColor}
            />
          </View>
        </View>
      )}

      {/* Valor principal */}
      <View style={styles.valueContainer}>
        <Text
          style={[
            styles.value,
            {
              fontSize: getValueFontSize(),
              fontWeight: 'bold',
              color: valueColor || '#111827',
            }
          ]}
        >
          {loading ? '---' : value}
        </Text>
      </View>

      {/* Etiqueta */}
      <Text
        style={[
          styles.label,
          {
            fontSize: getLabelFontSize(),
            color: labelColor || '#6B7280',
          }
        ]}
      >
        {label}
      </Text>

      {/* Descripción */}
      {description && (
        <Text
          style={[
            styles.description,
            {
              fontSize: 12,
              color: '#9CA3AF',
            }
          ]}
        >
          {description}
        </Text>
      )}

      {/* Tendencia */}
      {trend && !loading && (
        <View style={styles.trendContainer}>
          {getTrendIcon() && (
            <Icon
              name={getTrendIcon()!}
              size={14}
              color={getTrendColor()}
              style={styles.trendIcon}
            />
          )}
          <Text
            style={[
              styles.trendValue,
              {
                fontSize: 12,
                color: getTrendColor(),
                fontWeight: '500',
              }
            ]}
          >
            {trend.value}
          </Text>
          {trend.description && (
            <Text
              style={[
                styles.trendDescription,
                {
                  fontSize: 12,
                  color: '#6B7280',
                }
              ]}
            >
              {trend.description}
            </Text>
          )}
        </View>
      )}
    </View>
  );

  if (variant === 'card') {
    return (
      <TouchableOpacity
        style={[{ backgroundColor: '#ffffff', borderRadius: 8, padding: 16 }, style]}
        onPress={onPress}
        disabled={loading}
      >
        {renderContent()}
      </TouchableOpacity>
    );
  }

  if (variant === 'minimal') {
    return (
      <View style={[styles.minimalContainer, style]}>
        <Text
          style={{
            fontSize: getValueFontSize(),
            fontWeight: 'bold',
            color: valueColor || '#111827',
          }}
        >
          {loading ? '---' : value}
        </Text>
        <Text
          style={{
            fontSize: getLabelFontSize(),
            color: labelColor || '#6B7280',
          }}
        >
          {label}
        </Text>
      </View>
    );
  }

  return renderContent();
};

const styles = StyleSheet.create({
  container: {
    alignItems: 'flex-start',
  },
  header: {
    marginBottom: 12,
  },
  iconContainer: {
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  valueContainer: {
    marginBottom: 4,
  },
  value: {
    lineHeight: undefined,
  },
  label: {
    marginBottom: 4,
  },
  description: {
    marginBottom: 8,
  },
  trendContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 8,
  },
  trendIcon: {
    marginRight: 4,
  },
  trendValue: {
    marginRight: 4,
  },
  trendDescription: {
    marginLeft: 4,
  },
  minimalContainer: {
    alignItems: 'center',
  },
});