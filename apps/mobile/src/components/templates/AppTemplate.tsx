import React, { useState } from 'react';
import { View, StyleSheet, SafeAreaView } from 'react-native';
import { semanticColors } from '@hidroespinaca/shared';
import { Header } from '../organisms/Header';
import { BottomTabNavigator } from '../organisms/BottomTabNavigator';

interface AppTemplateProps {
  children: React.ReactNode;
  initialTab?: string;
  onTabChange?: (tabId: string) => void;
  testID?: string;
}

export function AppTemplate({
  children,
  initialTab = 'info-main',
  onTabChange,
  testID = 'app-template'
}: AppTemplateProps): React.ReactElement {
  const [activeTab, setActiveTab] = useState(initialTab);

  const handleTabPress = (tabId: string) => {
    setActiveTab(tabId);
    onTabChange?.(tabId);
  };

  return (
    <SafeAreaView style={styles.container} testID={testID}>
      {/* Header fijo en la parte superior */}
      <Header />
      
      {/* Contenido principal */}
      <View style={styles.content}>
        {children}
      </View>
      
      {/* Bottom Tab Navigator fijo en la parte inferior */}
      <BottomTabNavigator
        activeTab={activeTab}
        onTabPress={handleTabPress}
        testID={`${testID}-bottom-nav`}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
  content: {
    flex: 1,
    backgroundColor: semanticColors.background,
  },
});