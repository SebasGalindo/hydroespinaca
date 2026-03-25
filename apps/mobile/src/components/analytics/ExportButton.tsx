import React, { useCallback, useState } from 'react';
import { StyleSheet, Alert } from 'react-native';
import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';
import { colors, spacing } from '@hydroespinaca/shared';
import type { EnvironmentalVariableAggregate } from '@hydroespinaca/shared';
import { Button } from '../atoms/Button';
import { Icon } from '../atoms/Icon';
import type { ActuatorActivity } from '../../utils/actuatorMapper';
import type { FilterState } from '../../utils/analyticsFilters';

interface ExportButtonProps {
  environmentalData: EnvironmentalVariableAggregate[];
  actuatorData: ActuatorActivity[];
  filters: FilterState;
  activeTab: 'environmental' | 'actuators';
}

function buildCsv(
  envData: EnvironmentalVariableAggregate[],
  actData: ActuatorActivity[],
  tab: 'environmental' | 'actuators',
): string {
  if (tab === 'environmental') {
    const rows: string[] = ['Variable,Promedio,Mínimo,Máximo,Lecturas'];
    envData.forEach((v) => {
      rows.push(
        `"${v.variableName}",${v.summary.avg},${v.summary.min},${v.summary.max},${v.summary.count}`,
      );
    });
    return rows.join('\n');
  }

  const rows: string[] = ['Actuador,Duración (min),Activaciones,Proporción (%)'];
  actData.forEach((a) => {
    rows.push(
      `"${a.actuatorName}",${a.totalDuration.toFixed(2)},${a.activationCount},${a.proportion.toFixed(1)}`,
    );
  });
  return rows.join('\n');
}

export function ExportButton({
  environmentalData,
  actuatorData,
  filters,
  activeTab,
}: ExportButtonProps): React.ReactElement {
  const [exporting, setExporting] = useState(false);

  const handleExport = useCallback(async () => {
    try {
      setExporting(true);

      const csv = buildCsv(environmentalData, actuatorData, activeTab);
      const tabLabel = activeTab === 'environmental' ? 'ambiental' : 'actuadores';
      const timestamp = new Date().toISOString().split('T')[0];
      const filename = `analytics_${tabLabel}_${timestamp}.csv`;

      const file = new File(Paths.cache, filename);
      file.create();
      file.write(csv);

      const canShare = await Sharing.isAvailableAsync();
      if (canShare) {
        await Sharing.shareAsync(file.uri, {
          mimeType: 'text/csv',
          dialogTitle: `Exportar datos de ${tabLabel}`,
          UTI: 'public.comma-separated-values-text',
        });
      } else {
        Alert.alert('No disponible', 'La función de compartir no está disponible en este dispositivo.');
      }
    } catch (error) {
      console.error('Export error:', error);
      Alert.alert('Error', 'No se pudo exportar los datos. Intenta de nuevo.');
    } finally {
      setExporting(false);
    }
  }, [environmentalData, actuatorData, activeTab]);

  const hasData =
    activeTab === 'environmental'
      ? environmentalData.length > 0
      : actuatorData.length > 0;

  return (
    <Button
      variant="outline"
      size="sm"
      onPress={handleExport}
      disabled={!hasData || exporting}
      loading={exporting}
      style={styles.button}
      leftIcon={<Icon name="share" size={16} color={colors.hidro[600]} />}
    >
      Exportar
    </Button>
  );
}

const styles = StyleSheet.create({
  button: {
    minWidth: 100,
  },
});
