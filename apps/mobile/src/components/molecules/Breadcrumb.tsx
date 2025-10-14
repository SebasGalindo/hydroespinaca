import React from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  ViewStyle,
  TextStyle,
  ScrollView,
} from 'react-native';

// Interfaces
export interface BreadcrumbItem {
  id: string;
  label: string;
  path?: string;
  isActive?: boolean;
  disabled?: boolean;
}

export interface BreadcrumbProps {
  items: BreadcrumbItem[];
  onItemPress?: (item: BreadcrumbItem, index: number) => void;
  separator?: string | React.ReactNode;
  maxItems?: number;
  showEllipsis?: boolean;
  variant?: 'default' | 'minimal' | 'outlined' | 'compact';
  size?: 'small' | 'medium' | 'large';
  scrollable?: boolean;
  style?: ViewStyle;
  itemStyle?: ViewStyle;
  textStyle?: TextStyle;
  activeTextStyle?: TextStyle;
  separatorStyle?: TextStyle;
  disabled?: boolean;
  testID?: string;
}

// Hook personalizado para manejar breadcrumbs
export const useBreadcrumb = (initialItems: BreadcrumbItem[] = []) => {
  const [items, setItems] = React.useState<BreadcrumbItem[]>(initialItems);

  const addItem = React.useCallback((item: BreadcrumbItem) => {
    setItems(prev => [...prev, item]);
  }, []);

  const removeItem = React.useCallback((index: number) => {
    setItems(prev => prev.filter((_, i) => i !== index));
  }, []);

  const updateItem = React.useCallback((index: number, updates: Partial<BreadcrumbItem>) => {
    setItems(prev => prev.map((item, i) => i === index ? { ...item, ...updates } : item));
  }, []);

  const setActiveItem = React.useCallback((index: number) => {
    setItems(prev => prev.map((item, i) => ({ ...item, isActive: i === index })));
  }, []);

  const reset = React.useCallback(() => {
    setItems(initialItems);
  }, [initialItems]);

  const navigateToIndex = React.useCallback((index: number) => {
    setItems(prev => prev.slice(0, index + 1));
  }, []);

  return {
    items,
    addItem,
    removeItem,
    updateItem,
    setActiveItem,
    reset,
    navigateToIndex,
    setItems,
  };
};

// Función para truncar breadcrumbs largos
const truncateBreadcrumbs = (
  items: BreadcrumbItem[],
  maxItems: number,
  showEllipsis: boolean
): BreadcrumbItem[] => {
  if (items.length <= maxItems) return items;

  if (showEllipsis && items.length > 0) {
    const firstItem = items[0];
    if (!firstItem) return items.slice(-maxItems);
    const lastItems = items.slice(-(maxItems - 1));
    return [firstItem, { id: 'ellipsis', label: '...', disabled: true }, ...lastItems];
  }

  return items.slice(-maxItems);
};

// Componente Separator
const BreadcrumbSeparator: React.FC<{
  separator?: string | React.ReactNode;
  style?: TextStyle | undefined;
  variant?: string;
}> = ({ separator = '/', style, variant }) => {
  const separatorStyles = [
    styles.separator,
    variant === 'minimal' && styles.separatorMinimal,
    style,
  ];

  if (React.isValidElement(separator)) {
    return separator as React.ReactElement;
  }

  return <Text style={separatorStyles}>{separator}</Text>;
};

// Componente principal Breadcrumb
export const Breadcrumb: React.FC<BreadcrumbProps> = ({
  items,
  onItemPress,
  separator = '/',
  maxItems = 5,
  showEllipsis = true,
  variant = 'default',
  size = 'medium',
  scrollable = false,
  style,
  itemStyle,
  textStyle,
  activeTextStyle,
  separatorStyle,
  disabled = false,
  testID,
}) => {
  const processedItems = React.useMemo(() => {
    return truncateBreadcrumbs(items, maxItems, showEllipsis);
  }, [items, maxItems, showEllipsis]);

  const handleItemPress = React.useCallback((item: BreadcrumbItem, index: number) => {
    if (disabled || item.disabled || item.id === 'ellipsis') return;
    onItemPress?.(item, index);
  }, [disabled, onItemPress]);

  const getContainerVariantStyle = (variant: string): ViewStyle => {
    switch (variant) {
      case 'default': return styles.containerDefault;
      case 'minimal': return styles.containerMinimal;
      case 'outlined': return styles.containerOutlined;
      case 'compact': return styles.containerCompact;
      default: return {};
    }
  };

  const getContainerSizeStyle = (size: string): ViewStyle => {
    switch (size) {
      case 'small': return styles.containerSmall;
      case 'medium': return styles.containerMedium;
      case 'large': return styles.containerLarge;
      default: return {};
    }
  };

  const getItemSizeStyle = (size: string): ViewStyle => {
    switch (size) {
      case 'small': return styles.itemSmall;
      case 'medium': return styles.itemMedium;
      case 'large': return styles.itemLarge;
      default: return {};
    }
  };

  const containerStyles = StyleSheet.flatten([
    styles.container,
    getContainerVariantStyle(variant),
    getContainerSizeStyle(size),
    disabled && styles.containerDisabled,
    style,
  ]);

  const renderBreadcrumbItem = (item: BreadcrumbItem, index: number) => {
    const isLast = index === processedItems.length - 1;
    const isEllipsis = item.id === 'ellipsis';
    const isClickable = !disabled && !item.disabled && !isEllipsis && onItemPress;

    const itemStyles = StyleSheet.flatten([
      styles.item,
      getItemSizeStyle(size),
      item.isActive && styles.itemActive,
      (disabled || item.disabled) && styles.itemDisabled,
      itemStyle,
    ]);

    const textStyles = [
      styles.text,
      styles[`text${size.charAt(0).toUpperCase() + size.slice(1)}` as keyof typeof styles],
      styles[`text${variant.charAt(0).toUpperCase() + variant.slice(1)}` as keyof typeof styles],
      item.isActive && [styles.textActive, activeTextStyle],
      (disabled || item.disabled) && styles.textDisabled,
      textStyle,
    ];

    const ItemComponent = isClickable ? TouchableOpacity : View;

    return (
      <React.Fragment key={item.id}>
        <ItemComponent
          style={itemStyles}
          onPress={isClickable ? () => handleItemPress(item, index) : undefined}
          activeOpacity={isClickable ? 0.7 : 1}
          testID={testID ? `${testID}-item-${index}` : undefined}
        >
          <Text style={textStyles} numberOfLines={1}>
            {item.label}
          </Text>
        </ItemComponent>
        
        {!isLast && (
          <BreadcrumbSeparator
            separator={separator}
            style={separatorStyle}
            variant={variant}
          />
        )}
      </React.Fragment>
    );
  };

  const content = (
    <View style={containerStyles} testID={testID}>
      {processedItems.map(renderBreadcrumbItem)}
    </View>
  );

  if (scrollable) {
    return (
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.scrollContent}
      >
        {content}
      </ScrollView>
    );
  }

  return content;
};

// Componentes predefinidos para casos comunes
export const BreadcrumbNavigation: React.FC<Omit<BreadcrumbProps, 'variant'>> = (props) => (
  <Breadcrumb {...props} variant="default" />
);

export const BreadcrumbMinimal: React.FC<Omit<BreadcrumbProps, 'variant'>> = (props) => (
  <Breadcrumb {...props} variant="minimal" separator=">" />
);

export const BreadcrumbOutlined: React.FC<Omit<BreadcrumbProps, 'variant'>> = (props) => (
  <Breadcrumb {...props} variant="outlined" />
);

export const BreadcrumbCompact: React.FC<Omit<BreadcrumbProps, 'variant' | 'maxItems'>> = (props) => (
  <Breadcrumb {...props} variant="compact" maxItems={3} />
);

export const BreadcrumbScrollable: React.FC<Omit<BreadcrumbProps, 'scrollable'>> = (props) => (
  <Breadcrumb {...props} scrollable={true} />
);

// Manager para múltiples breadcrumbs
export interface BreadcrumbManagerProps {
  breadcrumbs: { [key: string]: BreadcrumbProps };
  activeBreadcrumb?: string;
  onBreadcrumbChange?: (key: string) => void;
}

export const BreadcrumbManager: React.FC<BreadcrumbManagerProps> = ({
  breadcrumbs,
  activeBreadcrumb,
  onBreadcrumbChange,
}) => {
  const [currentBreadcrumb, setCurrentBreadcrumb] = React.useState(
    activeBreadcrumb || Object.keys(breadcrumbs)[0]
  );

  React.useEffect(() => {
    if (activeBreadcrumb) {
      setCurrentBreadcrumb(activeBreadcrumb);
    }
  }, [activeBreadcrumb]);

  const handleBreadcrumbChange = React.useCallback((key: string) => {
    setCurrentBreadcrumb(key);
    onBreadcrumbChange?.(key);
  }, [onBreadcrumbChange]);

  const currentProps = currentBreadcrumb ? breadcrumbs[currentBreadcrumb] : null;
  if (!currentProps) return null;

  return <Breadcrumb {...currentProps} />;
};

// Estilos
const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  containerDefault: {
    backgroundColor: '#f9fafb',
    borderRadius: 8,
  },
  containerMinimal: {
    backgroundColor: 'transparent',
    paddingHorizontal: 0,
  },
  containerOutlined: {
    backgroundColor: 'transparent',
    borderWidth: 1,
    borderColor: '#e5e7eb',
    borderRadius: 8,
  },
  containerCompact: {
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  containerSmall: {
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  containerMedium: {
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  containerLarge: {
    paddingHorizontal: 24,
    paddingVertical: 16,
  },
  containerDisabled: {
    opacity: 0.5,
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  item: {
    paddingHorizontal: 4,
    paddingVertical: 4,
    borderRadius: 4,
  },
  itemSmall: {
    paddingHorizontal: 4,
    paddingVertical: 2,
  },
  itemMedium: {
    paddingHorizontal: 4,
    paddingVertical: 4,
  },
  itemLarge: {
    paddingHorizontal: 8,
    paddingVertical: 8,
  },
  itemActive: {
    backgroundColor: '#dbeafe',
  },
  itemDisabled: {
    opacity: 0.5,
  },
  text: {
    fontSize: 14,
    lineHeight: 20,
    color: '#4b5563',
  },
  textSmall: {
    fontSize: 12,
    lineHeight: 16,
  },
  textMedium: {
    fontSize: 14,
    lineHeight: 20,
  },
  textLarge: {
    fontSize: 16,
    lineHeight: 24,
  },
  textDefault: {
    color: '#4b5563',
  },
  textMinimal: {
    color: '#374151',
  },
  textOutlined: {
    color: '#4b5563',
  },
  textCompact: {
    color: '#4b5563',
  },
  textActive: {
    color: '#2563eb',
    fontWeight: '600',
  },
  textDisabled: {
    color: '#9ca3af',
  },
  separator: {
    fontSize: 14,
    lineHeight: 20,
    color: '#9ca3af',
    marginHorizontal: 4,
  },
  separatorMinimal: {
    color: '#6b7280',
    marginHorizontal: 2,
  },
});

export default Breadcrumb;