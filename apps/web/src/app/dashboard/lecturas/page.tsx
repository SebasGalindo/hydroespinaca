'use client';

import React, { useState, useMemo } from 'react';
import { useReadingsStore } from '@hydroespinaca/shared';
import SummaryTable from '@/components/dashboard/lecturas/SummaryTable';
import ReadingsTable from '@/components/dashboard/lecturas/ReadingsTable';
import FilterControls from '@/components/dashboard/lecturas/FilterControls';
import PageLayout from '@/components/layout/PageLayout';
import Section from '@/components/ui/Section';

// Re-export types for backward compatibility
export type { SensorSummary, IndividualReading } from '@hydroespinaca/shared';

export default function LecturasPage() {
  // Local filter state
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedTime, setSelectedTime] = useState('');
  const [selectedSensor, setSelectedSensor] = useState('');
  const [filterValue, setFilterValue] = useState('');

  // Get data from store
  const { sensorSummary, individualReadings } = useReadingsStore();

  // Filter logic moved to component
  const filteredReadings = useMemo(() => {
    let filtered = individualReadings;

    if (selectedDate) {
      filtered = filtered.filter(reading => 
        reading.fecha.startsWith(selectedDate)
      );
    }

    if (selectedTime) {
      filtered = filtered.filter(reading => 
        reading.fecha.includes(selectedTime)
      );
    }

    if (selectedSensor) {
      filtered = filtered.filter(reading => 
        reading.sensor === selectedSensor
      );
    }

    if (filterValue) {
      filtered = filtered.filter(reading => 
        reading.valor.toString().toLowerCase().includes(filterValue.toLowerCase())
      );
    }

    return filtered;
  }, [individualReadings, selectedDate, selectedTime, selectedSensor, filterValue]);

  return (
    <PageLayout
      title="Lecturas de Sensores"
      subtitle="Monitoreo de las variables del cultivo"
      maxWidth="xl"
    >
      <Section 
        title="Resumen de los últimos 10 minutos"
        spacing="md"
      >
        <SummaryTable sensorSummary={sensorSummary} />
      </Section>

      <Section 
        title="Filtros"
        spacing="md"
      >
        <FilterControls 
          selectedDate={selectedDate}
          setSelectedDate={setSelectedDate}
          selectedTime={selectedTime}
          setSelectedTime={setSelectedTime}
          selectedSensor={selectedSensor}
          setSelectedSensor={setSelectedSensor}
          filterValue={filterValue}
          setFilterValue={setFilterValue}
        />
      </Section>

      <Section 
        title="Lecturas Individuales"
        spacing="md"
      >
        <ReadingsTable individualReadings={filteredReadings} />
      </Section>
    </PageLayout>
  );
}