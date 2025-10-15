'use client';

import React from 'react';
import DashboardMonitoreo from '@/components/dashboard/monitoreo/DashboardMonitoreo';
import PageLayout from '@/components/layout/PageLayout';

export default function DashboardMonitoreoPage() {
  return (
    <PageLayout
      title="Analisis de Datos Hidropónicos"
      subtitle="Explora el entorno del invernadero a través de gráficos y diagramas interactivos. Selecciona variables y aplica filtros para analizar puntos de datos específicos."
      maxWidth="full"
    >
      <DashboardMonitoreo />
    </PageLayout>
  );
}