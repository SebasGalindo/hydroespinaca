'use client';

import React from 'react';
import SensorsConfigForm from '@/components/dashboard/sensores-config/SensorsConfigForm';
import PageLayout from '@/components/layout/PageLayout';

export default function SensoresConfigPage() {
  return (
    <PageLayout 
      title="Configuración de Sensores"
      subtitle="Gestiona y configura los sensores del sistema hidropónico"
      maxWidth="xl"
    >
      <SensorsConfigForm />
    </PageLayout>
  );
}