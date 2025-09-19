import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '../navigation/AppNavigator';

type Props = NativeStackScreenProps<RootStackParamList, 'Home'>;

export const HomeScreen: React.FC<Props> = ({ navigation }) => {
  const handleLogout = () => {
    // TODO: Implementar logout real
    navigation.navigate('Public');
  };

  return (
    <ScrollView style={styles.container}>
      <View style={styles.content}>
        <View style={styles.header}>
          <Text style={styles.title}>Panel de Control</Text>
          <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
            <Text style={styles.logoutButtonText}>Cerrar Sesión</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Estado del Sistema</Text>
          
          <View style={styles.statusGrid}>
            <View style={[styles.statusCard, styles.statusOk]}>
              <Text style={styles.statusCardTitle}>Sensores</Text>
              <Text style={styles.statusCardStatus}>✓ Funcionando</Text>
              <Text style={styles.statusCardDetail}>Última actualización: hace 2 min</Text>
            </View>
            
            <View style={[styles.statusCard, styles.statusOk]}>
              <Text style={styles.statusCardTitle}>Actuadores</Text>
              <Text style={styles.statusCardStatus}>✓ Funcionando</Text>
              <Text style={styles.statusCardDetail}>Bomba de nutrientes activa</Text>
            </View>
            
            <View style={[styles.statusCard, styles.statusOk]}>
              <Text style={styles.statusCardTitle}>Conectividad</Text>
              <Text style={styles.statusCardStatus}>✓ En línea</Text>
              <Text style={styles.statusCardDetail}>MQTT conectado</Text>
            </View>
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Lecturas Recientes</Text>
          
          <View style={styles.readingsContainer}>
            {[
              { name: 'Temperatura', value: '22.5°C' },
              { name: 'Humedad', value: '65%' },
              { name: 'pH', value: '6.8' },
              { name: 'Nutrientes', value: '850 ppm' },
            ].map((reading, index) => (
              <View key={index} style={styles.readingItem}>
                <Text style={styles.readingName}>{reading.name}</Text>
                <Text style={styles.readingValue}>{reading.value}</Text>
              </View>
            ))}
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Acciones Rápidas</Text>
          
          <View style={styles.actionsGrid}>
            <TouchableOpacity style={[styles.actionButton, styles.primaryAction]}>
              <Text style={styles.actionButtonText}>Activar Riego</Text>
            </TouchableOpacity>
            
            <TouchableOpacity style={[styles.actionButton, styles.primaryAction]}>
              <Text style={styles.actionButtonText}>Añadir Nutrientes</Text>
            </TouchableOpacity>
            
            <TouchableOpacity style={[styles.actionButton, styles.secondaryAction]}>
              <Text style={styles.secondaryActionText}>Ver Histórico</Text>
            </TouchableOpacity>
            
            <TouchableOpacity style={[styles.actionButton, styles.secondaryAction]}>
              <Text style={styles.secondaryActionText}>Configurar Alertas</Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  content: {
    padding: 16,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 24,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#2E7D32',
  },
  logoutButton: {
    backgroundColor: '#f44336',
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
  },
  logoutButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  section: {
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 16,
  },
  statusGrid: {
    gap: 12,
  },
  statusCard: {
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 8,
    borderLeftWidth: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  statusOk: {
    borderLeftColor: '#4CAF50',
  },
  statusCardTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  statusCardStatus: {
    fontSize: 14,
    color: '#4CAF50',
    fontWeight: '600',
    marginBottom: 4,
  },
  statusCardDetail: {
    fontSize: 12,
    color: '#666',
  },
  readingsContainer: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  readingItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  readingName: {
    fontSize: 14,
    color: '#666',
  },
  readingValue: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  actionsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 12,
  },
  actionButton: {
    flex: 1,
    minWidth: '45%',
    paddingVertical: 16,
    paddingHorizontal: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  primaryAction: {
    backgroundColor: '#4CAF50',
  },
  secondaryAction: {
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: '#ddd',
  },
  actionButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'center',
  },
  secondaryActionText: {
    color: '#666',
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'center',
  },
});