'use client';

import React from 'react';
import ActuadoresConfigForm from '@/components/dashboard/actuadores-config/ActuadoresConfigForm';
import PageLayout from '@/components/layout/PageLayout';

export default function ActuadoresConfigPage() {
  return (
    <PageLayout 
      title="Configuración de Actuadores"
      subtitle="Gestiona y controla los actuadores del invernadero"
      maxWidth="xl"
    >
      <ActuadoresConfigForm />
    </PageLayout>
  );
}