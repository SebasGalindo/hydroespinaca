import React, { useState } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SafeAreaView, View, StyleSheet } from 'react-native';
import { semanticColors } from '@hydroespinaca/shared';
import { 
  LoginScreen,
  InfoMainScreen, 
  LecturasScreen, 
  DashboardScreen, 
  LogicaFuzzyScreen, 
  CrearVariableScreen,
  ListaVariablesScreen,
  ListaSensoresScreen,
  ListaActuadoresScreen,
  AjustesScreen,
  HistorialScreen,
  EstadoSistemaScreen
} from '../screens';
import { FuzzyNavigatorScreen } from '../screens/FuzzyNavigatorScreen';
import { BottomTabNavigator } from '../components/organisms/BottomTabNavigator';
import { Header } from '../components/organisms/Header';

const Stack = createNativeStackNavigator();

// Mapeo de IDs de navegación a componentes de pantalla
const screenComponents = {
  'info-main': InfoMainScreen,
  'lecturas': LecturasScreen,
  'dashboard': DashboardScreen,
  'logica-fuzzy': FuzzyNavigatorScreen,
  'crear-variable': CrearVariableScreen,
  'lista-variables': ListaVariablesScreen,
  'lista-sensores': ListaSensoresScreen,
  'lista-actuadores': ListaActuadoresScreen,
  'ajustes': AjustesScreen,
  'historial': HistorialScreen,
  'estado-sistema': EstadoSistemaScreen,
};

// Componente principal que contiene las pantallas con bottom navigation
function MainTabsScreen(): React.ReactElement {
  const [activeTab, setActiveTab] = useState('info-main');

  const handleTabPress = (tabId: string, href: string) => {
    if (tabId !== 'mas') {
      setActiveTab(tabId);
    }
  };

  const renderCurrentScreen = () => {
    const ScreenComponent = screenComponents[activeTab as keyof typeof screenComponents];
    return ScreenComponent ? <ScreenComponent /> : <InfoMainScreen />;
  };

  return (
    <SafeAreaView style={styles.container}>
      <Header />
      <View style={styles.content}>
        {renderCurrentScreen()}
      </View>
      <BottomTabNavigator
        activeTab={activeTab}
        onTabPress={handleTabPress}
        testID="main-bottom-navigator"
      />
    </SafeAreaView>
  );
}

export function AppNavigator(): React.ReactElement {
  return (
    <NavigationContainer>
      <Stack.Navigator
        initialRouteName="Login"
        screenOptions={{
          headerShown: false,
        }}
      >
        <Stack.Screen 
          name="Login" 
          component={LoginScreen} 
        />
        <Stack.Screen 
          name="MainTabs" 
          component={MainTabsScreen} 
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  content: {
    flex: 1,
  },
});