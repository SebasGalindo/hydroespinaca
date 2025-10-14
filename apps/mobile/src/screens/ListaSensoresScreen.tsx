import React, { useEffect, useState } from 'react';
import { View, StyleSheet, ScrollView, RefreshControl } from 'react-native';
import { 
  semanticColors, 
  spacing, 
  useSensorStore,
  BaseEntity,
  EntityConfig,
  EntityAction,
  InfoField
} from '@hidroespinaca/shared';
import type { IndividualSensorData } from '@hidroespinaca/shared';
import type { SensorState } from '@hidroespinaca/shared/src/store/sensorStore';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { Icon } from '../components/atoms/Icon';
import { EntityCard } from '../components/molecules/EntityCard';
import { SensorBottomSheet } from '../components/organisms/SensorBottomSheet';
import { DeleteSensorConfirmationBottomSheet } from '../components/organisms/DeleteSensorConfirmationBottomSheet';

// Función para transformar SystemComponent a BaseEntity
const transformSystemComponentToEntity = (component: any): BaseEntity => {
  const fields: InfoField[] = [
    { key: 'physicalId', label: 'ID FÍSICO', value: component.details || 'N/A', type: 'text' },
    { key: 'location', label: 'UBICACIÓN', value: 'rack-1', type: 'text' },
    { key: 'frequency', label: 'FRECUENCIA', value: '30s', type: 'text' },
    { key: 'esp32Id', label: 'ESP32 ID', value: '6883fff7b079...', type: 'text' },
  ];

  return {
    id: component.name,
    title: component.name,
    subtitle: component.lastUpdate,
    identifier: component.details || 'N/A',
    description: `Sensor ${component.name.toLowerCase()}`,
    status: component.status === 'online' ? 'active' : component.status === 'warning' ? 'inactive' : 'deprecated',
    modifiedDate: component.lastUpdate,
    fields
  };
};



export function ListaSensoresScreen(): React.ReactElement {
  const {
    systemComponents,
    initializeSystemComponents,
    loading,
    setLoading,
    individualSensors,
    addIndividualSensor,
    updateIndividualSensor,
    removeIndividualSensor,
    initializeIndividualSensors
  } = useSensorStore() as SensorState;
  
  // Estados para los BottomSheets
  const [isSensorBottomSheetVisible, setIsSensorBottomSheetVisible] = useState(false);
  const [isDeleteConfirmationVisible, setIsDeleteConfirmationVisible] = useState(false);
  const [selectedSensor, setSelectedSensor] = useState<IndividualSensorData | null>(null);
  const [isEditMode, setIsEditMode] = useState(false);

  useEffect(() => {
    initializeSystemComponents();
    initializeIndividualSensors();
  }, [initializeSystemComponents, initializeIndividualSensors]);

  const handleRefresh = () => {
    setLoading(true);
    setTimeout(() => {
      initializeSystemComponents();
      setLoading(false);
    }, 1000);
  };

  // Configuración para los sensores del sistema
  const systemSensorConfig: EntityConfig = {
    entityType: 'sensor',
    showDescription: false,
    showModifiedDate: false,
    showIdentifier: true,
  };

  // Función para convertir BaseEntity a IndividualSensorData
  const convertToSensorData = (entity: BaseEntity): IndividualSensorData => {
    // Buscar si existe un sensor individual con este ID
    const individualSensor = individualSensors.find((s: IndividualSensorData) => s.id === entity.id || s.idFisico === entity.id);
    
    if (individualSensor) {
      // Si es un sensor individual, usar sus datos completos
      return {
        id: individualSensor.id || '',
        idFisico: individualSensor.idFisico,
        ubicacion: individualSensor.ubicacion,
        esp32Id: individualSensor.esp32Id,
        frecuenciaLectura: individualSensor.frecuenciaLectura,
        variablesAMedir: individualSensor.variablesAMedir,
        unidadMedida: individualSensor.unidadMedida,
        rangoMinimo: individualSensor.rangoMinimo,
        rangoMaximo: individualSensor.rangoMaximo,
        rangoOptimoMinimo: individualSensor.rangoOptimoMinimo,
        rangoOptimoMaximo: individualSensor.rangoOptimoMaximo,
        estado: individualSensor.estado,
        createdAt: individualSensor.createdAt || '',
        lastModified: individualSensor.lastModified || ''
      };
    } else {
      // Si es un sensor del sistema, usar valores por defecto
      return {
        idFisico: entity.fields?.find(f => f.key === 'physicalId')?.value || '',
        ubicacion: entity.fields?.find(f => f.key === 'location')?.value || '',
        esp32Id: entity.fields?.find(f => f.key === 'esp32Id')?.value || '',
        frecuenciaLectura: 30, // Default value
        variablesAMedir: [], // Default empty array
        unidadMedida: '',
        rangoMinimo: 0,
        rangoMaximo: 100,
        rangoOptimoMinimo: 20,
        rangoOptimoMaximo: 80,
        estado: entity.status === 'active' ? 'activo' : 'inactivo'
      };
    }
  };

  // Función para obtener las acciones de cada sensor del sistema
  const getSystemSensorActions = (sensor: BaseEntity): EntityAction[] => [
    {
      id: 'edit',
      label: 'Editar',
      variant: 'primary',
      icon: 'edit',
      onClick: () => {
        const sensorData = convertToSensorData(sensor);
        setSelectedSensor(sensorData);
        setIsEditMode(true);
        setIsSensorBottomSheetVisible(true);
      },
    },
    {
      id: 'delete',
      label: 'Eliminar',
      variant: 'danger',
      icon: 'delete',
      onClick: () => {
        const sensorData = convertToSensorData(sensor);
        setSelectedSensor(sensorData);
        setIsDeleteConfirmationVisible(true);
      },
    },
  ];

  const handleCreateSensor = () => {
    setSelectedSensor(null);
    setIsEditMode(false);
    setIsSensorBottomSheetVisible(true);
  };

  const handleSensorSave = (sensorData: IndividualSensorData) => {
    console.log('Guardando sensor:', sensorData);
    
    if (sensorData.id) {
      // Actualizar sensor existente
      updateIndividualSensor(sensorData.id, sensorData);
    } else {
      // Crear nuevo sensor
      addIndividualSensor(sensorData);
    }
    
    setIsSensorBottomSheetVisible(false);
    setSelectedSensor(null);
  };

  const handleSensorDelete = () => {
    console.log('Eliminando sensor:', selectedSensor);
    
    if (selectedSensor?.id) {
      removeIndividualSensor(selectedSensor.id);
    }
    
    setIsDeleteConfirmationVisible(false);
    setSelectedSensor(null);
  };

  const getSensorIcon = (sensorName: string) => {
    const name = sensorName.toLowerCase();
    if (name.includes('temperatura')) return 'temperature';
    if (name.includes('humedad')) return 'humidity';
    if (name.includes('ph')) return 'ph';
    if (name.includes('luz') || name.includes('lumínica')) return 'light';
    if (name.includes('conductividad')) return 'electric';
    if (name.includes('wifi') || name.includes('conectividad')) return 'water';
    return 'settings';
  };

  // Función para transformar IndividualSensorData a BaseEntity
  const transformIndividualSensorToEntity = (sensor: IndividualSensorData): BaseEntity => {
    return {
      id: sensor.id || sensor.idFisico,
      title: `Sensor ${sensor.idFisico}`,
      subtitle: sensor.ubicacion,
      identifier: sensor.idFisico,
      description: sensor.ubicacion,
      status: sensor.estado === 'activo' ? 'active' : 'inactive',
      modifiedDate: sensor.lastModified || new Date().toISOString(),
      fields: [
        { key: 'physicalId', label: 'ID Físico', value: sensor.idFisico },
        { key: 'location', label: 'Ubicación', value: sensor.ubicacion },
        { key: 'esp32Id', label: 'ESP32 ID', value: sensor.esp32Id },
        { key: 'frequency', label: 'Frecuencia', value: `${sensor.frecuenciaLectura}s` },
        { key: 'variables', label: 'Variables', value: sensor.variablesAMedir.join(', ') || 'No configuradas' },
        { key: 'unit', label: 'Unidad', value: sensor.unidadMedida || 'No especificada' },
        { key: 'range', label: 'Rango', value: `${sensor.rangoMinimo} - ${sensor.rangoMaximo}` },
        { key: 'optimalRange', label: 'Rango Óptimo', value: `${sensor.rangoOptimoMinimo} - ${sensor.rangoOptimoMaximo}` }
      ]
    };
  };

  // Función para obtener las acciones de cada sensor individual
  const getIndividualSensorActions = (entity: BaseEntity): EntityAction[] => [
    {
      id: 'edit',
      label: 'Editar',
      variant: 'primary',
      icon: 'edit',
      onClick: () => {
        const sensorData = convertToSensorData(entity);
        sensorData.id = entity.id; // Asegurar que tenga el ID
        setSelectedSensor(sensorData);
        setIsEditMode(true);
        setIsSensorBottomSheetVisible(true);
      }
    },
    {
      id: 'delete',
      label: 'Eliminar',
      variant: 'danger',
      icon: 'delete',
      onClick: () => {
        const sensorData = convertToSensorData(entity);
        sensorData.id = entity.id; // Asegurar que tenga el ID
        setSelectedSensor(sensorData);
        setIsDeleteConfirmationVisible(true);
      }
    }
  ];

  const transformedSystemComponents = systemComponents.map(transformSystemComponentToEntity);
  const transformedIndividualSensors = individualSensors.map(transformIndividualSensorToEntity);

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Configuración de Sensores
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
          Administra y monitorea los sensores del invernadero
        </Text>
        
        {/* Botón Crear */}
        <Button
          variant="primary"
          onPress={handleCreateSensor}
          style={styles.createButton}
          leftIcon={<Icon name="plus" size={16} color={semanticColors.textInverse} />}
        >
          AGREGAR SENSOR
        </Button>
      </View>

      <ScrollView 
        style={styles.scrollView}
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={loading} onRefresh={handleRefresh} />
        }
      >
        {/* Individual Sensors Section */}
        {transformedIndividualSensors.length > 0 && (
          <View style={styles.section}>
            <Text variant="h3" color={semanticColors.primary} style={styles.sectionTitle}>
              Sensores Configurados
            </Text>
            {transformedIndividualSensors.map((sensor: BaseEntity) => (
              <EntityCard
                key={sensor.id}
                entity={sensor}
                config={systemSensorConfig}
                actions={getIndividualSensorActions(sensor)}
                icon={getSensorIcon(sensor.title)}
                iconColor={semanticColors.primary}
                style={styles.card}
              />
            ))}
          </View>
        )}

        {/* System Components Section */}
        {transformedSystemComponents.length > 0 && (
          <View style={styles.section}>
            <Text variant="h3" color={semanticColors.primary} style={styles.sectionTitle}>
              Sensores del Sistema
            </Text>
            {transformedSystemComponents.map((component: BaseEntity) => (
              <EntityCard
                key={component.id}
                entity={component}
                config={systemSensorConfig}
                actions={getSystemSensorActions(component)}
                icon={getSensorIcon(component.title)}
                iconColor={semanticColors.primary}
                style={styles.card}
              />
            ))}
          </View>
        )}

        {/* Empty State */}
        {transformedSystemComponents.length === 0 && transformedIndividualSensors.length === 0 && !loading && (
          <View style={styles.emptyContainer}>
            <Text variant="body" color={semanticColors.textSecondary}>
              No hay sensores configurados
            </Text>
          </View>
        )}

        {/* Loading State */}
        {loading && (
          <View style={styles.loadingContainer}>
            <Text variant="body" color={semanticColors.textSecondary}>
              Cargando sensores...
            </Text>
          </View>
        )}
      </ScrollView>

      {/* BottomSheets */}
      <SensorBottomSheet
        isVisible={isSensorBottomSheetVisible}
        onClose={() => {
          setIsSensorBottomSheetVisible(false);
          setSelectedSensor(null);
        }}
        onSave={handleSensorSave}
        sensor={selectedSensor}
        isEditMode={isEditMode}
      />

      <DeleteSensorConfirmationBottomSheet
        isVisible={isDeleteConfirmationVisible}
        onClose={() => {
          setIsDeleteConfirmationVisible(false);
          setSelectedSensor(null);
        }}
        onConfirm={handleSensorDelete}
        sensor={selectedSensor}
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
  section: {
    marginBottom: spacing.lg,
  },
  sectionTitle: {
    marginBottom: spacing.md,
    textAlign: 'left',
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