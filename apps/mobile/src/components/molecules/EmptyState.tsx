import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  Image,
  ImageSourcePropType,
  ViewStyle,
  TextStyle,
  ImageStyle,
  TouchableOpacity,
} from 'react-native';
import { IconName } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

export interface EmptyStateProps {
  /** Título principal */
  title?: string;
  /** Descripción o subtítulo */
  description?: string;
  /** Fuente de la imagen */
  imageSource?: ImageSourcePropType;
  /** URL de imagen remota */
  imageUri?: string;
  /** Nombre del icono en lugar de imagen */
  iconName?: IconName;
  /** Tamaño del icono */
  iconSize?: number;
  /** Color del icono */
  iconColor?: string;
  /** Si mostrar botón */
  enableButton?: boolean;
  /** Texto del botón */
  buttonText?: string;
  /** Callback del botón */
  onButtonPress?: () => void;
  /** Variante de estilo */
  variant?: 'default' | 'minimal' | 'illustration';
  /** Tamaño del componente */
  size?: 'small' | 'medium' | 'large';
  /** Estilo del contenedor principal */
  style?: ViewStyle;
  /** Estilo del contenedor de contenido */
  contentStyle?: ViewStyle;
  /** Estilo de la imagen */
  imageStyle?: ImageStyle;
  /** Estilo del título */
  titleStyle?: TextStyle;
  /** Estilo de la descripción */
  descriptionStyle?: TextStyle;
  /** Estilo del botón */
  buttonStyle?: ViewStyle;
  /** Estilo del texto del botón */
  buttonTextStyle?: TextStyle;
  /** Componentes hijos personalizados */
  children?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title = 'No hay datos',
  description = 'No se encontraron elementos para mostrar',
  imageSource,
  imageUri,
  iconName = 'inbox',
  iconSize = 64,
  iconColor,
  enableButton = false,
  buttonText = 'Reintentar',
  onButtonPress,
  variant = 'default',
  size = 'medium',
  style,
  contentStyle,
  imageStyle,
  titleStyle,
  descriptionStyle,
  buttonStyle,
  buttonTextStyle,
  children,
}) => {
  const getSizeStyles = () => {
    const sizes = {
      small: {
        container: { paddingVertical: 24, paddingHorizontal: 16 },
        iconSize: 48,
        titleSize: 16,
        descriptionSize: 14,
        spacing: 12,
      },
      medium: {
        container: { paddingVertical: 40, paddingHorizontal: 24 },
        iconSize: 64,
        titleSize: 18,
        descriptionSize: 16,
        spacing: 16,
      },
      large: {
        container: { paddingVertical: 56, paddingHorizontal: 32 },
        iconSize: 80,
        titleSize: 20,
        descriptionSize: 18,
        spacing: 20,
      },
    };

    return sizes[size];
  };

  const getVariantStyles = () => {
    const variants = {
      default: {
        backgroundColor: '#f9fafb',
        borderRadius: 12,
        borderWidth: 1,
        borderColor: '#e5e7eb',
      },
      minimal: {
        backgroundColor: 'transparent',
        borderRadius: 0,
        borderWidth: 0,
        borderColor: 'transparent',
      },
      illustration: {
        backgroundColor: '#ffffff',
        borderRadius: 16,
        borderWidth: 0,
        borderColor: 'transparent',
        shadowColor: '#000000',
        shadowOffset: {
          width: 0,
          height: 2,
        },
        shadowOpacity: 0.1,
        shadowRadius: 8,
        elevation: 4,
      },
    };

    return variants[variant];
  };

  const sizeStyles = getSizeStyles();
  const variantStyles = getVariantStyles();
  const finalIconSize = iconSize || sizeStyles.iconSize;
  const finalIconColor = iconColor || '#9ca3af';

  const renderImage = () => {
    if (imageSource || imageUri) {
      return (
        <Image
          source={imageSource || { uri: imageUri }}
          style={[
            styles.image,
            {
              width: finalIconSize * 1.5,
              height: finalIconSize * 1.5,
            },
            imageStyle,
          ]}
          resizeMode="contain"
        />
      );
    }

    if (iconName) {
      return (
        <View style={[styles.iconContainer, { marginBottom: sizeStyles.spacing }]}>
          <Icon
            name={iconName as IconName}
            size={finalIconSize}
            color={finalIconColor}
          />
        </View>
      );
    }

    return null;
  };

  return (
    <View
      style={[
        styles.container,
        variantStyles,
        sizeStyles.container,
        style,
      ]}
    >
      <View style={[styles.content, contentStyle]}>
        {renderImage()}

        {title && (
          <Text
            style={[
              styles.title,
              {
                fontSize: sizeStyles.titleSize,
                marginBottom: sizeStyles.spacing / 2,
              },
              titleStyle,
            ]}
          >
            {title}
          </Text>
        )}

        {description && (
          <Text
            style={[
              styles.description,
              {
                fontSize: sizeStyles.descriptionSize,
                marginBottom: sizeStyles.spacing,
              },
              descriptionStyle,
            ]}
          >
            {description}
          </Text>
        )}

        {children}

        {enableButton && buttonText && onButtonPress && (
          <View style={[styles.buttonContainer, { marginTop: sizeStyles.spacing }]}>
            <TouchableOpacity
              onPress={onButtonPress}
              style={[styles.button, buttonStyle]}
            >
              <Text style={[styles.buttonText, buttonTextStyle]}>
                {buttonText}
              </Text>
            </TouchableOpacity>
          </View>
        )}
      </View>
    </View>
  );
};

// Componentes predefinidos para casos comunes
export const EmptyStateNoData: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="settings"
    title="No hay datos"
    description="No se encontraron elementos para mostrar"
    {...props}
  />
);

export const EmptyStateNoResults: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="search"
    title="Sin resultados"
    description="No se encontraron resultados para tu búsqueda"
    enableButton
    buttonText="Limpiar filtros"
    {...props}
  />
);

export const EmptyStateNoConnection: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="wifi"
    title="Sin conexión"
    description="Verifica tu conexión a internet e intenta de nuevo"
    enableButton
    buttonText="Reintentar"
    variant="illustration"
    {...props}
  />
);

export const EmptyStateError: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="warning"
    title="Algo salió mal"
    description="Ocurrió un error inesperado. Intenta de nuevo más tarde"
    enableButton
    buttonText="Reintentar"
    iconColor="#ef4444"
    {...props}
  />
);

export const EmptyStateLoading: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="refresh"
    title="Cargando..."
    description="Estamos preparando todo para ti"
    variant="minimal"
    {...props}
  />
);

export const EmptyStateComingSoon: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="clock"
    title="Próximamente"
    description="Esta funcionalidad estará disponible muy pronto"
    iconColor="#3b82f6"
    variant="illustration"
    {...props}
  />
);

export const EmptyStatePermissions: React.FC<Partial<EmptyStateProps>> = (props) => (
  <EmptyState
    iconName="lock"
    title="Permisos requeridos"
    description="Necesitas otorgar permisos para acceder a esta funcionalidad"
    enableButton
    buttonText="Configurar"
    iconColor="#d97706"
    {...props}
  />
);

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: 200,
  },
  content: {
    alignItems: 'center',
    justifyContent: 'center',
    maxWidth: 300,
  },
  iconContainer: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  image: {
    marginBottom: 16,
  },
  title: {
    fontWeight: '600',
    color: '#111827',
    textAlign: 'center',
    lineHeight: 24,
  },
  description: {
    fontWeight: '400',
    color: '#6b7280',
    textAlign: 'center',
    lineHeight: 22,
  },
  buttonContainer: {
    alignItems: 'center',
    minWidth: 120,
  },
  button: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#d1d5db',
    backgroundColor: '#ffffff',
  },
  buttonText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#374151',
    textAlign: 'center',
  },
});