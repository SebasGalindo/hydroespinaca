import React from 'react';
import { View, StyleSheet } from 'react-native';
import { ControllerStatus } from './ControllerStatus';
import { spacing } from '@hydroespinaca/shared';
import type { JobStatus, Stats, InternalRoutine } from '@hydroespinaca/shared';

export interface ControllerSectionProps {
  lastUpdateTimestamp: string;
  jobStatus: JobStatus;
  stats: Stats;
  internalRoutines: InternalRoutine[];
}

export function ControllerSection({
  lastUpdateTimestamp,
  jobStatus,
  stats,
  internalRoutines,
}: ControllerSectionProps): React.ReactElement {
  const timeSinceUpdate = Math.floor(
    (new Date().getTime() - new Date(lastUpdateTimestamp).getTime()) / 1000
  );

  return (
    <View style={styles.section}>
      <ControllerStatus
        timeSinceUpdate={timeSinceUpdate}
        jobStatus={jobStatus}
        stats={stats}
        internalRoutines={internalRoutines}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    paddingHorizontal: spacing.lg,
    marginBottom: spacing.lg,
  },
});
