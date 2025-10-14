import React from 'react';
import { View, StyleSheet, ScrollView, SafeAreaView } from 'react-native';
import { semanticColors, spacing, borderRadius, colors } from '@hidroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Header } from '../components/organisms/Header';
import { EntityCard } from '../components/molecules/EntityCard';
import { InfoField } from '../components/molecules/InfoField';
import { CrudList } from '../components/organisms/CrudList';
import { CrudListExample } from '../examples/CrudListExample';
import type { Variable, EntityConfig, EntityAction, IconName } from '@hidroespinaca/shared';

export function ComponentsExampleScreen(): React.ReactElement {
  // Datos de ejemplo para EntityCard
  const exampleVariable: Variable = {
    id: '1',
    title: 'Temperatura Ambiente',
    subtitle: 'Sensor de temperatura del invernadero',
    identifier: 'temp-ambiente',
    description: 'Monitoreo de la temperatura ambiente del cultivo hidropónico',
    unit: '°C',
    type: 'input',
    dataType: 'numeric',
    minValue: 15,
    maxValue: 35,
    isRequired: true,
    category: 'environmental',
    status: 'active',
    modifiedDate: '2024-01-20',
    fields: [
      { key: 'type', label: 'TIPO', value: 'input', type: 'text' },
      { key: 'category', label: 'CATEGORÍA', value: 'environmental', type: 'text' },
      { key: 'dataType', label: 'TIPO DE DATO', value: 'numeric', type: 'text' },
      { key: 'range', label: 'RANGO', value: '15-35°C', type: 'text' },
      { key: 'status', label: 'ESTADO', value: 'Activo', type: 'badge', variant: 'success' },
    ],
  };

  const entityConfig: EntityConfig = {
    entityType: 'variable',
    showDescription: true,
    showModifiedDate: true,
  };

  const entityActions: EntityAction[] = [
    {
      id: 'edit',
      label: 'Editar',
      variant: 'primary',
      icon: 'settings',
      onClick: () => console.log('Editar variable'),
    },
    {
      id: 'delete',
      label: 'Eliminar',
      variant: 'danger',
      icon: 'close',
      onClick: () => console.log('Eliminar variable'),
    },
  ];

  // Datos de ejemplo para InfoField
  const exampleFields = [
    { key: 'temp', label: 'TEMPERATURA', value: '24.5°C', type: 'text' as const },
    { key: 'humidity', label: 'HUMEDAD', value: '65%', type: 'badge' as const, variant: 'info' as const },
    { key: 'ph', label: 'pH', value: '6.8', type: 'number' as const },
    { key: 'unit', label: 'UNIDAD', value: '°C', type: 'text' as const },
    { key: 'status', label: 'ESTADO', value: 'Activo', type: 'badge' as const, variant: 'success' as const },
    { key: 'warning', label: 'ALERTA', value: 'Revisar', type: 'badge' as const, variant: 'warning' as const },
    { key: 'error', label: 'ERROR', value: 'Crítico', type: 'badge' as const, variant: 'danger' as const },
  ];

  return (
    <SafeAreaView style={styles.container}>
      <Header />
      <ScrollView style={styles.scrollView} contentContainerStyle={styles.content}>
        <View style={styles.header}>
          <Text variant="h1" color={semanticColors.textPrimary} style={styles.title}>
            Componentes Corregidos
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
            EntityCard, InfoField y CrudList
          </Text>
        </View>

        {/* Sección EntityCard */}
        <View style={styles.section}>
          <Text variant="h3" style={styles.sectionTitle}>
            EntityCard
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.sectionDescription}>
            Componente para mostrar entidades con información detallada y acciones
          </Text>
          <View style={styles.componentContainer}>
            <EntityCard
              entity={exampleVariable}
              config={entityConfig}
              actions={entityActions}
              icon="temperature"
              iconColor="#FF6B6B"
              onPress={(entity) => console.log('Card presionado:', entity.title)}
            />
          </View>
        </View>

        {/* Sección InfoField */}
        <View style={styles.section}>
          <Text variant="h3" style={styles.sectionTitle}>
            InfoField
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.sectionDescription}>
            Componente para mostrar campos de información con diferentes tipos y variantes
          </Text>
          <View style={styles.componentContainer}>
            {exampleFields.map((field, index) => (
              <InfoField
                key={`field-${index}`}
                field={field}
                style={styles.infoFieldItem}
              />
            ))}
          </View>
        </View>

        {/* Sección CrudList */}
        <View style={styles.section}>
          <Text variant="h3" style={styles.sectionTitle}>
            CrudList
          </Text>
          <Text variant="body" color={semanticColors.textSecondary} style={styles.sectionDescription}>
            Lista completa con paginación, acciones y configuración de entidades
          </Text>
          <View style={styles.crudListContainer}>
            <CrudListExample />
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  scrollView: {
    flex: 1,
  },
  content: {
    padding: spacing.md,
    paddingBottom: spacing.xl,
  },
  header: {
    marginBottom: spacing.xl,
    alignItems: 'center',
  },
  title: {
    textAlign: 'center',
    marginBottom: spacing.xs,
  },
  subtitle: {
    textAlign: 'center',
  },
  section: {
    marginBottom: spacing.xl,
  },
  sectionTitle: {
    marginBottom: spacing.xs,
    color: semanticColors.textPrimary,
  },
  sectionDescription: {
    marginBottom: spacing.md,
    lineHeight: 20,
  },
  componentContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: borderRadius.md,
    padding: spacing.md,
  },
  infoFieldItem: {
    marginBottom: spacing.sm,
  },
  crudListContainer: {
    backgroundColor: semanticColors.backgroundSecondary,
    borderRadius: borderRadius.md,
    padding: spacing.sm,
    minHeight: 400,
  },
});