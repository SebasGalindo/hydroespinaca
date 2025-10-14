import React, { useState } from 'react';
import { View, StyleSheet, ViewStyle } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import { Card } from '../molecules/Card';
import { Text } from '../atoms/Text';
import { Button } from '../atoms/Button';
import { Select } from '../atoms/Select';
import { DatePicker } from '../molecules/DatePicker';
import { Icon } from '../atoms/Icon';

export interface FilterTimeCardProps {
  onExport?: (filters: TimeFilters) => void;
  onFilterChange?: (filters: TimeFilters) => void;
  isRealTime?: boolean;
  onRealTimeToggle?: (enabled: boolean) => void;
  style?: ViewStyle;
}

export interface TimeFilters {
  startDate: Date;
  endDate: Date;
  timeRange: 'custom' | '1h' | '6h' | '24h' | '7d' | '30d';
  exportFormat: 'csv' | 'excel' | 'pdf';
}

const timeRangeOptions = [
  { label: 'Personalizado', value: 'custom' },
  { label: 'Última hora', value: '1h' },
  { label: 'Últimas 6 horas', value: '6h' },
  { label: 'Últimas 24 horas', value: '24h' },
  { label: 'Últimos 7 días', value: '7d' },
  { label: 'Últimos 30 días', value: '30d' },
];

const exportFormatOptions = [
  { label: 'CSV', value: 'csv' },
  { label: 'Excel', value: 'excel' },
  { label: 'PDF', value: 'pdf' },
];

export function FilterTimeCard({
  onExport,
  onFilterChange,
  isRealTime = false,
  onRealTimeToggle,
  style,
}: FilterTimeCardProps): React.ReactElement {
  const [filters, setFilters] = useState<TimeFilters>({
    startDate: new Date(Date.now() - 24 * 60 * 60 * 1000), // 24 horas atrás
    endDate: new Date(),
    timeRange: '24h',
    exportFormat: 'csv',
  });

  const [showStartDatePicker, setShowStartDatePicker] = useState(false);
  const [showEndDatePicker, setShowEndDatePicker] = useState(false);

  const handleTimeRangeChange = (option: { label: string; value: string | number }) => {
    const value = String(option.value);
    const newFilters = { ...filters, timeRange: value as TimeFilters['timeRange'] };
    
    if (value !== 'custom') {
      const now = new Date();
      let startDate = new Date();
      
      switch (value) {
        case '1h':
          startDate = new Date(now.getTime() - 60 * 60 * 1000);
          break;
        case '6h':
          startDate = new Date(now.getTime() - 6 * 60 * 60 * 1000);
          break;
        case '24h':
          startDate = new Date(now.getTime() - 24 * 60 * 60 * 1000);
          break;
        case '7d':
          startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
          break;
        case '30d':
          startDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
          break;
      }
      
      newFilters.startDate = startDate;
      newFilters.endDate = now;
    }
    
    setFilters(newFilters);
    onFilterChange?.(newFilters);
  };

  const handleDateChange = (date: Date, type: 'start' | 'end') => {
    const newFilters = {
      ...filters,
      [type === 'start' ? 'startDate' : 'endDate']: date,
      timeRange: 'custom' as const,
    };
    setFilters(newFilters);
    onFilterChange?.(newFilters);
  };

  const handleExportFormatChange = (option: { label: string; value: string | number }) => {
    const value = String(option.value);
    const newFilters = { ...filters, exportFormat: value as TimeFilters['exportFormat'] };
    setFilters(newFilters);
  };

  const handleExport = () => {
    onExport?.(filters);
  };

  const handleRealTimeToggle = () => {
    onRealTimeToggle?.(!isRealTime);
  };

  return (
    <Card style={StyleSheet.flatten([styles.card, style])}>
      <View style={styles.header}>
        <Icon name="search" size={20} color={semanticColors.primary} />
        <Text variant="h3" style={styles.title}>
          Filtros y Exportación
        </Text>
      </View>

      <View style={styles.content}>
        {/* Selector de rango de tiempo */}
        <View style={styles.section}>
          <Text variant="label" style={styles.sectionTitle}>
            Rango de Tiempo
          </Text>
          <Select
            options={timeRangeOptions}
            value={filters.timeRange}
            onSelect={handleTimeRangeChange}
            placeholder="Seleccionar rango"
          />
        </View>

        {/* Fechas personalizadas */}
        {filters.timeRange === 'custom' && (
          <View style={styles.section}>
            <Text variant="label" style={styles.sectionTitle}>
              Fechas Personalizadas
            </Text>
            <View style={styles.dateRow}>
              <View style={styles.dateField}>
                <Text variant="caption" style={styles.dateLabel}>
                  Desde
                </Text>
                <Button
                  variant="outline"
                  size="sm"
                  onPress={() => setShowStartDatePicker(true)}
                  style={styles.dateButton}
                >
                  {filters.startDate.toLocaleDateString()}
                </Button>
              </View>
              <View style={styles.dateField}>
                <Text variant="caption" style={styles.dateLabel}>
                  Hasta
                </Text>
                <Button
                  variant="outline"
                  size="sm"
                  onPress={() => setShowEndDatePicker(true)}
                  style={styles.dateButton}
                >
                  {filters.endDate.toLocaleDateString()}
                </Button>
              </View>
            </View>
          </View>
        )}

        {/* Toggle tiempo real */}
        <View style={styles.section}>
          <View style={styles.realTimeRow}>
            <View style={styles.realTimeInfo}>
              <Text variant="label" style={styles.sectionTitle}>
                Tiempo Real
              </Text>
              <Text variant="caption" color={semanticColors.textSecondary}>
                Actualización automática de datos
              </Text>
            </View>
            <Button
              variant={isRealTime ? 'primary' : 'outline'}
              size="sm"
              onPress={handleRealTimeToggle}
              style={styles.realTimeButton}
            >
              {isRealTime ? 'Activado' : 'Desactivado'}
            </Button>
          </View>
        </View>

        {/* Formato de exportación */}
        <View style={styles.section}>
          <Text variant="label" style={styles.sectionTitle}>
            Formato de Exportación
          </Text>
          <Select
            options={exportFormatOptions}
            value={filters.exportFormat}
            onSelect={handleExportFormatChange}
            placeholder="Seleccionar formato"
          />
        </View>

        {/* Botones de acción */}
        <View style={styles.actions}>
          <Button
            variant="primary"
            onPress={handleExport}
            style={styles.exportButton}
            leftIcon={<Icon name="download" size={16} color={semanticColors.surface} />}
          >
            Exportar Datos
          </Button>
        </View>
      </View>

      {/* Date Pickers */}
      {showStartDatePicker && (
        <DatePicker
          value={filters.startDate}
          onDateChange={(date) => handleDateChange(date, 'start')}
          onClose={() => setShowStartDatePicker(false)}
          maximumDate={filters.endDate}
        />
      )}
      
      {showEndDatePicker && (
        <DatePicker
          value={filters.endDate}
          onDateChange={(date) => handleDateChange(date, 'end')}
          onClose={() => setShowEndDatePicker(false)}
          minimumDate={filters.startDate}
          maximumDate={new Date()}
        />
      )}
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    marginBottom: spacing.lg,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.md,
  },
  title: {
    marginLeft: spacing.sm,
    color: semanticColors.textPrimary,
  },
  content: {
    gap: spacing.md,
  },
  section: {
    gap: spacing.sm,
  },
  sectionTitle: {
    color: semanticColors.textPrimary,
    fontWeight: '600',
  },
  dateRow: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  dateField: {
    flex: 1,
    gap: spacing.xs,
  },
  dateLabel: {
    color: semanticColors.textSecondary,
  },
  dateButton: {
    justifyContent: 'center',
  },
  realTimeRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  realTimeInfo: {
    flex: 1,
    gap: spacing.xs,
  },
  realTimeButton: {
    minWidth: 100,
  },
  actions: {
    marginTop: spacing.sm,
  },
  exportButton: {
    width: '100%',
  },
});