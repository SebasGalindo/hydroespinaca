import React, { useEffect, useState } from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { 
  semanticColors, 
  spacing, 
  useVariableStore,
  BaseEntity,
  EntityConfig,
  EntityAction,
  InfoField,
  VariableData
} from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { Icon } from '../components/atoms/Icon';
import { EntityCard } from '../components/molecules/EntityCard';
import { VariableBottomSheet } from '../components/organisms/VariableBottomSheet';
import { DeleteConfirmationBottomSheet } from '../components/organisms/DeleteConfirmationBottomSheet';
import { VariableFormData } from '../components/organisms/VariableForm';

// Función para transformar VariableData a BaseEntity
const transformVariableToEntity = (variable: any): BaseEntity => {
  const fields: InfoField[] = [
    { key: 'type', label: 'TIPO', value: variable.type, type: 'text' },
    { key: 'category', label: 'CATEGORÍA', value: variable.category, type: 'text' },
    { key: 'unit', label: 'UNIDAD', value: variable.unit, type: 'text' },
    { key: 'dataType', label: 'TIPO DE DATO', value: variable.dataType, type: 'text' },
  ];

  // Agregar rango si tiene valores min/max
  if (variable.minValue !== undefined && variable.maxValue !== undefined) {
    fields.push({
      key: 'range',
      label: 'RANGO',
      value: `${variable.minValue} - ${variable.maxValue} ${variable.unit}`,
      type: 'text'
    });
  }

  return {
    id: variable.id,
    title: variable.name,
    subtitle: variable.description || '',
    identifier: variable.id,
    description: variable.description,
    status: variable.status,
    modifiedDate: variable.lastModified,
    fields
  };
};

export function ListaVariablesScreen(): React.ReactElement {
  const { variables, loading, initializeVariables, addVariable, updateVariable, removeVariable } = useVariableStore();
  
  // Estados para los bottom sheets
  const [isCreateModalVisible, setIsCreateModalVisible] = useState(false);
  const [isEditModalVisible, setIsEditModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [selectedVariable, setSelectedVariable] = useState<VariableData | null>(null);

  useEffect(() => {
    if (variables.length === 0) {
      initializeVariables();
    }
  }, [variables.length, initializeVariables]);

  // Configuración para las variables
  const variableConfig: EntityConfig = {
    entityType: 'variable',
    showDescription: true,
    showModifiedDate: true,
    showIdentifier: true,
  };

  // Funciones para transformar datos entre formatos
  const transformFormDataToVariable = (formData: VariableFormData): Omit<VariableData, 'id' | 'lastModified' | 'createdAt'> => {
    const baseData = {
      name: formData.name,
      description: formData.description,
      type: formData.type,
      dataType: formData.dataType,
      unit: formData.unit,
      category: formData.category,
      status: 'active' as const,
      isRequired: false, // Por defecto las variables no son requeridas
    };

    // Solo agregar minValue y maxValue si son válidos y el tipo es numérico
    if (formData.dataType === 'numeric') {
      const minVal = parseFloat(formData.minValue);
      const maxVal = parseFloat(formData.maxValue);
      
      return {
        ...baseData,
        ...(formData.minValue && !isNaN(minVal) && { minValue: minVal }),
        ...(formData.maxValue && !isNaN(maxVal) && { maxValue: maxVal }),
      };
    }

    return baseData;
  };

  const transformVariableToFormData = (variable: VariableData): VariableFormData => ({
    name: variable.name,
    description: variable.description || '',
    type: variable.type,
    dataType: variable.dataType,
    unit: variable.unit,
    category: variable.category,
    minValue: variable.minValue !== undefined ? variable.minValue.toString() : '',
    maxValue: variable.maxValue !== undefined ? variable.maxValue.toString() : '',
  });

  // Funciones para manejar las acciones
  const handleCreateVariable = () => {
    setIsCreateModalVisible(true);
  };

  const handleEditVariable = (variableId: string) => {
    const variable = variables.find(v => v.id === variableId);
    if (variable) {
      setSelectedVariable(variable);
      setIsEditModalVisible(true);
    }
  };

  const handleDeleteVariable = (variableId: string) => {
    const variable = variables.find(v => v.id === variableId);
    if (variable) {
      setSelectedVariable(variable);
      setIsDeleteModalVisible(true);
    }
  };

  const handleConfirmCreate = (formData: VariableFormData) => {
    const newVariable = transformFormDataToVariable(formData);
    addVariable(newVariable);
    setIsCreateModalVisible(false);
  };

  const handleConfirmEdit = (formData: VariableFormData) => {
    if (selectedVariable) {
      const updatedVariable = {
        ...transformFormDataToVariable(formData),
        id: selectedVariable.id,
      };
      updateVariable(selectedVariable.id, updatedVariable);
      setIsEditModalVisible(false);
      setSelectedVariable(null);
    }
  };

  const handleConfirmDelete = () => {
    if (selectedVariable) {
      removeVariable(selectedVariable.id);
      setIsDeleteModalVisible(false);
      setSelectedVariable(null);
    }
  };

  // Función para obtener las acciones de cada variable
  const getVariableActions = (variable: BaseEntity): EntityAction[] => [
    {
      id: 'edit',
      label: 'Editar',
      variant: 'primary',
      icon: 'edit',
      onClick: () => handleEditVariable(variable.id),
    },
    {
      id: 'delete',
      label: 'Eliminar',
      variant: 'danger',
      icon: 'delete',
      onClick: () => handleDeleteVariable(variable.id),
    },
  ];

  const transformedVariables = variables.map(transformVariableToEntity);

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Configuración de Variables
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
          Administra las variables del sistema de control
        </Text>
        
        {/* Botón Crear */}
        <Button
          variant="primary"
          onPress={handleCreateVariable}
          style={styles.createButton}
          leftIcon={<Icon name="plus" size={16} color={semanticColors.textInverse} />}
        >
          AGREGAR VARIABLE
        </Button>
      </View>

      {/* Lista de Variables */}
      <ScrollView 
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
      >
        {loading ? (
          <View style={styles.loadingContainer}>
            <Text variant="body" color={semanticColors.textSecondary}>
              Cargando variables...
            </Text>
          </View>
        ) : transformedVariables.length === 0 ? (
          <View style={styles.emptyContainer}>
            <Text variant="body" color={semanticColors.textSecondary}>
              No hay variables configuradas
            </Text>
          </View>
        ) : (
          transformedVariables.map((variable) => (
            <EntityCard
              key={variable.id}
              entity={variable}
              config={variableConfig}
              actions={getVariableActions(variable)}
              icon="settings"
              iconColor={semanticColors.primary}
              style={styles.card}
            />
          ))
        )}
      </ScrollView>

      {/* Bottom Sheets */}
      <VariableBottomSheet
        isVisible={isCreateModalVisible}
        onClose={() => setIsCreateModalVisible(false)}
        onConfirm={handleConfirmCreate}
        title="Crear Nueva Variable"
      />

      <VariableBottomSheet
        isVisible={isEditModalVisible}
        onClose={() => {
          setIsEditModalVisible(false);
          setSelectedVariable(null);
        }}
        onConfirm={handleConfirmEdit}
        initialData={selectedVariable ? transformVariableToFormData(selectedVariable) : undefined}
        isEditing={true}
        title="Editar Variable"
      />

      <DeleteConfirmationBottomSheet
        isVisible={isDeleteModalVisible}
        onClose={() => {
          setIsDeleteModalVisible(false);
          setSelectedVariable(null);
        }}
        onConfirm={handleConfirmDelete}
        variableName={selectedVariable?.name}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.backgroundPrimary,
  },
  header: {
    padding: spacing.lg,
    backgroundColor: semanticColors.backgroundPrimary,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.borderMuted,
  },
  title: {
    marginBottom: spacing.xs,
    textAlign: 'left',
  },
  subtitle: {
    marginBottom: spacing.lg,
    textAlign: 'left',
    lineHeight: 20,
  },
  createButton: {
    alignSelf: 'flex-start',
    backgroundColor: semanticColors.primary,
  },
  scrollView: {
    flex: 1,
  },
  scrollContent: {
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
  },
  card: {
    marginVertical: spacing.xs,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: spacing.xl,
  },
});