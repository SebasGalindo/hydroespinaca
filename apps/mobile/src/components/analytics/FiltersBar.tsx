import React, { useState, useCallback, useMemo } from 'react';
import {
  View,
  TouchableOpacity,
  StyleSheet,
  Platform,
} from 'react-native';
import DateTimePicker, { DateTimePickerEvent } from '@react-native-community/datetimepicker';
import { colors, spacing, borderRadius, semanticColors, typography } from '@hydroespinaca/shared';
import { Text } from '../atoms/Text';
import { Icon } from '../atoms/Icon';
import { Button } from '../atoms/Button';
import {
  type ViewMode,
  type FilterState,
  VIEW_LABELS,
  getSuggestedRanges,
  adjustDateRangeForView,
  validateFilters,
  getDaysDifference,
} from '../../utils/analyticsFilters';

interface FiltersBarProps {
  currentFilters: FilterState;
  onApplyFilters: (filters: FilterState) => void;
  isLoading?: boolean;
}

type PickerTarget = 'from' | 'to' | null;

const VIEW_MODE_OPTIONS: ViewMode[] = ['hourly', 'daily', 'weekly', 'monthly'];

export function FiltersBar({
  currentFilters,
  onApplyFilters,
  isLoading = false,
}: FiltersBarProps): React.ReactElement {
  const [dateRange, setDateRange] = useState(currentFilters.dateRange);
  const [viewMode, setViewMode] = useState(currentFilters.viewMode);
  const [showPicker, setShowPicker] = useState<PickerTarget>(null);

  const validation = useMemo(
    () => validateFilters(dateRange.from, dateRange.to, viewMode),
    [dateRange, viewMode],
  );

  const hasChanges = useMemo(() => {
    return (
      dateRange.from !== currentFilters.dateRange.from ||
      dateRange.to !== currentFilters.dateRange.to ||
      viewMode !== currentFilters.viewMode
    );
  }, [dateRange, viewMode, currentFilters]);

  const suggestedRanges = useMemo(() => getSuggestedRanges(viewMode), [viewMode]);
  const daysDiff = useMemo(
    () => getDaysDifference(dateRange.from, dateRange.to),
    [dateRange],
  );

  const handleDateChange = useCallback(
    (_event: DateTimePickerEvent, selectedDate?: Date) => {
      if (Platform.OS === 'android') setShowPicker(null);
      if (!selectedDate || !showPicker) return;

      const dateStr = selectedDate.toISOString().split('T')[0]!;
      const newRange = { ...dateRange };

      if (showPicker === 'from') {
        newRange.from = dateStr;
        // Ensure from <= to
        if (dateStr > dateRange.to) newRange.to = dateStr;
      } else {
        newRange.to = dateStr;
        if (dateStr < dateRange.from) newRange.from = dateStr;
      }

      setDateRange(newRange);
      if (Platform.OS === 'ios') setShowPicker(null);
    },
    [showPicker, dateRange],
  );

  const handleViewModeChange = useCallback(
    (mode: ViewMode) => {
      setViewMode(mode);
      const adjusted = adjustDateRangeForView(dateRange.from, dateRange.to, mode);
      setDateRange(adjusted);
    },
    [dateRange],
  );

  const handleQuickSelect = useCallback(
    (days: number) => {
      const today = new Date().toISOString().split('T')[0]!;
      if (days <= 0) {
        setDateRange({ from: today, to: today });
        setViewMode('hourly');
      } else {
        const from = new Date(Date.now() - days * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0]!;
        setDateRange({ from, to: today });
      }
    },
    [],
  );

  const handleApply = useCallback(() => {
    if (!validation.valid || isLoading) return;
    onApplyFilters({ dateRange, viewMode });
  }, [dateRange, viewMode, validation, isLoading, onApplyFilters]);

  const formatDateLabel = (dateStr: string): string => {
    const d = new Date(dateStr + 'T12:00:00');
    return d.toLocaleDateString('es-CO', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  return (
    <View style={styles.container}>
      {/* Date Range Row */}
      <View style={styles.dateRow}>
        <TouchableOpacity
          style={styles.datePicker}
          onPress={() => setShowPicker('from')}
          disabled={isLoading}
        >
          <Icon name="calendar" size={16} color={semanticColors.textSecondary} />
          <Text variant="caption" color={semanticColors.textPrimary}>
            {formatDateLabel(dateRange.from)}
          </Text>
        </TouchableOpacity>

        <Text variant="caption" color={semanticColors.textTertiary}>→</Text>

        <TouchableOpacity
          style={styles.datePicker}
          onPress={() => setShowPicker('to')}
          disabled={isLoading}
        >
          <Icon name="calendar" size={16} color={semanticColors.textSecondary} />
          <Text variant="caption" color={semanticColors.textPrimary}>
            {formatDateLabel(dateRange.to)}
          </Text>
        </TouchableOpacity>
      </View>

      {/* View Mode Chips */}
      <View style={styles.chipsRow}>
        {VIEW_MODE_OPTIONS.map((mode) => (
          <TouchableOpacity
            key={mode}
            style={[styles.chip, viewMode === mode && styles.chipActive]}
            onPress={() => handleViewModeChange(mode)}
            disabled={isLoading}
          >
            <Text
              variant="caption"
              color={viewMode === mode ? colors.white : semanticColors.textSecondary}
              style={styles.chipText}
            >
              {VIEW_LABELS[mode]}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Quick select + Apply */}
      <View style={styles.actionsRow}>
        <View style={styles.quickSelects}>
          {suggestedRanges.map((range) => (
            <TouchableOpacity
              key={range.days}
              style={styles.quickChip}
              onPress={() => handleQuickSelect(range.days)}
              disabled={isLoading}
            >
              <Text variant="caption" color={semanticColors.textSecondary}>
                {range.label}
              </Text>
            </TouchableOpacity>
          ))}
        </View>

        <Button
          variant="primary"
          size="sm"
          onPress={handleApply}
          disabled={!hasChanges || !validation.valid || isLoading}
          loading={isLoading}
          style={styles.applyButton}
        >
          Aplicar
        </Button>
      </View>

      {/* Info row */}
      <View style={styles.infoRow}>
        <Text variant="caption" color={semanticColors.textTertiary}>
          {daysDiff} día{daysDiff !== 1 ? 's' : ''} seleccionado{daysDiff !== 1 ? 's' : ''}
        </Text>
        {hasChanges && validation.valid && (
          <Text variant="caption" color={colors.info[600]} style={styles.pendingText}>
            • Cambios pendientes
          </Text>
        )}
      </View>

      {/* Validation error */}
      {!validation.valid && validation.message && (
        <View style={styles.errorBanner}>
          <Icon name="warning" size={14} color={colors.warning[700]} />
          <Text variant="caption" color={colors.warning[700]} style={styles.errorText}>
            {validation.message}
          </Text>
        </View>
      )}

      {/* Native date pickers */}
      {showPicker && (
        <DateTimePicker
          value={new Date(
            (showPicker === 'from' ? dateRange.from : dateRange.to) + 'T12:00:00',
          )}
          mode="date"
          display={Platform.OS === 'ios' ? 'spinner' : 'default'}
          onChange={handleDateChange}
          maximumDate={new Date()}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: semanticColors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    marginHorizontal: spacing.md,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.gray[200],
    gap: spacing.sm,
  },
  dateRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  datePicker: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    borderWidth: 1,
    borderColor: colors.gray[200],
    borderRadius: borderRadius.md,
    backgroundColor: colors.gray[50],
  },
  chipsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  chip: {
    paddingVertical: 6,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.full,
    backgroundColor: colors.gray[100],
  },
  chipActive: {
    backgroundColor: colors.hidro[600],
  },
  chipText: {
    fontSize: typography.fontSize.xxs,
  },
  actionsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  quickSelects: {
    flexDirection: 'row',
    gap: spacing.xs,
  },
  quickChip: {
    paddingVertical: 4,
    paddingHorizontal: spacing.sm,
    borderRadius: borderRadius.md,
    backgroundColor: colors.gray[100],
  },
  applyButton: {
    minWidth: 80,
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  pendingText: {
    fontWeight: typography.fontWeight.medium,
  },
  errorBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    padding: spacing.sm,
    backgroundColor: colors.warning[50],
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.warning[200],
  },
  errorText: {
    flex: 1,
  },
});
