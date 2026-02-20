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
import { SettingsIcon, BoltIcon, DropletIcon, PlantIcon, PlusIcon, EditIcon, TrashIcon } from '@/components/ui/icons/Icons';
import Swal from 'sweetalert2';

import type { CostConfigVersion, UpdateCostConfigVersionRequest } from '@hydroespinaca/shared';

const CostConfigSection: React.FC = () => {
  const {
    currentCostConfig,
    costConfigVersions,
    costConfigLoading,
    costConfigError,
    fetchCurrentCostConfig,
    fetchCostConfigVersions,
    createCostConfigVersion,
    updateCostConfigVersion,
    deleteCostConfigVersion,
  } = useBiStore();

  const [showForm, setShowForm] = useState(false);
  const [editingVersion, setEditingVersion] = useState<CostConfigVersion | null>(null);

  useEffect(() => {
    fetchCurrentCostConfig();
    fetchCostConfigVersions();
  }, [fetchCurrentCostConfig, fetchCostConfigVersions]);

  const handleCreate = async (request: Parameters<typeof createCostConfigVersion>[0]) => {
    try {
      await createCostConfigVersion(request);
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

  const handleUpdate = async (request: UpdateCostConfigVersionRequest) => {
    if (!editingVersion) return;
    try {
      await updateCostConfigVersion(editingVersion.id, request);
      Swal.fire({
        icon: 'success',
        title: 'Configuración actualizada',
        text: 'La configuración de costos ha sido actualizada exitosamente.',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se pudo actualizar la configuración de costos.',
      });
    }
  };

  const handleDelete = async (id: string) => {
    const result = await Swal.fire({
      title: '¿Eliminar configuración?',
      text: 'Esta acción no se puede deshacer. Las demás versiones serán recalculadas.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonText: 'Cancelar',
      confirmButtonText: 'Sí, eliminar',
    });

    if (result.isConfirmed) {
      try {
        await deleteCostConfigVersion(id);
        Swal.fire({
          icon: 'success',
          title: 'Eliminada',
          text: 'La configuración ha sido eliminada.',
          timer: 2000,
          showConfirmButton: false,
        });
      } catch {
        Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo eliminar la configuración.' });
      }
    }
  };

  const openEditForm = (version: CostConfigVersion) => {
    setEditingVersion(version);
    setShowForm(true);
  };

  const openCreateForm = () => {
    setEditingVersion(null);
    setShowForm(true);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingVersion(null);
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
    {
      key: 'id',
      label: 'Acciones',
      render: (_value: unknown, row: unknown) => {
        const version = row as CostConfigVersion;
        return (
          <div className="flex items-center gap-1">
            <button
              onClick={(e) => { e.stopPropagation(); openEditForm(version); }}
              className="p-1.5 text-blue-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
              title="Editar"
            >
              <EditIcon size={16} />
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); handleDelete(version.id); }}
              className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
              title="Eliminar"
            >
              <TrashIcon size={16} />
            </button>
          </div>
        );
      },
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
        <Button variant="primary" size="sm" onClick={openCreateForm}>
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
          action={{ label: 'Crear Configuración', onClick: openCreateForm }}
        />
      ) : (
        <Table
          columns={columns}
          data={costConfigVersions}
          emptyMessage="No hay versiones de configuración"
        />
      )}

      {/* Create / Edit form modal */}
      <CostConfigForm
        isOpen={showForm}
        onClose={closeForm}
        onSubmit={editingVersion ? handleUpdate : handleCreate}
        isLoading={costConfigLoading}
        editData={editingVersion}
      />
    </div>
  );
};

export default CostConfigSection;
