import React, { useMemo } from 'react';
import {
  View,
  StyleSheet,
  ListRenderItem,
} from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Heading } from '../atoms/Heading';
import { Pressable } from '../atoms/Pressable';
import { Icon } from '../atoms/Icon';
import { DataList } from './DataList';
import { EntityCard } from '../molecules/EntityCard';
import { 
  BaseEntity, 
  CrudListProps, 
  EntityAction,
  PaginationProps,
  IconName 
} from '@hydroespinaca/shared';

// Mapeo de tipos de entidades a iconos
const ENTITY_ICONS: Record<string, IconName> = {
  variable: 'settings',
  sensor: 'power',
  actuator: 'power',
  custom: 'settings',
};

// Mapeo de tipos de entidades a colores
const ENTITY_COLORS: Record<string, string> = {
  variable: '#4ECDC4',
  sensor: '#FF6B6B',
  actuator: '#FFE66D',
  custom: semanticColors.primary,
};

export function CrudList<T extends BaseEntity>({
  title,
  subtitle,
  data,
  config,
  pagination,
  loading = false,
  error,
  emptyMessage,
  onRefresh,
  refreshing = false,
  getActions,
  onItemPress,
}: CrudListProps<T>): React.ReactElement {

  // Función para obtener el icono del tipo de entidad
  const getEntityIcon = (entityType: string): string => {
    return ENTITY_ICONS[entityType] || ENTITY_ICONS.custom || 'folder';
  };

  // Función para obtener el color del tipo de entidad
  const getEntityColor = (entityType: string): string => {
    return ENTITY_COLORS[entityType] || ENTITY_COLORS.custom || '#6B7280';
  };

  // Renderizador de items
  const renderItem: ListRenderItem<T> = ({ item }) => {
    const actions = getActions ? getActions(item) : config.defaultActions || [];
    const entityIcon = getEntityIcon(config.entityType);
    const entityColor = getEntityColor(config.entityType);

    return (
      <EntityCard
        entity={item}
        config={config}
        actions={actions}
        icon={entityIcon as IconName}
        iconColor={entityColor}
        {...(onItemPress && { onPress: () => onItemPress(item) })}
      />
    );
  };

  // Componente de paginación
  const renderPagination = () => {
    if (!pagination || pagination.totalPages <= 1) return null;

    return (
      <View style={styles.paginationContainer}>
        <View style={styles.paginationInfo}>
          <Text variant="bodySmall" color={semanticColors.textSecondary}>
            Página {pagination.currentPage} de {pagination.totalPages}
          </Text>
          <Text variant="bodySmall" color={semanticColors.textSecondary}>
            {pagination.totalItems} elementos total
          </Text>
        </View>
        
        <View style={styles.paginationControls}>
          <Pressable
            onPress={() => pagination.onPageChange(pagination.currentPage - 1)}
            disabled={pagination.currentPage <= 1 || pagination.loading}
            style={StyleSheet.flatten([
              styles.paginationButton,
              (pagination.currentPage <= 1 || pagination.loading) && styles.disabledButton
            ])}
          >
            <Icon 
              name="chevron-back" 
              size={20} 
              color={
                (pagination.currentPage <= 1 || pagination.loading) 
                  ? semanticColors.textMuted 
                  : semanticColors.primary
              } 
            />
          </Pressable>

          <Text variant="body" color={semanticColors.textPrimary} style={styles.pageNumber}>
            {pagination.currentPage}
          </Text>

          <Pressable
            onPress={() => pagination.onPageChange(pagination.currentPage + 1)}
            disabled={pagination.currentPage >= pagination.totalPages || pagination.loading}
            style={StyleSheet.flatten([
              styles.paginationButton,
              (pagination.currentPage >= pagination.totalPages || pagination.loading) && styles.disabledButton
            ])}
          >
            <Icon 
              name="chevron-forward" 
              size={20} 
              color={
                (pagination.currentPage >= pagination.totalPages || pagination.loading) 
                  ? semanticColors.textMuted 
                  : semanticColors.primary
              } 
            />
          </Pressable>
        </View>
      </View>
    );
  };

  // Título dinámico
  const listTitle = useMemo(() => {
    if (title) return title;
    
    const entityTypeNames: Record<string, string> = {
      variable: 'Variables',
      sensor: 'Sensores',
      actuator: 'Actuadores',
      custom: 'Elementos',
    };
    
    return entityTypeNames[config.entityType] || 'Lista';
  }, [title, config.entityType]);

  // Mensaje vacío dinámico
  const dynamicEmptyMessage = useMemo(() => {
    if (emptyMessage) return emptyMessage;
    
    const entityTypeNames = {
      variable: 'variables',
      sensor: 'sensores',
      actuator: 'actuadores',
      custom: 'elementos',
    };
    
    const entityName = entityTypeNames[config.entityType] || 'elementos';
    return `No hay ${entityName} disponibles`;
  }, [emptyMessage, config.entityType]);

  return (
    <View style={styles.container}>
      <DataList<T>
        data={data}
        renderItem={renderItem}
        title={listTitle}
        loading={loading}
        emptyMessage={dynamicEmptyMessage}
        refreshing={refreshing}
        estimatedItemSize={200}
        keyExtractor={(item: T, index: number) => 
          `${config.entityType}-${item.id}-${index}`
        }
        {...(subtitle && { subtitle })}
        {...(error && { error })}
        {...(onRefresh && { onRefresh })}
        {...(renderPagination() && { ListFooterComponent: renderPagination() })}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },

  paginationContainer: {
    padding: spacing.md,
    backgroundColor: semanticColors.backgroundPrimary,
    borderTopWidth: 1,
    borderTopColor: semanticColors.borderMuted,
  },

  paginationInfo: {
    alignItems: 'center',
    marginBottom: spacing.sm,
  },

  paginationControls: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.md,
  },

  paginationButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: semanticColors.backgroundSecondary,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: semanticColors.borderMuted,
  },

  disabledButton: {
    backgroundColor: semanticColors.backgroundMuted,
    borderColor: semanticColors.borderDisabled,
  },

  pageNumber: {
    minWidth: 40,
    textAlign: 'center',
    fontWeight: '600',
  },
});