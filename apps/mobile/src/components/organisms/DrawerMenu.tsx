import React, { useState } from 'react';
import { View, ScrollView, StyleSheet, Pressable } from 'react-native';
import { 
  semanticColors, 
  spacing, 
} from '@hidroespinaca/shared';
import { 
  drawerMenuConfig,
  DrawerMenuItem
} from '@hidroespinaca/shared/src/config/drawerMenu';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';


export interface DrawerMenuProps {
  isVisible: boolean;
  onClose: () => void;
  activeItemId?: string;
  onItemPress: (item: DrawerMenuItem) => void;
  showHeader?: boolean;
  testID?: string;
}

export const DrawerMenu: React.FC<DrawerMenuProps> = ({
  isVisible,
  onClose,
  activeItemId,
  onItemPress,
  showHeader = true,
  testID
}) => {
  const [expandedItems, setExpandedItems] = useState<string[]>([]);

  const toggleExpanded = (itemId: string) => {
    setExpandedItems(prev => 
      prev.includes(itemId) 
        ? prev.filter(id => id !== itemId)
        : [...prev, itemId]
    );
  };

  const isItemActive = (item: DrawerMenuItem): boolean => {
    if (activeItemId === item.id) return true;
    if (item.children) {
      return item.children.some((child: DrawerMenuItem) => child.id === activeItemId);
    }
    return false;
  };

  const renderStatusBadge = (status: 'connected' | 'disconnected') => {
    const isConnected = status === 'connected';
    return (
      <View style={[
        styles.statusBadge,
        { backgroundColor: isConnected ? '#10B981' : '#EF4444' }
      ]}>
        <View style={[
          styles.statusDot,
          { backgroundColor: isConnected ? '#FFFFFF' : '#FFFFFF' }
        ]} />
        <Text variant="bodySmall" style={styles.statusText}>
          {isConnected ? 'Conectado' : 'Desconectado'}
        </Text>
      </View>
    );
  };

  const renderMenuItem = (item: DrawerMenuItem, isChild = false) => {
    const isActive = isItemActive(item);
    const isExpanded = expandedItems.includes(item.id);
    const hasChildren = item.children && item.children.length > 0;

    return (
      <View key={item.id}>
        <Pressable
          style={[
            styles.menuItem,
            isChild && styles.childMenuItem,
            isActive && styles.activeMenuItem
          ]}
          onPress={() => {
            if (hasChildren) {
              toggleExpanded(item.id);
            } else {
              onItemPress(item);
            }
          }}
        >
          <View style={styles.menuItemContent}>
            <Icon
              name={item.iconName}
              size={20}
              color={isActive ? semanticColors.primary : semanticColors.textSecondary}
            />
            <Text
              variant="body"
              style={isActive ? styles.activeMenuItemText : styles.menuItemText}
            >
              {item.label}
            </Text>
          </View>

          <View style={styles.menuItemRight}>
            {item.status && renderStatusBadge(item.status)}
            {hasChildren && (
              <Icon
                name={isExpanded ? 'chevron-forward' : 'chevron-back'}
                size={16}
                color={semanticColors.textSecondary}
              />
            )}
          </View>
        </Pressable>

        {/* Render children if expanded */}
        {hasChildren && isExpanded && (
          <View style={styles.submenu}>
            {item.children!.map((child: DrawerMenuItem) => renderMenuItem(child, true))}
          </View>
        )}
      </View>
    );
  };

  if (!isVisible) return null;

  return (
    <View style={styles.overlay} testID={testID}>
      {/* Backdrop */}
      <Pressable style={styles.backdrop} onPress={onClose} />
      
      {/* Drawer Content */}
      <View style={styles.drawer}>
        {/* Header */}
        {showHeader && (
          <View style={styles.header}>
            <Text variant="h3" style={styles.headerTitle}>
              Todas las opciones
            </Text>
            <Pressable onPress={onClose} style={styles.closeButton}>
              <Icon name="close" size={24} color={semanticColors.textSecondary} />
            </Pressable>
          </View>
        )}

        {/* Menu Items */}
        <ScrollView style={styles.menuContainer} showsVerticalScrollIndicator={false}>
          {drawerMenuConfig.map((item: DrawerMenuItem) => renderMenuItem(item))}
        </ScrollView>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 1000,
  },
  backdrop: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
  },
  drawer: {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    width: 320,
    backgroundColor: semanticColors.backgroundPrimary,
    shadowColor: '#000',
    shadowOffset: {
      width: 2,
      height: 0,
    },
    shadowOpacity: 0.25,
    shadowRadius: 8,
    elevation: 8,
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.lg,
    backgroundColor: semanticColors.backgroundPrimary,
  },
  headerTitle: {
    color: semanticColors.textPrimary,
  },
  closeButton: {
    padding: spacing.xs,
  },
  menuContainer: {
    flex: 1,
    paddingHorizontal: spacing.md,
    paddingTop: spacing.sm,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
    minHeight: 48,
  },
  childMenuItem: {
    paddingLeft: spacing.xl + spacing.md, // Extra indentation for children
    backgroundColor: semanticColors.backgroundSecondary,
  },
  activeMenuItem: {
    backgroundColor: semanticColors.successBg,
  },
  menuItemContent: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  menuItemText: {
    marginLeft: spacing.md,
    color: semanticColors.textPrimary,
    flex: 1,
  },
  activeMenuItemText: {
    marginLeft: spacing.md,
    color: semanticColors.primary,
    fontWeight: '600',
    flex: 1,
  },
  menuItemRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: 12,
    marginLeft: spacing.xs,
  },
  statusDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    marginRight: spacing.xs,
  },
  statusText: {
    color: '#FFFFFF',
    fontSize: 10,
    fontWeight: '600',
  },
  submenu: {
    backgroundColor: semanticColors.backgroundSecondary,
  },
});