import React, { useEffect } from 'react';
import { View, StyleSheet, SafeAreaView } from 'react-native';
import { semanticColors, spacing, colors, useSensorStore } from '@hidroespinaca/shared';
import { Text } from '../components/atoms';
import { VariablesGrid } from '../components/organisms';
import type { MetricData } from '@hidroespinaca/shared';

export function InfoMainScreen(): React.ReactElement {
  const { 
    currentMetrics, 
    loading, 
    generateMockData 
  } = useSensorStore();

  // Inicializar datos al montar el componente
  useEffect(() => {
    generateMockData();
  }, [generateMockData]);

  // Separar variables por tipo (simulando IA vs Manual)
  const aiVariables: MetricData[] = currentMetrics.filter(metric => 
    ['Temperatura', 'Humedad', 'pH', 'Luz Solar', 'Conductividad'].includes(metric.title)
  );

  // Variables manuales simuladas (usando datos del variableStore si fuera necesario)
  const manualVariables: MetricData[] = [
    {
      title: 'Nivel de Agua',
      value: '45',
      unit: 'cm',
      status: 'optimal',
      trend: 'stable',
      change: '0 cm',
      iconType: 'humidity'
    },
    {
      title: 'Flujo de Agua',
      value: '2.5',
      unit: 'L/min',
      status: 'optimal',
      trend: 'up',
      change: '+0.2 L/min',
      iconType: 'electric'
    }
  ];

  const handleVariablePress = (metric: MetricData) => {
    console.log('Variable presionada:', metric.title);
    // Aquí se podría navegar a una pantalla de detalle
  };

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Text variant="h3" color="textSecondary">
            Cargando información del cultivo...
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text variant="h2" color="textPrimary" style={styles.title}>
          Información General Del Cultivo
        </Text>
      </View>
      
      <View style={styles.content}>
        <VariablesGrid
          aiVariables={aiVariables}
          manualVariables={manualVariables}
          onVariablePress={handleVariablePress}
          showSections={true}
        />
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  header: {
    backgroundColor: colors.hidro.bgLight,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.lg,
  },
  title: {
    textAlign: 'center',
    fontWeight: '700',
    fontSize: 24,
  },
  content: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
  },
});