import React, { useState } from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hydroespinaca/shared';
import { CrudList } from '../components/organisms/CrudList';
import { 
  Variable, 
  EntityConfig, 
  EntityAction,
  PaginationProps,
  FieldConfig 
} from '@hydroespinaca/shared';

// Datos de ejemplo para Variables
const mockVariables: Variable[] = [
  {
    id: '1',
    title: 'Temperatura Ambiente',
    subtitle: 'Temperatura del aire en el invernadero',
    identifier: 'temp-ambiente',
    description: 'Temperatura del aire en el invernadero',
    unit: '°C',
    type: 'input',
    dataType: 'numeric',
    minValue: 0,
    maxValue: 50,
    isRequired: true,
    category: 'environmental',
    status: 'active',
    modifiedDate: '2024-01-18',
    fields: [
      { key: 'type', label: 'TIPO', value: 'input', type: 'text' },
      { key: 'category', label: 'CATEGORÍA', value: 'environmental', type: 'text' },
      { key: 'unit', label: 'UNIDAD', value: '°C', type: 'text' },
      { key: 'dataType', label: 'TIPO DE DATO', value: 'numeric', type: 'text' },
      { key: 'range', label: 'RANGO', value: '0-50', type: 'text' },
    ],
  },
  {
    id: '2',
    title: 'Humedad Relativa',
    subtitle: 'Porcentaje de humedad en el ambiente',
    identifier: 'hum-relativa',
    description: 'Porcentaje de humedad en el ambiente',
    unit: '%',
    type: 'input',
    dataType: 'numeric',
    minValue: 0,
    maxValue: 100,
    isRequired: true,
    category: 'environmental',
    status: 'active',
    modifiedDate: '2024-01-17',
    fields: [
      { key: 'type', label: 'TIPO', value: 'input', type: 'text' },
      { key: 'category', label: 'CATEGORÍA', value: 'environmental', type: 'text' },
      { key: 'unit', label: 'UNIDAD', value: '%', type: 'text' },
      { key: 'dataType', label: 'TIPO DE DATO', value: 'numeric', type: 'text' },
      { key: 'range', label: 'RANGO', value: '0-100', type: 'text' },
    ],
  },
  {
    id: '3',
    title: 'pH del Agua',
    subtitle: 'Nivel de acidez del agua de riego',
    identifier: 'ph-agua',
    description: 'Nivel de acidez del agua de riego',
    unit: 'pH',
    type: 'input',
    dataType: 'numeric',
    minValue: 0,
    maxValue: 14,
    isRequired: true,
    category: 'control',
    status: 'inactive',
    modifiedDate: '2024-01-16',
    fields: [
      { key: 'type', label: 'TIPO', value: 'input', type: 'text' },
      { key: 'category', label: 'CATEGORÍA', value: 'control', type: 'text' },
      { key: 'unit', label: 'UNIDAD', value: 'pH', type: 'text' },
      { key: 'dataType', label: 'TIPO DE DATO', value: 'numeric', type: 'text' },
      { key: 'range', label: 'RANGO', value: '0-14', type: 'text' },
    ],
  },
];

// Configuración para Variables
const variableConfig: EntityConfig = {
  entityType: 'variable',
  titleField: 'title',
  subtitleField: 'subtitle',
  technicalIdField: 'identifier',
  statusField: 'status',
  lastModifiedField: 'modifiedDate',
  infoFields: [
    { key: 'type', label: 'TIPO', type: 'text' },
    { key: 'category', label: 'CATEGORÍA', type: 'text' },
    { key: 'unit', label: 'UNIDAD', type: 'text' },
    { key: 'dataType', label: 'TIPO DE DATO', type: 'text' },
    { key: 'range', label: 'RANGO', type: 'text' },
  ],
  defaultActions: [
    { 
      id: 'edit',
      type: 'edit', 
      label: 'Editar', 
      variant: 'primary',
      icon: 'settings',
      onClick: () => console.log('Editar acción por defecto')
    },
    { 
      id: 'delete',
      type: 'delete', 
      label: 'Eliminar', 
      variant: 'danger',
      icon: 'close',
      onClick: () => console.log('Eliminar acción por defecto')
    },
  ],
};

export function CrudListExample(): React.ReactElement {
  const [currentPage, setCurrentPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  // Paginación simulada
  const itemsPerPage = 10;
  const totalItems = mockVariables.length;
  const totalPages = Math.ceil(totalItems / itemsPerPage);

  const pagination: PaginationProps = {
    currentPage,
    totalPages,
    totalItems,
    itemsPerPage,
    loading,
    onPageChange: (page: number) => {
      setLoading(true);
      setCurrentPage(page);
      // Simular carga
      setTimeout(() => setLoading(false), 500);
    },
  };

  // Función para obtener acciones específicas por item
  const getActions = (item: Variable): EntityAction[] => {
    const actions: EntityAction[] = [
      { 
        id: 'edit',
        type: 'edit', 
        label: 'Editar', 
        variant: 'primary',
        icon: 'settings',
        onClick: () => console.log('Editar:', item.title)
      },
    ];

    // Solo agregar eliminar si no está activo
    if (item.status !== 'active') {
      actions.push({ 
        id: 'delete',
        type: 'delete', 
        label: 'Eliminar', 
        variant: 'danger',
        icon: 'close',
        onClick: () => console.log('Eliminar:', item.title)
      });
    }

    return actions;
  };

  // Manejadores de eventos
  const handleRefresh = () => {
    setRefreshing(true);
    // Simular recarga
    setTimeout(() => setRefreshing(false), 1000);
  };

  const handleItemPress = (item: Variable) => {
    console.log('Item presionado:', item.title);
  };

  return (
    <View style={styles.container}>
      <CrudList<Variable>
        title="Variables del Sistema"
        subtitle="Gestiona las variables de entrada y salida"
        data={mockVariables}
        config={variableConfig}
        pagination={pagination}
        loading={loading}
        refreshing={refreshing}
        onRefresh={handleRefresh}
        getActions={getActions}
        onItemPress={handleItemPress}
        emptyMessage="No hay variables configuradas"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.backgroundPrimary,
  },
});