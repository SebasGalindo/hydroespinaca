'use client';

import React, { useEffect, useState } from 'react';
import { useBiStore } from '@hydroespinaca/shared';
import { formatCurrency, formatDate } from '@hydroespinaca/shared';
import Table from '@/components/ui/Table';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';
import EmptyState from '@/components/ui/EmptyState';
import StatCard from '@/components/ui/StatCard';
import CostConfigForm from './CostConfigForm';
import { SettingsIcon, BoltIcon, DropletIcon, PlantIcon, PlusIcon } from '@/components/ui/icons/Icons';
import Swal from 'sweetalert2';

import type { CostConfigVersion } from '@hydroespinaca/shared';

const CostConfigSection: React.FC = () => {
  const {
    currentCostConfig,
    costConfigVersions,
    costConfigLoading,
    costConfigError,
    fetchCurrentCostConfig,
    fetchCostConfigVersions,
    createCostConfigVersion,
  } = useBiStore();

  const [showForm, setShowForm] = useState(false);

  useEffect(() => {
    fetchCurrentCostConfig();
    fetchCostConfigVersions();
  }, [fetchCurrentCostConfig, fetchCostConfigVersions]);

  const handleCreate = async (request: Parameters<typeof createCostConfigVersion>[0]) => {
    try {
      await createCostConfigVersion(request);
      await fetchCurrentCostConfig();
      await fetchCostConfigVersions();
      Swal.fire({
        icon: 'success',
        title: 'Configuración creada',
        text: 'La nueva configuración de costos ha sido creada exitosamente.',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se pudo crear la configuración de costos.',
      });
    }
  };

  const columns = [
    {
      key: 'isActive',
      label: 'Estado',
      render: (value: unknown) => (
        <Badge variant={value ? 'success' : 'default'} size="sm">
          {value ? 'Activa' : 'Inactiva'}
        </Badge>
      ),
    },
    {
      key: 'electricityCostPerKwh',
      label: 'Electricidad/kWh',
      render: (value: unknown, row: unknown) =>
        formatCurrency(Number(value), (row as CostConfigVersion).currency),
    },
    {
      key: 'waterCostPerLiter',
      label: 'Agua/L',
      render: (value: unknown, row: unknown) =>
        formatCurrency(Number(value), (row as CostConfigVersion).currency),
    },
    {
      key: 'nutrientCostPerLiter',
      label: 'Nutrientes/L',
      render: (value: unknown, row: unknown) =>
        formatCurrency(Number(value), (row as CostConfigVersion).currency),
    },
    {
      key: 'effectiveFrom',
      label: 'Desde',
      render: (value: unknown) => formatDate(String(value)),
    },
    {
      key: 'effectiveTo',
      label: 'Hasta',
      render: (value: unknown) => value ? formatDate(String(value)) : '—',
    },
  ];

  if (costConfigError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700 text-sm font-inter">
        {costConfigError}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Current config summary */}
      {currentCostConfig && (
        <div>
          <h3 className="text-sm font-semibold text-gray-700 mb-3 font-inter">
            Configuración Vigente
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <StatCard
              label="Electricidad"
              value={formatCurrency(currentCostConfig.electricityCostPerKwh, currentCostConfig.currency)}
              unit="/kWh"
              icon={<BoltIcon size={20} />}
              variant="warning"
            />
            <StatCard
              label="Agua"
              value={formatCurrency(currentCostConfig.waterCostPerLiter, currentCostConfig.currency)}
              unit="/L"
              icon={<DropletIcon size={20} />}
              variant="info"
            />
            <StatCard
              label="Nutrientes"
              value={formatCurrency(currentCostConfig.nutrientCostPerLiter, currentCostConfig.currency)}
              unit="/L"
              icon={<PlantIcon size={20} />}
              variant="success"
            />
          </div>
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-between items-center">
        <h3 className="text-sm font-semibold text-gray-700 font-inter">
          Historial de Versiones
        </h3>
        <Button variant="primary" size="sm" onClick={() => setShowForm(true)}>
          <span className="flex items-center gap-1">
            <PlusIcon size={16} />
            Nueva Versión
          </span>
        </Button>
      </div>

      {/* Versions table */}
      {costConfigVersions.length === 0 && !costConfigLoading ? (
        <EmptyState
          icon={<SettingsIcon size={48} />}
          title="Sin configuración de costos"
          description="Crea la primera configuración de costos para poder registrar consumos y calcular gastos."
          action={{ label: 'Crear Configuración', onClick: () => setShowForm(true) }}
        />
      ) : (
        <Table
          columns={columns}
          data={costConfigVersions}
          emptyMessage="No hay versiones de configuración"
        />
      )}

      {/* Create form modal */}
      <CostConfigForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSubmit={handleCreate}
        isLoading={costConfigLoading}
      />
    </div>
  );
};

export default CostConfigSection;
