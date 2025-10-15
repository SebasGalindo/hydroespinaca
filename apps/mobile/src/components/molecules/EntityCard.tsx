import React from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { semanticColors, spacing, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Pressable } from '../atoms/Pressable';
import { Icon } from '../atoms/Icon';
import { Card } from './Card';
import { EntityHeader } from './EntityHeader';
import { InfoField } from './InfoField';
import { 
  BaseEntity, 
  EntityConfig, 
  EntityAction,
  IconName 
} from '@hydroespinaca/shared';

export interface EntityCardProps<T extends BaseEntity> {
  /** Datos de la entidad */
  entity: T;
  /** Configuración del tipo de entidad */
  config: EntityConfig;
  /** Acciones disponibles para esta entidad */
  actions?: EntityAction[];
  /** Función llamada al presionar el card (si es presionable) */
  onPress?: (entity: T) => void;
  /** Icono para el tipo de entidad */
  icon?: IconName;
  /** Color del icono */
  iconColor?: string;
  /** Estilo personalizado */
  style?: ViewStyle;
}

export function EntityCard<T extends BaseEntity>({
  entity,
  config,
  actions = [],
  onPress,
  icon,
  iconColor,
  style,
}: EntityCardProps<T>): React.ReactElement {

  const renderDescription = () => {
    if (!config.showDescription || !entity.description) return null;
    
    return (
      <Text 
        variant="body" 
        color={semanticColors.textSecondary}
        style={styles.description}
      >
        {entity.description}
      </Text>
    );
  };

  const renderFields = () => {
    if (!entity.fields || entity.fields.length === 0) return null;

    return (
      <View style={styles.fieldsContainer}>
        {entity.fields.map((field, index) => (
          <InfoField 
            key={`${entity.id}-field-${index}`}
            field={field}
            style={styles.fieldItem}
          />
        ))}
      </View>
    );
  };

  const renderModifiedDate = () => {
    if (!config.showModifiedDate || !entity.modifiedDate) return null;

    return (
      <View style={styles.modifiedDateContainer}>
        <Icon 
          name="clock" 
          size={14} 
          color={semanticColors.textSecondary} 
          style={styles.clockIcon}
        />
        <Text 
          variant="bodySmall" 
          color={semanticColors.textSecondary}
          style={styles.modifiedDate}
        >
          Modificado: {entity.modifiedDate}
        </Text>
      </View>
    );
  };

  const renderActions = () => {
    if (!actions || actions.length === 0) return null;

    return (
      <View style={styles.actionsContainer}>
        {actions.map((action) => (
          <Pressable
            key={action.id}
            onPress={() => action.onClick()}
            style={StyleSheet.flatten([
              styles.actionButton,
              action.variant === 'danger' && styles.dangerButton,
              action.variant === 'primary' && styles.primaryButton,
            ])}
          >
            {action.icon && (
              <Icon 
                name={action.icon as IconName} 
                size={16} 
                color={getActionColor(action.variant)} 
                style={styles.actionIcon}
              />
            )}
            <Text 
              variant="bodySmall" 
              color={getActionColor(action.variant)}
              style={styles.actionText}
            >
              {action.label}
            </Text>
          </Pressable>
        ))}
      </View>
    );
  };

  const getActionColor = (variant?: string) => {
    switch (variant) {
      case 'danger':
        return semanticColors.errorText;
      case 'primary':
        return semanticColors.primary;
      default:
        return semanticColors.textSecondary;
    }
  };

  return (
    <Card 
      padding="md" 
      elevated={true} 
      style={{...styles.card, ...style}}
    >
      {/* Header con título, subtítulo y estado */}
      <EntityHeader
        title={entity.title}
        status={entity.status}
        {...(entity.subtitle && { subtitle: entity.subtitle })}
        {...(config.showIdentifier && entity.identifier && { identifier: entity.identifier })}
        {...(icon && { icon })}
        {...(iconColor && { iconColor })}
        {...(config.showIdentifier && { showIdentifier: config.showIdentifier })}
      />

      {/* Descripción */}
      {renderDescription()}

      {/* Campos de información */}
      {renderFields()}

      {/* Fecha de modificación */}
      {renderModifiedDate()}

      {/* Botones de acción */}
      {renderActions()}
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    marginVertical: spacing.xs,
  },

  description: {
    marginBottom: spacing.md,
    lineHeight: 20,
  },

  fieldsContainer: {
    marginBottom: spacing.md,
  },

  fieldItem: {
    // InfoField ya tiene sus propios estilos
  },

  modifiedDateContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.md,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.borderMuted,
  },

  clockIcon: {
    marginRight: spacing.xs,
  },

  modifiedDate: {
    fontSize: 12,
  },

  actionsContainer: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: semanticColors.borderMuted,
  },

  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: 6,
    backgroundColor: semanticColors.backgroundSecondary,
    borderWidth: 1,
    borderColor: semanticColors.borderMuted,
  },

  primaryButton: {
    backgroundColor: semanticColors.primary + '10',
    borderColor: semanticColors.primary + '30',
  },

  dangerButton: {
    backgroundColor: semanticColors.errorText + '10',
    borderColor: semanticColors.errorText + '30',
  },

  actionIcon: {
    marginRight: spacing.xs,
  },

  actionText: {
    fontWeight: typography.fontWeight.medium as any,
    fontSize: 12,
  },
});