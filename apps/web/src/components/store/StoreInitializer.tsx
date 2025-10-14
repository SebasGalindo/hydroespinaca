'use client';

import { useEffect, useState } from 'react';
import { useVariableStore, useActuatorStore, useSensorStore, useReadingsStore } from '@hidroespinaca/shared';

export function StoreInitializer() {
  const [isClient, setIsClient] = useState(false);
  
  useEffect(() => {
    setIsClient(true);
  }, []);

  const initializeVariables = useVariableStore(state => state.initializeVariables);
  const initializeActuadores = useActuatorStore(state => state.initializeActuadores);
  const generateMockData = useSensorStore(state => state.generateMockData);
  const initializeSystemComponents = useSensorStore(state => state.initializeSystemComponents);
  const initializeReadings = useReadingsStore(state => state.initializeReadings);

  useEffect(() => {
    if (isClient) {
      console.log('Initializing stores...');
      initializeVariables();
      initializeActuadores();
      generateMockData();
      initializeSystemComponents();
      initializeReadings();
    }
  }, [isClient, initializeVariables, initializeActuadores, generateMockData, initializeSystemComponents, initializeReadings]);

  return null;
}