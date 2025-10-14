import React, { useState } from 'react';
import { View, StyleSheet, Pressable, Dimensions } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Icon } from '../atoms/Icon';
import { getMainTabs, NavigationItem } from '@hidroespinaca/shared/src/config/navigation';
import { DrawerMenuItem } from '@hidroespinaca/shared/src/config/drawerMenu';
import { Text } from '../atoms/Text';
import { DrawerMenuModal } from './DrawerMenuModal';

export interface BottomTabNavigatorProps {
  activeTab?: string;
  onTabPress?: (tabId: string, href: string) => void;
  testID?: string;
}

export function BottomTabNavigator({
  activeTab: externalActiveTab,
  onTabPress,
  testID,
}: BottomTabNavigatorProps): React.ReactElement {
  const [internalActiveTab, setInternalActiveTab] = useState('info-main');
  const [isDrawerModalVisible, setIsDrawerModalVisible] = useState(false);
  const activeTab = externalActiveTab || internalActiveTab;
  
  // Obtener pestañas desde la configuración compartida
  const tabs: NavigationItem[] = getMainTabs();
  
  const screenWidth = Dimensions.get('window').width;
  const tabWidth = screenWidth / tabs.length;

  const handleTabPress = (tabId: string, href: string) => {
    // Si es la pestaña "Más", abrir el modal en lugar de navegar
    if (tabId === 'mas') {
      setIsDrawerModalVisible(true);
      return;
    }
    
    if (!externalActiveTab) {
      setInternalActiveTab(tabId);
    }
    onTabPress?.(tabId, href);
  };

  const handleDrawerItemPress = (item: DrawerMenuItem) => {
    // Simular navegación desde el drawer
    if (item.href) {
      onTabPress?.('drawer-navigation', item.href);
    }
  };

  const handleTabChange = (tabId: string) => {
    // Cambiar tab usando el sistema interno
    if (!externalActiveTab) {
      setInternalActiveTab(tabId);
    }
    onTabPress?.(tabId, '');
  };

  const handleCloseDrawer = () => {
    setIsDrawerModalVisible(false);
  };

  const renderTab = (tab: NavigationItem) => {
    const isActive = activeTab === tab.id;
    
    return (
      <Pressable
        key={tab.id}
        style={[
          styles.tab,
          { width: tabWidth },
          isActive && styles.activeTab,
        ]}
        onPress={() => handleTabPress(tab.id, tab.href)}
        testID={`${testID}-tab-${tab.id}`}
        accessibilityRole="tab"
        accessibilityState={{ selected: isActive }}
        accessibilityLabel={tab.label}
      >
        <View style={styles.tabContent}>
          <Icon
            name={tab.iconName}
            size={20}
            color={isActive ? semanticColors.primary : semanticColors.textSecondary}
          />
          <Text
            variant="caption"
            color={isActive ? semanticColors.primary : semanticColors.textSecondary}
            style={{
              ...styles.tabLabel,
              textAlign: 'center'
            }}
            numberOfLines={2}
          >
            {tab.label}
          </Text>
        </View>
      </Pressable>
    );
  };

  return (
    <>
      <View style={styles.container} testID={testID}>
        <View style={styles.tabBar}>
          {tabs.map(renderTab)}
        </View>
      </View>
      
      <DrawerMenuModal
        visible={isDrawerModalVisible}
        onClose={handleCloseDrawer}
        activeItemId={activeTab}
        onItemPress={handleDrawerItemPress}
        onTabChange={handleTabChange}
        testID={`${testID}-drawer-modal`}
      />
    </>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.background,
    borderTopWidth: 1,
    borderTopColor: semanticColors.border,
    elevation: 8,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: -2,
    },
    shadowOpacity: 0.1,
    shadowRadius: 4,
  },
  tabBar: {
    flexDirection: 'row',
    paddingBottom: spacing.sm,
    paddingTop: spacing.xs,
    backgroundColor: semanticColors.background,
  },
  tab: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: spacing.xs,
    paddingHorizontal: spacing.xs / 2,
  },
  activeTab: {
    backgroundColor: 'transparent',
  },
  tabContent: {
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48,
  },
  tabIcon: {
    marginBottom: spacing.xs / 2,
  },
  tabLabel: {
    fontSize: 10,
    lineHeight: 12,
  },
});