import React, { useState } from 'react';
import { View, StyleSheet, SafeAreaView } from 'react-native';
import { semanticColors, spacing, colors } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Header } from '../components/organisms/Header';
import { BottomTabNavigator } from '../components/organisms/BottomTabNavigator';

export function MainScreen(): React.ReactElement {
  const [activeTab, setActiveTab] = useState('info-main');

  const getTabContent = () => {
    switch (activeTab) {
      case 'info-main':
        return {
          title: 'Información Principal',
          description: 'Bienvenido al sistema de cultivo hidropónico',
        };
      case 'lecturas':
        return {
          title: 'Lecturas',
          description: 'Monitoreo de sensores y datos del sistema',
        };
      case 'dashboard':
        return {
          title: 'Dashboard',
          description: 'Panel de control y métricas del sistema',
        };
      case 'logica-fuzzy':
        return {
          title: 'Lógica Fuzzy',
          description: 'Sistema de control inteligente',
        };
      case 'crear-variable':
        return {
          title: 'Crear Variable',
          description: 'Configuración de nuevas variables del sistema',
        };
      case 'mas':
        return {
          title: 'Más Opciones',
          description: 'Configuraciones adicionales y opciones',
        };
      default:
        return {
          title: 'Hidroespinaca',
          description: 'Sistema de cultivo hidropónico inteligente',
        };
    }
  };

  const content = getTabContent();

  return (
    <SafeAreaView style={styles.container}>
      {/* Header fijo en la parte superior */}
      <Header />
      
      {/* Contenido principal */}
      <View style={styles.content}>
        <View style={styles.centerContent}>
          <Text variant="h1" color={semanticColors.textPrimary} style={styles.title}>
            {content.title}
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
            {content.description}
          </Text>
        </View>
      </View>
      
      {/* Bottom Tab Navigator fijo en la parte inferior */}
      <BottomTabNavigator
        activeTab={activeTab}
        onTabPress={setActiveTab}
        testID="main-bottom-tab-navigator"
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  content: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  centerContent: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
  },
  title: {
    textAlign: 'center',
    marginBottom: spacing.md,
  },
  description: {
    textAlign: 'center',
    fontSize: 16,
    lineHeight: 24,
  },
});