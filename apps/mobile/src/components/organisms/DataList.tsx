import React, { useMemo } from 'react';
import {
  FlatList,
  View,
  StyleSheet,
  ViewStyle,
  ListRenderItem,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { semanticColors, spacing, borderRadius, typography, colors } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';

export interface DataListProps<T = any> {
  /** Datos a mostrar en la lista */
  data: T[];
  /** Función para renderizar cada item */
  renderItem: ListRenderItem<T>;
  /** Estado de carga */
  loading?: boolean;
  /** Estado de error */
  error?: string | null;
  /** Función para refrescar los datos */
  onRefresh?: () => void;
  /** Indica si está refrescando */
  refreshing?: boolean;
  /** Función para cargar más datos */
  onEndReached?: () => void;
  /** Umbral para cargar más datos */
  onEndReachedThreshold?: number;
  /** Indica si hay más datos para cargar */
  hasMore?: boolean;
  /** Mensaje cuando no hay datos */
  emptyMessage?: string;
  /** Título de la lista */
  title?: string;
  /** Subtítulo de la lista */
  subtitle?: string;
  /** Estilo personalizado del contenedor */
  style?: ViewStyle;
  /** Altura estimada de cada item para optimización */
  estimatedItemSize?: number;
  /** Función para extraer la key de cada item */
  keyExtractor?: (item: T, index: number) => string;
  /** Separador entre items */
  ItemSeparatorComponent?: React.ComponentType<any> | null;
  /** Componente de header */
  ListHeaderComponent?: React.ComponentType<any> | React.ReactElement | null;
  /** Componente de footer */
  ListFooterComponent?: React.ComponentType<any> | React.ReactElement | null;
  /** Número de items a renderizar inicialmente */
  initialNumToRender?: number;
  /** Tamaño de la ventana de renderizado */
  windowSize?: number;
  /** Máximo número de items a renderizar */
  maxToRenderPerBatch?: number;
  /** Tiempo de debounce para scroll */
  updateCellsBatchingPeriod?: number;
  /** Habilitar virtualización */
  removeClippedSubviews?: boolean;
  /** ID para testing */
  testID?: string;
  /** Estilo adicional para el contenido interno de la lista (FlatList.contentContainerStyle) */
  contentContainerStyle?: ViewStyle;
}

export function DataList<T = any>({
  data,
  renderItem,
  loading = false,
  error = null,
  onRefresh,
  refreshing = false,
  onEndReached,
  onEndReachedThreshold = 0.1,
  hasMore = false,
  emptyMessage = 'No hay datos disponibles',
  title,
  subtitle,
  style,
  estimatedItemSize = 80,
  keyExtractor,
  ItemSeparatorComponent,
  ListHeaderComponent,
  ListFooterComponent,
  initialNumToRender = 10,
  windowSize = 10,
  maxToRenderPerBatch = 5,
  updateCellsBatchingPeriod = 50,
  removeClippedSubviews = true,
  testID,
  contentContainerStyle,
}: DataListProps<T>): React.ReactElement {
  
  // Componente de separador por defecto
  const defaultSeparator = useMemo(() => {
    return () => <View style={styles.separator} />;
  }, []);

  // Componente de header con título y subtítulo
  const headerComponent = useMemo(() => {
    if (!title && !ListHeaderComponent) return null;
    
    const TitleHeader = () => (
      <View style={styles.header}>
        {title && (
          <Text variant="h3" color={semanticColors.textPrimary} style={styles.title}>
            {title}
          </Text>
        )}
        {subtitle && (
          <Text variant="bodySmall" color={semanticColors.textSecondary} style={styles.subtitle}>
            {subtitle}
          </Text>
        )}
      </View>
    );

    if (ListHeaderComponent) {
      return (
        <View>
          <TitleHeader />
          {React.isValidElement(ListHeaderComponent) ? 
            ListHeaderComponent : 
            React.createElement(ListHeaderComponent as React.ComponentType)
          }
        </View>
      );
    }

    return <TitleHeader />;
  }, [title, subtitle, ListHeaderComponent]);

  // Componente de footer con indicador de carga
  const footerComponent = useMemo(() => {
    const LoadingFooter = () => {
      if (!hasMore || !data.length) return null;
      
      return (
        <View style={styles.footer}>
          <ActivityIndicator size="small" color={semanticColors.primary} />
          <Text variant="caption" color={semanticColors.textSecondary} style={styles.loadingText}>
            Cargando más datos...
          </Text>
        </View>
      );
    };

    if (ListFooterComponent) {
      return (
        <View>
          {React.isValidElement(ListFooterComponent) ? 
            ListFooterComponent : 
            React.createElement(ListFooterComponent as React.ComponentType)
          }
          <LoadingFooter />
        </View>
      );
    }

    return <LoadingFooter />;
  }, [hasMore, data.length, ListFooterComponent]);

  // Componente de estado vacío
  const emptyComponent = useMemo(() => {
    if (loading) {
      return (
        <View style={styles.emptyContainer}>
          <ActivityIndicator size="large" color={semanticColors.primary} />
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.emptyText}>
            Cargando datos...
          </Text>
        </View>
      );
    }

    if (error) {
      return (
        <View style={styles.emptyContainer}>
          <Icon name="error" size={48} color={semanticColors.errorText} />
          <Text variant="h4" color={semanticColors.errorText} style={styles.emptyTitle}>
            Error al cargar datos
          </Text>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.emptyText}>
          {error}
        </Text>
        </View>
      );
    }

    return (
      <View style={styles.emptyContainer}>
        <Icon name="search" size={48} color={semanticColors.textSecondary} />
        <Text variant="h4" color={semanticColors.textSecondary} style={styles.emptyTitle}>
          No hay datos disponibles
        </Text>
        <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.emptyText}>
          No se encontraron elementos para mostrar.
        </Text>
      </View>
    );
  }, [loading, error, emptyMessage]);

  // Control de refresh
  const refreshControl = useMemo(() => {
    if (!onRefresh) return undefined;
    
    return (
      <RefreshControl
        refreshing={refreshing}
        onRefresh={onRefresh}
        colors={[semanticColors.primary]}
        tintColor={semanticColors.primary}
      />
    );
  }, [onRefresh, refreshing]);

  return (
    <View style={[styles.container, style]} testID={testID}>
      <FlatList
        data={data}
        renderItem={renderItem}
        // Estado vacío se maneja con ListEmptyComponent
        keyExtractor={keyExtractor || ((item: any, index: number) => 
          item.id?.toString() || index.toString()
        )}
        ItemSeparatorComponent={ItemSeparatorComponent || defaultSeparator}
        ListHeaderComponent={headerComponent}
        ListFooterComponent={footerComponent}
        ListEmptyComponent={emptyComponent}
        refreshControl={refreshControl}
        onEndReached={onEndReached}
        onEndReachedThreshold={onEndReachedThreshold}
        // Optimizaciones de rendimiento
        getItemLayout={estimatedItemSize ? (data, index) => ({
          length: estimatedItemSize,
          offset: estimatedItemSize * index,
          index,
        }) : undefined}
        initialNumToRender={initialNumToRender}
        windowSize={windowSize}
        maxToRenderPerBatch={maxToRenderPerBatch}
        updateCellsBatchingPeriod={updateCellsBatchingPeriod}
        removeClippedSubviews={removeClippedSubviews}
        // Estilos
        contentContainerStyle={[
          data.length === 0 ? styles.emptyContentContainer : styles.contentContainer,
          contentContainerStyle,
        ]}
        showsVerticalScrollIndicator={false}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  contentContainer: {
    paddingHorizontal: spacing.md,
  },
  header: {
    paddingTop: spacing.md,
    paddingBottom: spacing.sm,
  },
  title: {
    fontWeight: typography.fontWeight.bold as any,
    marginBottom: spacing.xs,
  },
  subtitle: {
    lineHeight: 20,
  },
  separator: {
    height: 1,
    backgroundColor: semanticColors.borderMuted,
  },
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.md,
  },
  loadingText: {
    marginLeft: spacing.sm,
  },
  emptyContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xl,
  },
  emptyContentContainer: {
    flexGrow: 1,
  },
  emptyTitle: {
    marginTop: spacing.md,
    marginBottom: spacing.sm,
    textAlign: 'center',
  },
  emptyText: {
    textAlign: 'center',
    lineHeight: 20,
  },
});