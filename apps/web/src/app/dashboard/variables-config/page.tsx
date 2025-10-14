'use client';

import React from 'react';
import VariablesConfigForm from '@/components/dashboard/variables-config/VariablesConfigForm';
import PageLayout from '@/components/layout/PageLayout';

export default function VariablesConfigPage() {
  return (
    <PageLayout 
      title="Configuración de Variables"
      subtitle="Gestiona las variables del sistema de control"
      maxWidth="xl"
    >
      <VariablesConfigForm />
    </PageLayout>
  );
}