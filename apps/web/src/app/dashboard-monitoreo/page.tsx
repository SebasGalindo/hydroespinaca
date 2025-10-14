'use client';

import React from 'react';
import DashboardMonitoreo from '@/components/dashboard/monitoreo/DashboardMonitoreo';
import PageLayout from '@/components/layout/PageLayout';

export default function DashboardMonitoreoPage() {
  return (
    <PageLayout 
      title="Dashboard de Monitoreo"
      subtitle="Análisis en tiempo real de variables del cultivo hidropónico"
      maxWidth="full"
    >
      <DashboardMonitoreo />
    </PageLayout>
  );
}