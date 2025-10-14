import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing, typography } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Heading } from '../atoms/Heading';
import { Badge } from '../atoms/Badge';
import { Icon } from '../atoms/Icon';
import { 
  EntityStatus,
  IconName 
} from '@hidroespinaca/shared';

export interface EntityHeaderProps {
  /** Título principal de la entidad */
  title: string;
  /** Subtítulo opcional */
  subtitle?: string;
  /** Identificador técnico opcional */
  identifier?: string;
  /** Estado de la entidad */
  status: EntityStatus;
  /** Icono opcional para el tipo de entidad */
  icon?: IconName;
  /** Color del icono */
  iconColor?: string;
  /** Mostrar el identificador */
  showIdentifier?: boolean;
  /** Estilo personalizado */
  style?: any;
}

export function EntityHeader({
  title,
  subtitle,
  identifier,
  status,
  icon,
  iconColor = semanticColors.primary,
  showIdentifier = true,
  style,
}: EntityHeaderProps): React.ReactElement {
  
  const getStatusBadgeProps = () => {
    switch (status) {
      case 'active':
        return {
          variant: 'success' as const,
          label: 'Activa',
        };
      case 'inactive':
        return {
          variant: 'error' as const,
          label: 'Inactiva',
        };
      case 'deprecated':
        return {
          variant: 'warning' as const,
          label: 'Obsoleta',
        };
      default:
        return {
          variant: 'default' as const,
          label: status,
        };
    }
  };

  const statusBadge = getStatusBadgeProps();

  return (
    <View style={[styles.container, style]}>
      {/* Fila superior: Título y Badge de estado */}
      <View style={styles.topRow}>
        <View style={styles.titleContainer}>
          {icon && (
            <View style={[styles.iconContainer, { backgroundColor: iconColor + '20' }]}>
              <Icon name={icon} size={20} color={iconColor} />
            </View>
          )}
          <Heading 
            level={4} 
            color={semanticColors.textPrimary}
            style={styles.title}
          >
            {title}
          </Heading>
        </View>
        <Badge 
          variant={statusBadge.variant} 
          size="lg"
          style={styles.statusBadge}
        >
          {statusBadge.label}
        </Badge>
      </View>

      {/* Subtítulo */}
      {subtitle && (
        <Text 
          variant="bodyLarge" 
          color={semanticColors.textPrimary}
          style={styles.subtitle}
        >
          {subtitle}
        </Text>
      )}

      {/* Identificador técnico */}
      {showIdentifier && identifier && (
        <Text 
          variant="bodySmall" 
          color={semanticColors.textSecondary}
          style={styles.identifier}
        >
          {identifier}
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.md,
  },

  topRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.xs,
  },

  titleContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: spacing.sm,
  },

  iconContainer: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: spacing.sm,
  },

  title: {
    flex: 1,
    fontWeight: typography.fontWeight.bold as any,
  },

  statusBadge: {
    alignSelf: 'flex-start',
  },

  subtitle: {
    fontWeight: typography.fontWeight.semibold as any,
    marginBottom: spacing.xs,
  },

  identifier: {
    fontFamily: 'monospace',
    fontWeight: typography.fontWeight.medium as any,
    opacity: 0.8,
  },
});