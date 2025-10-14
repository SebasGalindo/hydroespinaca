import React, { useMemo, useState, useEffect } from 'react';
import { View, StyleSheet, Text, TouchableOpacity, Dimensions } from 'react-native';
import { Button, IconButton } from '../atoms';

export interface PaginationProps {
  /** Página actual (1-indexed) */
  currentPage: number;
  /** Total de páginas */
  totalPages: number;
  /** Callback cuando cambia la página */
  onPageChange: (page: number) => void;
  /** Número de páginas hermanas a mostrar alrededor de la página actual */
  siblingCount?: number;
  /** Número de páginas límite a mostrar al inicio y final */
  boundaryCount?: number;
  /** Si está deshabilitado */
  disabled?: boolean;
  /** Variante del componente */
  variant?: 'default' | 'outlined' | 'minimal';
  /** Tamaño del componente */
  size?: 'small' | 'medium' | 'large';
  /** Mostrar botones de primera/última página */
  showFirstLast?: boolean;
  /** Mostrar información de página */
  showPageInfo?: boolean;
  /** Texto personalizado para la información de página */
  pageInfoText?: (current: number, total: number) => string;
  /** Color personalizado */
  color?: 'primary' | 'secondary' | 'neutral';
}

const DOTS = '...';

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  siblingCount = 1,
  boundaryCount = 1,
  disabled = false,
  variant = 'default',
  size = 'medium',
  showFirstLast = true,
  showPageInfo = false,
  pageInfoText = (current, total) => `Página ${current} de ${total}`,
  color = 'primary',
}) => {
  const [screenWidth, setScreenWidth] = useState(Dimensions.get('window').width);

  useEffect(() => {
    const subscription = Dimensions.addEventListener('change', ({ window }) => {
      setScreenWidth(window.width);
    });

    return () => subscription?.remove();
  }, []);

  // Calcular cuántos botones de página pueden caber
  const maxVisiblePages = useMemo(() => {
    // Ancho estimado de cada elemento:
    // - Botón de navegación: ~48px
    // - Botón de página: ~48px
    // - Gaps y márgenes: ~8px por elemento
    const navigationButtonsWidth = showFirstLast ? 4 * 56 : 2 * 56; // 4 o 2 botones de navegación con margen
    const availableWidth = screenWidth - navigationButtonsWidth - 64; // padding del container más conservador
    const pageButtonWidth = 56; // ancho estimado de cada botón de página con margen
    
    const maxPages = Math.floor(availableWidth / pageButtonWidth);
    
    // Mínimo 3 páginas visibles, máximo 5 para pantallas pequeñas
    return Math.max(3, Math.min(maxPages, screenWidth < 400 ? 5 : 7));
  }, [screenWidth, showFirstLast]);

  const paginationRange = useMemo(() => {
    // Si el número total de páginas es menor que el máximo visible, mostrar todas
    if (maxVisiblePages >= totalPages) {
      return range(1, totalPages);
    }

    // Reservar espacio para dots y primera/última página
    const availableSlots = maxVisiblePages - 2; // Reservar 2 slots para dots/primera/última
    const halfVisible = Math.floor(availableSlots / 2);
    
    let startPage = Math.max(currentPage - halfVisible, 1);
    let endPage = Math.min(startPage + availableSlots - 1, totalPages);

    // Ajustar si estamos cerca del final
    if (endPage === totalPages) {
      startPage = Math.max(totalPages - availableSlots + 1, 1);
    }

    // Ajustar si estamos cerca del inicio
    if (startPage === 1) {
      endPage = Math.min(availableSlots, totalPages);
    }

    const result: (number | string)[] = [];

    // Caso especial: si startPage es 1, no necesitamos dots al inicio
    if (startPage === 1) {
      result.push(...range(1, endPage));
      
      // Agregar dots y última página si es necesario
      if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
          result.push(DOTS);
        }
        result.push(totalPages);
      }
    }
    // Caso especial: si endPage es totalPages, no necesitamos dots al final
    else if (endPage === totalPages) {
      result.push(1);
      if (startPage > 2) {
        result.push(DOTS);
      }
      result.push(...range(startPage, totalPages));
    }
    // Caso general: necesitamos dots en ambos lados
    else {
      result.push(1);
      if (startPage > 2) {
        result.push(DOTS);
      }
      result.push(...range(startPage, endPage));
      if (endPage < totalPages - 1) {
        result.push(DOTS);
      }
      result.push(totalPages);
    }

    return result;
  }, [totalPages, currentPage, maxVisiblePages]);

  const getButtonVariant = (): 'primary' | 'secondary' | 'ghost' | 'outline' => {
    switch (variant) {
      case 'outlined':
        return 'outline';
      case 'minimal':
        return 'ghost';
      default:
        return 'primary';
    }
  };

  const getButtonSize = (): 'sm' | 'md' | 'lg' => {
    switch (size) {
      case 'small':
        return 'sm';
      case 'large':
        return 'lg';
      default:
        return 'md';
    }
  };

  const getColorScheme = () => {
    switch (color) {
      case 'secondary':
        return {
          primary: '#6b7280',
          primaryHover: '#4b5563',
          text: '#374151',
        };
      case 'neutral':
        return {
          primary: '#6b7280',
          primaryHover: '#4b5563',
          text: '#374151',
        };
      default:
        return {
          primary: '#3b82f6',
          primaryHover: '#2563eb',
          text: '#1d4ed8',
        };
    }
  };

  const colorScheme = getColorScheme();

  const handlePageClick = (page: number | string) => {
    if (typeof page === 'number' && page !== currentPage && !disabled) {
      onPageChange(page);
    }
  };

  const handlePrevious = () => {
    if (currentPage > 1 && !disabled) {
      onPageChange(currentPage - 1);
    }
  };

  const handleNext = () => {
    if (currentPage < totalPages && !disabled) {
      onPageChange(currentPage + 1);
    }
  };

  const handleFirst = () => {
    if (currentPage !== 1 && !disabled) {
      onPageChange(1);
    }
  };

  const handleLast = () => {
    if (currentPage !== totalPages && !disabled) {
      onPageChange(totalPages);
    }
  };

  if (totalPages <= 1) {
    return null;
  }

  return (
    <View style={[styles.container, disabled && styles.disabledContainer]}>
      {showPageInfo && (
        <View style={styles.pageInfo}>
          <Text style={[styles.pageInfoText, { color: colorScheme.text }]}>
            {pageInfoText(currentPage, totalPages)}
          </Text>
        </View>
      )}

      <View style={styles.buttonsContainer}>
        {/* Botón Primera página */}
        {showFirstLast && (
          <IconButton
            icon="play-skip-back"
            variant={getButtonVariant()}
            size={getButtonSize()}
            disabled={disabled || currentPage === 1}
            onPress={handleFirst}
            style={StyleSheet.flatten([
              styles.navigationButton,
              size === 'small' && styles.smallButton,
              size === 'large' && styles.largeButton,
            ])}
          />
        )}

        {/* Botón Anterior */}
        <IconButton
          icon="chevron-back"
          variant={getButtonVariant()}
          size={getButtonSize()}
          disabled={disabled || currentPage === 1}
          onPress={handlePrevious}
          style={StyleSheet.flatten([
            styles.navigationButton,
            size === 'small' && styles.smallButton,
            size === 'large' && styles.largeButton,
          ])}
        />

        {/* Números de página */}
        <View style={styles.pagesContainer}>
          {paginationRange.map((pageNumber, index) => {
            if (pageNumber === DOTS) {
              return (
                <View key={`dots-${index}`} style={styles.dotsContainer}>
                  <Text style={[styles.dotsText, { color: colorScheme.text }]}>...</Text>
                </View>
              );
            }

            const isCurrentPage = pageNumber === currentPage;

            return (
              <Button
                key={pageNumber}
                variant={isCurrentPage ? 'primary' : getButtonVariant()}
                size={getButtonSize()}
                disabled={disabled}
                onPress={() => handlePageClick(pageNumber)}
                style={StyleSheet.flatten([
                  styles.pageButton,
                  size === 'small' && styles.smallButton,
                  size === 'large' && styles.largeButton,
                  isCurrentPage && { backgroundColor: colorScheme.primary },
                ])}
              >
                {pageNumber.toString()}
              </Button>
            );
          })}
        </View>

        {/* Botón Siguiente */}
        <IconButton
          icon="chevron-forward"
          variant={getButtonVariant()}
          size={getButtonSize()}
          disabled={disabled || currentPage === totalPages}
          onPress={handleNext}
          style={StyleSheet.flatten([
            styles.navigationButton,
            size === 'small' && styles.smallButton,
            size === 'large' && styles.largeButton,
          ])}
        />

        {/* Botón Última página */}
        {showFirstLast && (
          <IconButton
            icon="play-skip-forward"
            variant={getButtonVariant()}
            size={getButtonSize()}
            disabled={disabled || currentPage === totalPages}
            onPress={handleLast}
            style={StyleSheet.flatten([
              styles.navigationButton,
              size === 'small' && styles.smallButton,
              size === 'large' && styles.largeButton,
            ])}
          />
        )}
      </View>
    </View>
  );
};

// Función auxiliar para generar rangos
const range = (start: number, end: number): number[] => {
  const length = end - start + 1;
  return Array.from({ length }, (_, idx) => idx + start);
};

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 8,
  },
  disabledContainer: {
    opacity: 0.6,
  },
  pageInfo: {
    marginBottom: 4,
  },
  pageInfoText: {
    fontSize: 12,
    fontWeight: '500',
    color: '#6b7280',
  },
  buttonsContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
    alignSelf: 'center',
  },
  navigationButton: {
    minWidth: 32,
    minHeight: 32,
    marginHorizontal: 2,
  },
  smallButton: {
    minWidth: 28,
    minHeight: 28,
  },
  largeButton: {
    minWidth: 36,
    minHeight: 36,
  },
  pagesContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 2,
    flexShrink: 1,
    maxWidth: '70%',
  },
  pageButton: {
    marginHorizontal: 2,
  },
  dotsContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 2,
    marginHorizontal: 2,
  },
  dotsText: {
    fontSize: 12,
    fontWeight: '500',
    color: '#374151',
    textAlign: 'center',
  },
});

export default Pagination;