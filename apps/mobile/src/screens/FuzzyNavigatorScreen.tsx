import React, { useState } from 'react';
import { FuzzySystemsScreen } from './FuzzySystemsScreen';
import { FuzzySystemDetailScreen } from './FuzzySystemDetailScreen';

type NavigationState = 
  | { screen: 'list' }
  | { screen: 'detail'; systemId: string };

export const FuzzyNavigatorScreen: React.FC = () => {
  const [navigationState, setNavigationState] = useState<NavigationState>({ screen: 'list' });

  const handleNavigateToDetail = (systemId: string) => {
    setNavigationState({ screen: 'detail', systemId });
  };

  const handleNavigateBack = () => {
    setNavigationState({ screen: 'list' });
  };

  if (navigationState.screen === 'detail') {
    return (
      <FuzzySystemDetailScreen
        systemId={navigationState.systemId}
        onNavigateBack={handleNavigateBack}
      />
    );
  }

  return (
    <FuzzySystemsScreen
      onNavigateToDetail={handleNavigateToDetail}
    />
  );
};