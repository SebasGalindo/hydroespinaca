'use client';

import React from 'react';
import NewVariableForm from '@/components/dashboard/nueva-variable/NewVariableForm';
import PageLayout from '@/components/layout/PageLayout';

export default function NewVariablePage() {
  return (
    <PageLayout 
      title="Nueva Variable Manual"
      subtitle="Define los parámetros para la variable que será controlada manualmente"
      maxWidth="md"
    >
      <NewVariableForm />
    </PageLayout>
  );
}