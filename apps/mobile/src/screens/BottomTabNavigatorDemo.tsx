import React, { useState } from 'react';
import { View, StyleSheet, SafeAreaView } from 'react-native';
import { semanticColors, spacing, borderRadius } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { BottomTabNavigator } from '../components/organisms/BottomTabNavigator';
import { Header } from '../components/organisms/Header';
import { BottomSheetDemo } from '../components/organisms/BottomSheetDemo';
import { DataList } from '../components/organisms/DataList';
import { ReadingsTable } from '../components/organisms/ReadingsTable';
import { LoginForm } from '../components/organisms/LoginForm';
import { useReadingsStore } from '@hydroespinaca/shared';

export function BottomTabNavigatorDemo(): React.ReactElement {
  const [activeTab, setActiveTab] = useState('info-main');
  const [activeMode, setActiveMode] = useState<'summary' | 'individual'>('summary');
  
  const {
    sensorSummary,
    individualReadings,
    initializeReadings
  } = useReadingsStore();

  // Inicializar datos al montar el componente
  React.useEffect(() => {
    initializeReadings();
  }, [initializeReadings]);

  const getTabContent = () => {
    switch (activeTab) {
      case 'info-main':
        return {
          title: 'Lecturas y Datos del Sistema',
          subtitle: 'DataList & ReadingsTable',
          description: 'Visualización de datos del cultivo hidropónico con componentes avanzados',
        };
      case 'lecturas':
        return {
          title: 'Login Form',
          subtitle: 'Formulario de Inicio de Sesión',
          description: 'Ejemplo del componente LoginForm con validaciones y diseño responsive',
        };
      case 'dashboard':
        return {
          title: 'Dashboard',
          subtitle: 'Panel de Control',
          description: 'Gráficos y métricas del sistema hidropónico',
        };
      case 'logica-fuzzy':
        return {
          title: 'Lógica Fuzzy',
          subtitle: 'Sistema de Control Inteligente',
          description: 'Configuración y monitoreo del sistema de lógica difusa',
        };
      case 'crear-variable':
        return {
          title: 'Crear Variable',
          subtitle: 'Nueva Variable de Control',
          description: 'Formulario para crear nuevas variables del sistema',
        };
      case 'mas':
        return {
          title: 'Más Opciones',
          subtitle: 'Menú Adicional',
          description: 'Configuraciones adicionales y opciones del sistema',
        };
      default:
        return {
          title: 'Selecciona una opción',
          subtitle: '',
          description: '',
        };
    }
  };

  const content = getTabContent();

  const handleRefresh = () => {
    initializeReadings();
  };

  const handleSummaryItemPress = (item: any) => {
    console.log('Summary item pressed:', item);
  };

  const handleIndividualItemPress = (item: any) => {
    console.log('Individual item pressed:', item);
  };

  return (
    <SafeAreaView style={styles.container}>
      <Header />
      <View style={styles.content}>
        <View style={styles.header}>
          <Text variant="h1" color={semanticColors.textPrimary} style={styles.title}>
            {content.title}
          </Text>
          {content.subtitle && (
            <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
              {content.subtitle}
            </Text>
          )}
        </View>
        
        <View style={styles.body}>
          {activeTab === 'crear-variable' ? (
            <BottomSheetDemo />
          ) : activeTab === 'lecturas' ? (
            <View style={styles.loginFormContainer}>
              <Text variant="body" color={semanticColors.textPrimary} style={styles.description}>
                {content.description}
              </Text>
              
              <LoginForm
                onLoginSuccess={() => {
                  console.log('Login exitoso desde demo');
                  // Aquí se podría navegar a otra pantalla
                }}
                onForgotPassword={() => {
                  console.log('Olvidé mi contraseña desde demo');
                }}
                onSignUp={() => {
                  console.log('Registrarse desde demo');
                }}
                testID="demo-login-form"
              />
            </View>
          ) : activeTab === 'info-main' ? (
            <View style={styles.infoMainContainer}>
              <Text variant="body" color={semanticColors.textPrimary} style={styles.description}>
                {content.description}
              </Text>
              
              {/* Selector de modo */}
              <View style={styles.modeSelector}>
                <Button
                  variant={activeMode === 'summary' ? 'primary' : 'outline'}
                  onPress={() => setActiveMode('summary')}
                  style={styles.modeButton}
                >
                  Resumen de Sensores
                </Button>
                <Button
                  variant={activeMode === 'individual' ? 'primary' : 'outline'}
                  onPress={() => setActiveMode('individual')}
                  style={styles.modeButton}
                >
                  Lecturas Individuales
                </Button>
              </View>

              {/* ReadingsTable con datos reales */}
              <View style={styles.readingsTableContainer}>
                <ReadingsTable
                  mode={activeMode}
                  summaryData={sensorSummary}
                  individualData={individualReadings}
                  loading={false}
                  error={null}
                  onRefresh={handleRefresh}
                  refreshing={false}
                  onSummaryItemPress={handleSummaryItemPress}
                  onIndividualItemPress={handleIndividualItemPress}
                  showSensorIcons={false}
                  dateFormat="short"
                  emptyMessage={
                    activeMode === 'summary' 
                      ? 'No hay datos de resumen disponibles' 
                      : 'No hay lecturas individuales disponibles'
                  }
                />
              </View>

              {/* Ejemplo de DataList básico */}
              <View style={styles.dataListSection}>
                <Text variant="h4" style={styles.dataListTitle}>
                  DataList Básico (Ejemplo)
                </Text>
                <View style={styles.dataListContainer}>
                  <DataList
                    data={[
                      { id: '1', title: 'Item 1', description: 'Descripción del item 1' },
                      { id: '2', title: 'Item 2', description: 'Descripción del item 2' },
                      { id: '3', title: 'Item 3', description: 'Descripción del item 3' },
                    ]}
                    renderItem={({ item }) => (
                      <View style={styles.dataListItem}>
                        <Text variant="body" color={semanticColors.textPrimary}>
                          {item.title}
                        </Text>
                        <Text variant="caption" color={semanticColors.textSecondary}>
                          {item.description}
                        </Text>
                      </View>
                    )}
                    keyExtractor={(item) => item.id}
                  />
                </View>
              </View>
            </View>
          ) : (
            <>
              <Text variant="body" color={semanticColors.textPrimary} style={styles.description}>
                {content.description}
              </Text>
              
              <View style={styles.activeTabInfo}>
                <Text variant="caption" color={semanticColors.textSecondary}>
                  Tab activo: {activeTab}
                </Text>
              </View>
            </>
          )}
        </View>
      </View>
      
      <BottomTabNavigator
        activeTab={activeTab}
        onTabPress={setActiveTab}
        testID="bottom-tab-navigator-demo"
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  content: {
    flex: 1,
    padding: spacing.md,
  },
  header: {
    marginBottom: spacing.lg,
    paddingTop: spacing.md,
  },
  title: {
    marginBottom: spacing.xs,
    textAlign: 'center',
  },
  subtitle: {
    textAlign: 'center',
    fontStyle: 'italic',
  },
  body: {
    flex: 1,
  },
  infoMainContainer: {
    flex: 1,
    width: '100%',
  },
  description: {
    textAlign: 'center',
    marginBottom: spacing.lg,
    paddingHorizontal: spacing.sm,
    fontSize: 14,
    lineHeight: 20,
  },
  activeTabInfo: {
    padding: spacing.md,
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: semanticColors.border,
    alignSelf: 'center',
  },
  modeSelector: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.lg,
    paddingHorizontal: spacing.xs,
  },
  modeButton: {
    flex: 1,
  },
  readingsTableContainer: {
    flex: 1,
    minHeight: 350,
    maxHeight: 450,
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: borderRadius.md,
    marginBottom: spacing.lg,
    marginHorizontal: spacing.xs,
    padding: spacing.sm,
  },
  dataListSection: {
    flex: 0,
    marginBottom: spacing.md,
  },
  dataListTitle: {
    marginBottom: spacing.sm,
    paddingHorizontal: spacing.md,
    textAlign: 'left',
  },
  dataListContainer: {
    minHeight: 180,
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: borderRadius.md,
    marginHorizontal: spacing.xs,
    padding: spacing.sm,
  },
  dataListItem: {
    padding: spacing.md,
    backgroundColor: semanticColors.background,
    marginBottom: spacing.xs,
    borderRadius: borderRadius.sm,
    borderWidth: 1,
    borderColor: semanticColors.border,
  },
  loginFormContainer: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: spacing.sm,
  },
});