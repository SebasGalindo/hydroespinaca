import React from 'react';
import { View, StyleSheet } from 'react-native';
import { semanticColors, spacing } from '@hidroespinaca/shared';
import type { SimpleFuzzySystem } from '@hidroespinaca/shared';
import { Text } from '../atoms/Text';
import { Badge } from '../atoms/Badge';
import { Card } from './Card';

export interface FuzzySystemCardProps {
  system: SimpleFuzzySystem;
  onPress?: (system: SimpleFuzzySystem) => void;
}

export const FuzzySystemCard: React.FC<FuzzySystemCardProps> = ({
  system,
  onPress,
}) => {
  const handlePress = () => {
    onPress?.(system);
  };

  // Extract names from variables and rules
  const variableNames = [
    ...(system.input_variables?.map(v => v.name) || []),
    ...(system.output_variables?.map(v => v.name) || [])
  ];
  
  const ruleNames = system.rules?.map(r => r.name) || [];
  
  // Extract unique routine names from rules
  const routineNames = Array.from(
    new Set(system.rules?.map(r => r.routine?.routine_name).filter(Boolean) || [])
  );

  return (
    <Card onPress={handlePress} style={styles.card}>
      <View style={styles.header}>
        <View style={styles.titleContainer}>
          <Text variant="h3" style={styles.title}>
            {system.name}
          </Text>
          <Badge 
            variant={system.status === 'ACTIVE' ? 'success' : 'default'}
            size="sm"
          >
            {system.status === 'ACTIVE' ? 'Activo' : 'Inactivo'}
          </Badge>
        </View>
        
        {/* Defuzzification and Operators */}
        <View style={styles.methodContainer}>
          <Text variant="body" color={semanticColors.textMuted}>
            Defuzzificación: {system.defuzzification_method || 'No definido'}
          </Text>
          {system.operators && (
            <Text variant="body" color={semanticColors.textMuted} style={styles.operators}>
              AND: {system.operators.and || 'No definido'} | OR: {system.operators.or || 'No definido'}
            </Text>
          )}
        </View>
      </View>

      {/* Detailed Information Sections */}
      <View style={styles.detailsContainer}>
        {/* Rules Section */}
        <View style={styles.detailSection}>
          <Text variant="body" style={styles.sectionTitle}>
            Reglas ({ruleNames.length})
          </Text>
          <View style={styles.sectionContent}>
            {ruleNames.length > 0 ? (
              <>
                {ruleNames.slice(0, 2).map((rule, index) => (
                  <Text key={index} variant="body" color={semanticColors.textSecondary} style={styles.listItem}>
                    • {rule}
                  </Text>
                ))}
                {ruleNames.length > 2 && (
                  <Text variant="body" color={semanticColors.textMuted} style={styles.moreText}>
                    +{ruleNames.length - 2} más
                  </Text>
                )}
              </>
            ) : (
              <Text variant="body" color={semanticColors.textMuted} style={styles.emptyText}>
                Sin reglas
              </Text>
            )}
          </View>
        </View>

        {/* Variables Section */}
         <View style={styles.detailSection}>
           <Text variant="body" style={styles.sectionTitle}>
             Variables ({variableNames.length})
           </Text>
           <View style={styles.sectionContent}>
             {variableNames.length > 0 ? (
               <>
                 {variableNames.slice(0, 2).map((variable, index) => (
                   <Text key={index} variant="body" color={semanticColors.textSecondary} style={styles.listItem}>
                     • {variable}
                   </Text>
                 ))}
                 {variableNames.length > 2 && (
                   <Text variant="body" color={semanticColors.textMuted} style={styles.moreText}>
                     +{variableNames.length - 2} más
                   </Text>
                 )}
               </>
             ) : (
               <Text variant="body" color={semanticColors.textMuted} style={styles.emptyText}>
                 Sin variables
               </Text>
             )}
           </View>
         </View>

         {/* Routines Section */}
         <View style={styles.detailSection}>
           <Text variant="body" style={styles.sectionTitle}>
             Rutinas ({routineNames.length})
           </Text>
           <View style={styles.sectionContent}>
             {routineNames.length > 0 ? (
               <>
                 {routineNames.slice(0, 2).map((routine, index) => (
                   <Text key={index} variant="body" color={semanticColors.textSecondary} style={styles.listItem}>
                     • {routine}
                   </Text>
                 ))}
                 {routineNames.length > 2 && (
                   <Text variant="body" color={semanticColors.textMuted} style={styles.moreText}>
                     +{routineNames.length - 2} más
                   </Text>
                 )}
               </>
             ) : (
               <Text variant="body" color={semanticColors.textMuted} style={styles.emptyText}>
                 Sin rutinas
               </Text>
             )}
           </View>
         </View>
       </View>
     </Card>
   );
 };

const styles = StyleSheet.create({
  card: {
    marginBottom: spacing.md,
  },
  header: {
    marginBottom: spacing.md,
  },
  titleContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: spacing.sm,
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  title: {
    flex: 1,
    marginRight: spacing.sm,
    minWidth: 0, // Allow text to shrink
  },
  methodContainer: {
    gap: spacing.sm,
  },
  operators: {
    marginTop: spacing.xs,
    fontSize: 14,
  },
  detailsContainer: {
    gap: spacing.md,
  },
  detailSection: {
    backgroundColor: semanticColors.backgroundSecondary,
    padding: spacing.sm,
    borderRadius: 8,
  },
  sectionTitle: {
    fontWeight: '600',
    color: semanticColors.textPrimary,
    marginBottom: spacing.sm,
    fontSize: 16,
  },
  sectionContent: {
    gap: spacing.sm,
  },
  listItem: {
    lineHeight: 20,
    fontSize: 14,
  },
  moreText: {
    fontStyle: 'italic',
    fontSize: 14,
  },
  emptyText: {
    fontStyle: 'italic',
    fontSize: 14,
  },
});