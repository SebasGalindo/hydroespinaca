import React, { useState, useEffect } from 'react';
import { View, StyleSheet, ScrollView, RefreshControl } from 'react-native';
import { semanticColors, spacing, useActuatorStore, BaseEntity, EntityConfig, EntityAction, InfoField } from '@hydroespinaca/shared';
import { Text } from '../components/atoms/Text';
import { Button } from '../components/atoms/Button';
import { Icon } from '../components/atoms/Icon';
import { EntityCard } from '../components/molecules/EntityCard';
import { ActuadorBottomSheet, ActuadorData } from '../components/organisms/ActuadorBottomSheet';
import { DeleteActuadorConfirmationBottomSheet } from '../components/organisms/DeleteActuadorConfirmationBottomSheet';

// Transform ActuadorData to BaseEntity
const transformActuadorToEntity = (actuador: any): BaseEntity => {
  const fields: InfoField[] = [
    {
      label: 'Tipo',
      value: getTypeText(actuador.type),
      type: 'text'
    },
    {
      label: 'Ubicación',
      value: actuador.location,
      type: 'text'
    },
    {
      label: 'ESP32 ID',
      value: actuador.esp32Id,
      type: 'text'
    },
    {
      label: 'PIN ESP32',
      value: `GPIO ${actuador.pin}`,
      type: 'text'
    }
  ];

  return {
    id: actuador.id,
    title: actuador.name,
    status: actuador.status as 'active' | 'inactive' | 'deprecated',
    modifiedDate: actuador.lastModified,
    fields
  };
};

// Helper function for type text
const getTypeText = (type: string) => {
  switch (type) {
    case 'pump':
      return 'Bomba';
    case 'valve':
      return 'Válvula';
    case 'fan':
      return 'Ventilador';
    case 'heater':
      return 'Calentador';
    case 'light':
      return 'Luz';
    case 'motor':
      return 'Motor';
    default:
      return type;
  }
};

// Helper function for actuator icon
const getActuatorIcon = (type: string) => {
  switch (type) {
    case 'pump':
      return 'water';
    case 'valve':
      return 'settings';
    case 'fan':
      return 'power';
    case 'heater':
      return 'temperature';
    case 'light':
      return 'sun';
    case 'motor':
      return 'settings';
    default:
      return 'power';
  }
};

export function ListaActuadoresScreen(): React.ReactElement {
  const { actuadores, initializeActuadores, addActuador, updateActuador, removeActuador } = useActuatorStore();
  const [loading, setLoading] = React.useState(false);
  
  // Estados para los BottomSheets
  const [isActuadorBottomSheetVisible, setIsActuadorBottomSheetVisible] = useState(false);
  const [isDeleteConfirmationVisible, setIsDeleteConfirmationVisible] = useState(false);
  const [selectedActuador, setSelectedActuador] = useState<ActuadorData | null>(null);
  const [isEditMode, setIsEditMode] = useState(false);

  useEffect(() => {
    initializeActuadores();
  }, [initializeActuadores]);

  const handleRefresh = () => {
    setLoading(true);
    setTimeout(() => {
      initializeActuadores();
      setLoading(false);
    }, 1000);
  };

  // Función para convertir actuador a ActuadorData
  const convertToActuadorData = (actuador: any): ActuadorData => {
    return {
      id: actuador.id,
      nombre: actuador.name || '',
      tipoActuador: actuador.type || 'pump',
      ubicacion: actuador.location || '',
      esp32Id: actuador.esp32Id || '',
      pinEsp32: actuador.pin || 0,
      status: actuador.status === 'active' ? 'active' : 'inactive'
    };
  };

  const handleCreateActuador = () => {
    setSelectedActuador(null);
    setIsEditMode(false);
    setIsActuadorBottomSheetVisible(true);
  };

  const handleActuadorSave = (actuadorData: ActuadorData) => {
    console.log('Guardar actuador:', actuadorData);
    
    // Mapear los datos del formulario al formato del store
    const storeData = {
      name: actuadorData.nombre,
      type: actuadorData.tipoActuador as 'pump' | 'valve' | 'fan' | 'heater' | 'light' | 'motor',
      location: actuadorData.ubicacion,
      esp32Id: actuadorData.esp32Id,
      pin: actuadorData.pinEsp32,
      status: actuadorData.status as 'active' | 'inactive' | 'error' | 'maintenance'
    };

    if (isEditMode && selectedActuador?.id) {
      // Actualizar actuador existente
      updateActuador(selectedActuador.id, storeData);
    } else {
      // Crear nuevo actuador
      addActuador(storeData);
    }
    
    setIsActuadorBottomSheetVisible(false);
    setSelectedActuador(null);
  };

  const handleActuadorDelete = () => {
    console.log('Eliminar actuador:', selectedActuador);
    
    if (selectedActuador?.id) {
      removeActuador(selectedActuador.id);
    }
    
    setIsDeleteConfirmationVisible(false);
    setSelectedActuador(null);
  };

  // EntityConfig for actuadores
  const actuadorConfig: EntityConfig = {
    entityType: 'actuator',
    titleField: 'name',
    infoFields: [
      { key: 'type', label: 'Tipo', type: 'text' },
      { key: 'location', label: 'Ubicación', type: 'text' },
      { key: 'esp32Id', label: 'ESP32 ID', type: 'text' },
      { key: 'pin', label: 'PIN ESP32', type: 'text' }
    ],
    defaultActions: [
      { id: 'edit', label: 'Editar', icon: 'edit', onClick: () => {} },
      { id: 'delete', label: 'Eliminar', icon: 'delete', onClick: () => {} }
    ]
  };

  // Get actions for each actuador
  const getActuadorActions = (actuador: any): EntityAction[] => [
    {
      id: 'edit',
      label: 'Editar',
      variant: 'primary',
      icon: 'edit',
      onClick: () => {
        const actuadorData = convertToActuadorData(actuador);
        setSelectedActuador(actuadorData);
        setIsEditMode(true);
        setIsActuadorBottomSheetVisible(true);
      }
    },
    {
      id: 'delete',
      label: 'Eliminar',
      variant: 'danger',
      icon: 'delete',
      onClick: () => {
        const actuadorData = convertToActuadorData(actuador);
        setSelectedActuador(actuadorData);
        setIsDeleteConfirmationVisible(true);
      }
    }
  ];

  // Transform actuadores to entities
  const transformedActuadores = actuadores.map(transformActuadorToEntity);

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Configuración de Actuadores
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.subtitle}>
          Administra y controla los actuadores del invernadero
        </Text>
        
        {/* Action Buttons */}
        <View style={styles.actionButtons}>
          <Button
            variant="primary"
            onPress={handleCreateActuador}
            style={styles.actionButton}
          >
            <Icon name="plus" size={16} color="white" style={styles.buttonIcon} />
            AGREGAR ACTUADOR
          </Button>
        </View>
      </View>

      <ScrollView 
        style={styles.scrollView}
        refreshControl={
          <RefreshControl refreshing={loading} onRefresh={handleRefresh} />
        }
      >
        {/* Actuators List */}
        <View style={styles.section}>
          {/* Empty State */}
          {transformedActuadores.length === 0 ? (
            <View style={styles.emptyState}>
              <Icon 
                name="settings" 
                size={48} 
                color={semanticColors.textSecondary}
                style={styles.emptyIcon}
              />
              <Text variant="h3" color={semanticColors.textSecondary} style={styles.emptyTitle}>
                No hay actuadores configurados
              </Text>
              <Text variant="body" color={semanticColors.textSecondary} style={styles.emptyDescription}>
                Agrega tu primer actuador para comenzar a controlar el sistema
              </Text>
            </View>
          ) : (
            /* Actuadores Cards */
            transformedActuadores.map((entity, index) => {
              const actuador = actuadores[index];
              if (!actuador) return null;
              
              return (
                <EntityCard
                  key={entity.id}
                  entity={entity}
                  config={actuadorConfig}
                  actions={getActuadorActions(actuador)}
                  onPress={() => console.log('Actuador pressed:', entity.id)}
                  icon={getActuatorIcon(actuador.type)}
                  iconColor={semanticColors.primary}
                  style={styles.entityCard}
                />
              );
            })
          )}
        </View>
      </ScrollView>

      {/* BottomSheets */}
      <ActuadorBottomSheet
        isVisible={isActuadorBottomSheetVisible}
        onClose={() => {
          setIsActuadorBottomSheetVisible(false);
          setSelectedActuador(null);
        }}
        onSave={handleActuadorSave}
        actuador={selectedActuador}
        isEditMode={isEditMode}
      />

      <DeleteActuadorConfirmationBottomSheet
        isVisible={isDeleteConfirmationVisible}
        onClose={() => {
          setIsDeleteConfirmationVisible(false);
          setSelectedActuador(null);
        }}
        onConfirm={handleActuadorDelete}
        actuador={selectedActuador}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  header: {
    padding: spacing.lg,
    backgroundColor: semanticColors.surface,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.border,
  },
  title: {
    marginBottom: spacing.xs,
  },
  subtitle: {
    marginBottom: spacing.md,
    lineHeight: 20,
  },
  actionButtons: {
    flexDirection: 'row',
    justifyContent: 'center',
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  buttonIcon: {
    marginRight: spacing.xs,
  },
  scrollView: {
    flex: 1,
  },
  section: {
    padding: spacing.lg,
  },
  sectionTitle: {
    marginBottom: spacing.md,
  },
  entityCard: {
    marginBottom: spacing.lg,
  },
  emptyState: {
    alignItems: 'center',
    padding: spacing.xl,
    backgroundColor: semanticColors.surface,
    borderRadius: 8,
    marginTop: spacing.md,
  },
  emptyIcon: {
    marginBottom: spacing.md,
  },
  emptyTitle: {
    marginBottom: spacing.sm,
    textAlign: 'center',
  },
  emptyDescription: {
    textAlign: 'center',
    lineHeight: 20,
  },
});