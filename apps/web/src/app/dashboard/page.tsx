import React from 'react';
import VariableCard from '@/components/dashboard/VariableCard';
import ActionCard from '@/components/dashboard/ActionCard';
import PageLayout from '@/components/layout/PageLayout';

export default function DashboardPage() {
  return (
    <PageLayout 
      title="Información General Del Cultivo"
      subtitle="Variables Controladas Mediante IA"
      maxWidth="xl"
    >
          
      {/* Variables controladas por IA */}
      <section className="mb-8" aria-labelledby="ai-variables-heading">
        <h2 id="ai-variables-heading" className="sr-only">Variables controladas por IA</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-4">
              <VariableCard
                title="Temperatura"
                value="24°C"
                optimal="Óptima: 23-25°C"
                iconType="temperature"
                status="optimal"
              />
              <VariableCard
                title="Humedad"
                value="75%"
                optimal="Óptima: 70-80 %"
                iconType="humidity"
                status="optimal"
              />
              <VariableCard
                title="Nivel de pH"
                value="6.5"
                optimal="Óptima: 6.0-7.0"
                iconType="ph"
                status="optimal"
              />
              <VariableCard
                title="Luz Solar"
                value="85%"
                optimal="Óptima: 80-90%"
                iconType="sun"
                status="optimal"
              />
              <VariableCard
                title="Conductividad"
                value="1.2 mS/cm"
                optimal="Óptima: 1.0-1.5 mS/cm"
                iconType="electric"
                status="optimal"
              />
        </div>
      </section>

      {/* Variables controladas manualmente */}
      <section className="mb-8" aria-labelledby="manual-variables-heading">
        <h2 id="manual-variables-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Variables Controladas Manualmente
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <VariableCard
                title="Nivel de Agua"
                value="45 cm"
                optimal="Óptimo: 40-50 cm"
                iconType="ruler"
                status="optimal"
              />
              <VariableCard
                title="Flujo de Agua"
                value="2.5 L/min"
                optimal="Óptimo: 2-3 L/min"
                iconType="water"
                status="optimal"
              />
        </div>
      </section>

      {/* Tarjetas de acción */}
      <section aria-labelledby="actions-heading">
        <h2 id="actions-heading" className="text-xl font-bold text-green-800 mb-4 font-inter">
          Acciones Rápidas
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <ActionCard
                iconType="sensor"
                title="Calibrar Sensores"
                description="Verificar y ajustar la precisión de los sensores"
                buttonText="Calibrar"
                buttonColor="green"
              />
              <ActionCard
                iconType="trending"
                title="Ver Tendencias"
                description="Analizar el comportamiento de las variables"
                buttonText="Ver Gráficos"
                buttonColor="green"
              />
              <ActionCard
                iconType="brain"
                title="Configurar IA"
                description="Ajustar parámetros del sistema inteligente"
                buttonText="Configurar"
                buttonColor="green"
              />
              <ActionCard
                iconType="plus"
                title="Agregar Variable"
                description="Incluir nueva variable al monitoreo"
                buttonText="Agregar"
                buttonColor="green"
              />
        </div>
      </section>
    </PageLayout>
  );
}