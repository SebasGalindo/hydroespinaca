import React, { useMemo } from 'react';
import {
  View,
  StyleSheet,
  ListRenderItem,
} from 'react-native';
import { semanticColors, spacing, borderRadius, typography, IconName } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Heading } from '../atoms/Heading';
import { Icon } from '../atoms/Icon';
import { Badge } from '../atoms/Badge';
import { Card } from '../molecules/Card';
import { DataList, DataListProps } from './DataList';
import { SensorSummary, IndividualReading } from '@hydroespinaca/shared';

// Tipos para los diferentes modos de la tabla
export type ReadingsTableMode = 'summary' | 'individual';

export interface ReadingsTableProps extends Omit<DataListProps, 'data' | 'renderItem'> {
  /** Modo de la tabla: resumen o lecturas individuales */
  mode: ReadingsTableMode;
  /** Datos de resumen de sensores */
  summaryData?: SensorSummary[];
  /** Datos de lecturas individuales */
  individualData?: IndividualReading[];
  /** Función llamada al tocar un item del resumen */
  onSummaryItemPress?: (item: SensorSummary) => void;
  /** Función llamada al tocar una lectura individual */
  onIndividualItemPress?: (item: IndividualReading) => void;
  /** Mostrar iconos de sensores */
  showSensorIcons?: boolean;
  /** Formato de fecha personalizado */
  dateFormat?: 'short' | 'long' | 'time';
}

// Mapeo de tipos de sensores a iconos
const SENSOR_ICONS: Record<string, IconName> = {
  temperature: 'power',
  humidity: 'battery',
  light: 'star',
  ph: 'info',
  conductivity: 'power',
  dissolved_oxygen: 'heart',
  turbidity: 'eye',
  default: 'settings',
};

// Mapeo de tipos de sensores a colores
const SENSOR_COLORS: Record<string, string> = {
  temperature: '#FF6B6B',
  humidity: '#4ECDC4',
  light: '#FFE66D',
  ph: '#A8E6CF',
  conductivity: '#FFB74D',
  dissolved_oxygen: '#81C784',
  turbidity: '#90A4AE',
  default: semanticColors.primary,
};

export function ReadingsTable({
  mode,
  summaryData = [],
  individualData = [],
  onSummaryItemPress,
  onIndividualItemPress,
  showSensorIcons = true,
  dateFormat = 'short',
  ...dataListProps
}: ReadingsTableProps): React.ReactElement {

  // Función para formatear fechas
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    
    switch (dateFormat) {
      case 'long':
        return date.toLocaleDateString('es-ES', {
          year: 'numeric',
          month: 'long',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
        });
      case 'time':
        return date.toLocaleTimeString('es-ES', {
          hour: '2-digit',
          minute: '2-digit',
        });
      default:
        return date.toLocaleDateString('es-ES', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
        });
    }
  };

  // Función para obtener el icono del sensor
  const getSensorIcon = (sensorType: string): IconName => {
    const icon = SENSOR_ICONS[sensorType.toLowerCase()];
    return icon ?? SENSOR_ICONS.default ?? 'settings';
  };

  // Función para obtener el color del sensor
  const getSensorColor = (sensorType: string): string => {
    const color = SENSOR_COLORS[sensorType.toLowerCase()];
    return color ?? SENSOR_COLORS.default ?? semanticColors.primary;
  };

  // Función para formatear el valor con unidad
  const formatValue = (value: number, unit: string): string => {
    return `${value.toFixed(1)} ${unit}`;
  };

  // Renderizador para items de resumen con layout 5 filas x 2 columnas
  const renderSummaryItem: ListRenderItem<SensorSummary> = ({ item }) => {
    const sensorColor = getSensorColor(item.sensor);
    const sensorIcon = getSensorIcon(item.sensor);

    return (
      <Card 
        padding="md" 
        elevated={true} 
        style={styles.summaryCard}
      >
        {/* Fila 1: Título del sensor */}
        <View style={styles.sensorHeader}>
          {showSensorIcons && (
            <View style={[styles.iconContainer, { backgroundColor: sensorColor + '20' }]}>
              <Icon name={sensorIcon} size={24} color={sensorColor} />
            </View>
          )}
          <View style={styles.sensorTitleContainer}>
            <Text variant="h4" color={semanticColors.textPrimary} style={styles.sensorTitle}>
              {item.sensor}
            </Text>
            <Badge variant="info" size="md" style={styles.headerBadge}>
              {item.unidad}
            </Badge>
          </View>
        </View>

        {/* Fila 2: Media */}
        <View style={styles.dataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.labelText}>
            Media:
          </Text>
          <Text variant="bodyLarge" color={sensorColor} style={styles.valueText}>
            {formatValue(item.media, item.unidad)}
          </Text>
        </View>

        {/* Fila 3: Mínimo */}
        <View style={styles.dataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.labelText}>
            Mínimo:
          </Text>
          <Text variant="bodyLarge" color={semanticColors.textPrimary} style={styles.valueText}>
            {formatValue(item.minimo, item.unidad)}
          </Text>
        </View>

        {/* Fila 4: Máximo */}
        <View style={styles.dataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.labelText}>
            Máximo:
          </Text>
          <Text variant="bodyLarge" color={semanticColors.textPrimary} style={styles.valueText}>
            {formatValue(item.maximo, item.unidad)}
          </Text>
        </View>

        {/* Fila 5: Última Lectura */}
        <View style={styles.dataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.labelText}>
            Última Lectura:
          </Text>
          <Text variant="bodyLarge" color={semanticColors.textPrimary} style={styles.valueText}>
            {item.ultimaLectura}
          </Text>
        </View>
      </Card>
    );
  };

  // Renderizador para lecturas individuales mejorado
  const renderIndividualItem: ListRenderItem<IndividualReading> = ({ item }) => {
    const sensorColor = getSensorColor(item.sensor);

    return (
      <Card 
        padding="md"
        elevated={true} 
        style={styles.individualCard}
      >
        {/* Fila 1: Fecha y Hora */}
        <View style={styles.cleanDataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.cleanLabelText}>
            Fecha y Hora:
          </Text>
          <Text variant="bodyLarge" color={semanticColors.textPrimary} style={styles.cleanValueText}>
            {item.fecha}
          </Text>
        </View>

        {/* Fila 2: Sensor */}
        <View style={styles.cleanDataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.cleanLabelText}>
            Sensor:
          </Text>
          <View style={styles.sensorValueContainer}>
            <Text variant="bodyLarge" color={semanticColors.textPrimary} style={styles.cleanValueText}>
              {item.sensor}
            </Text>
            <Badge variant="info" size="md" style={styles.centeredBadge}>
              {item.unidad}
            </Badge>
          </View>
        </View>

        {/* Fila 3: Valor */}
        <View style={styles.cleanDataRow}>
          <Text variant="bodyLarge" color={semanticColors.textSecondary} style={styles.cleanLabelText}>
            Valor:
          </Text>
          <Text variant="bodyLarge" color={sensorColor} style={styles.consistentValueText}>
            {formatValue(item.valor, item.unidad)}
          </Text>
        </View>
      </Card>
    );
  };

  // Título según el modo - solo generar automáticamente si no se especifica title
  const title = useMemo(() => {
    // Si se pasa title explícitamente (incluso vacío), usarlo
    if (dataListProps.title !== undefined) return dataListProps.title;
    // Solo generar automáticamente para el modo summary
    return mode === 'summary' ? 'Resumen de los últimos 10 minutos' : '';
  }, [mode, dataListProps.title]);

  if (mode === 'summary') {
    return (
      <DataList<SensorSummary>
        {...dataListProps}
        data={summaryData || []}
        renderItem={renderSummaryItem}
        title={title}
        estimatedItemSize={200}
        keyExtractor={(item: SensorSummary, index: number) => 
          `summary-${item.sensor}-${index}`
        }
        // Evitar doble padding horizontal cuando el resumen se renderiza dentro de un Card con padding
        contentContainerStyle={{ paddingHorizontal: 0 }}
      />
    );
  } else {
    return (
      <DataList<IndividualReading>
        {...dataListProps}
        data={individualData || []}
        renderItem={renderIndividualItem}
        title={title}
        estimatedItemSize={120}
        keyExtractor={(item: IndividualReading, index: number) => 
          `individual-${item.id}-${index}`
        }
      />
    );
  }
}

const styles = StyleSheet.create({
  // Estilos para Card de resumen
  summaryCard: {
    marginVertical: spacing.xs,
  },

  // Estilos para Card de lecturas individuales
  individualCard: {
    marginVertical: spacing.xs,
  },

  // Header del sensor en resumen
  sensorHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.md,
    paddingBottom: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: semanticColors.borderMuted,
  },

  sensorTitleContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },

  sensorTitle: {
    fontWeight: typography.fontWeight.semibold as any,
  },

  headerBadge: {
    marginLeft: spacing.sm,
  },

  // Estilos para filas de datos en resumen
  dataRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.sm,
  },

  labelText: {
    flex: 1,
    fontWeight: typography.fontWeight.medium as any,
  },

  valueText: {
    flex: 1,
    textAlign: 'right',
    fontWeight: typography.fontWeight.semibold as any,
  },

  // Estilos para filas de datos en lecturas individuales (sin separadores)
  cleanDataRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: spacing.sm,
  },

  cleanLabelText: {
    flex: 1,
    fontWeight: typography.fontWeight.medium as any,
    fontSize: 16, // Tamaño más grande para mejor legibilidad
  },

  cleanValueText: {
    flex: 2,
    textAlign: 'right',
    fontWeight: typography.fontWeight.medium as any,
    fontSize: 16, // Tamaño consistente
  },

  sensorValueContainer: {
    flex: 2,
    flexDirection: 'row',
    justifyContent: 'flex-end',
    alignItems: 'center',
    gap: spacing.xs,
  },

  consistentValueText: {
    flex: 2,
    textAlign: 'right',
    fontWeight: typography.fontWeight.semibold as any,
    fontSize: 16, // Mismo tamaño que otros textos para coherencia
  },

  centeredBadge: {
    alignSelf: 'center', // Centrado verticalmente
  },

  // Icono del sensor
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: spacing.sm,
  },
});