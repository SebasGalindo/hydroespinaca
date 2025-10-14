import React, { useState } from 'react';
import { View, StyleSheet, SafeAreaView, Alert } from 'react-native';
import { semanticColors, spacing, colors } from '@hidroespinaca/shared';
import { Text, Button } from '../components/atoms';
import { NewVariableForm, VariableFormData } from '../components/organisms/NewVariableForm';

export function CrearVariableScreen(): React.ReactElement {
  const [isFormVisible, setIsFormVisible] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleOpenForm = () => {
    setIsFormVisible(true);
  };

  const handleCloseForm = () => {
    setIsFormVisible(false);
  };

  const handleSaveVariable = async (data: VariableFormData) => {
    setIsLoading(true);
    
    try {
      // Aquí iría la lógica para guardar la variable
      // Por ahora simulo una llamada a API
      await new Promise(resolve => setTimeout(() => resolve(undefined), 1500));
      
      console.log('Variable guardada:', data);
      
      Alert.alert(
        'Variable Creada',
        `La variable "${data.name}" ha sido creada exitosamente.`,
        [
          {
            text: 'OK',
            onPress: () => {
              setIsFormVisible(false);
              setIsLoading(false);
            }
          }
        ]
      );
    } catch (error) {
      setIsLoading(false);
      Alert.alert(
        'Error',
        'Hubo un problema al crear la variable. Por favor, inténtalo de nuevo.',
        [{ text: 'OK' }]
      );
    }
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.content}>
        <Text variant="h1" color={semanticColors.primary} style={styles.title}>
          Crear Variable
        </Text>
        <Text variant="body" color={semanticColors.textSecondary} style={styles.description}>
          Configuración de nuevas variables del sistema
        </Text>
        
        <View style={styles.buttonContainer}>
          <Button
            variant="primary"
            onPress={handleOpenForm}
            style={styles.createButton}
          >
            Nueva Variable Manual
          </Button>
        </View>
      </View>

      <NewVariableForm
        visible={isFormVisible}
        onClose={handleCloseForm}
        onSave={handleSaveVariable}
        loading={isLoading}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.hidro.bgLight,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: spacing.lg,
  },
  title: {
    marginBottom: spacing.md,
    textAlign: 'center',
  },
  description: {
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: spacing.xl,
  },
  buttonContainer: {
    width: '100%',
    maxWidth: 300,
    alignItems: 'center',
  },
  createButton: {
    width: '100%',
    paddingVertical: spacing.md,
  },
});