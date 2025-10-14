import React from 'react';
import { Modal, View, StyleSheet, Pressable, Dimensions } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { DrawerMenu } from './DrawerMenu';
import { DrawerMenuItem } from '@hidroespinaca/shared/src/config/drawerMenu';

export interface DrawerMenuModalProps {
  visible: boolean;
  onClose: () => void;
  activeItemId?: string;
  onItemPress?: (item: DrawerMenuItem) => void;
  onTabChange?: (tabId: string) => void;
  testID?: string;
}

export function DrawerMenuModal({
  visible,
  onClose,
  activeItemId,
  onItemPress,
  onTabChange,
  testID,
}: DrawerMenuModalProps): React.ReactElement {
  const screenHeight = Dimensions.get('window').height;
  const modalHeight = screenHeight * 0.85; // 85% de la altura de la pantalla

  // Mapeo de IDs del menú a IDs de tabs internos
  const tabMapping: Record<string, string> = {
    // Pantallas principales del bottom tab
    'info-main': 'info-main',
    'lecturas': 'lecturas',
    'dashboard': 'dashboard',
    'logica-fuzzy': 'logica-fuzzy',
    // Pantallas secundarias
    'crear-variable': 'crear-variable',
    'variables': 'lista-variables',
    'sensores': 'lista-sensores', 
    'actuadores': 'lista-actuadores',
    'ajustes': 'ajustes',
    'historial': 'historial',
    'estado-sistema': 'estado-sistema',
  };

  const handleItemPress = (item: DrawerMenuItem) => {
    // Si hay un callback personalizado, ejecutarlo
    onItemPress?.(item);
    
    // Cambiar tab usando el sistema interno si existe un mapeo
    const tabId = tabMapping[item.id];
    if (tabId && onTabChange) {
      onTabChange(tabId);
    }
    
    onClose(); // Cerrar el modal después de navegar
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={onClose}
      testID={testID}
    >
      <View style={styles.container}>
        <DrawerMenu
          isVisible={visible}
          onClose={onClose}
          {...(activeItemId && { activeItemId })}
          onItemPress={handleItemPress}
          showHeader={true}
          testID={`${testID}-drawer-menu`}
        />
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
});