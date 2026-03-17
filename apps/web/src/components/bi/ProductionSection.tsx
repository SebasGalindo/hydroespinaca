'use client';

import React, { useEffect, useState } from 'react';
import { useBiStore, formatCurrency, formatDate } from '@hydroespinaca/shared';
import type { ProductionRecord } from '@hydroespinaca/shared';
import Table from '@/components/ui/Table';
import Button from '@/components/ui/Button';
import EmptyState from '@/components/ui/EmptyState';
import ProductionForm from './ProductionForm';
import { PlusIcon, TrashIcon, PlantIcon } from '@/components/ui/icons/Icons';
import { showConfirm, showError, showSuccess } from '@/lib/swal';

const ProductionSection = React.memo(function ProductionSection() {
  const {
    productionRecords,
    productionLoading,
    productionError,
    fetchProductionRecords,
    createProductionRecord,
    deleteProductionRecord,
  } = useBiStore();

  const [showForm, setShowForm] = useState(false);

  useEffect(() => {
    fetchProductionRecords();
  }, [fetchProductionRecords]);

  const handleCreate = async (request: Parameters<typeof createProductionRecord>[0]) => {
    try {
      await createProductionRecord(request);
      showSuccess({ title: 'Producción registrada', text: 'El registro de producción ha sido creado exitosamente.' });
    } catch {
      showError({ text: 'No se pudo registrar la producción.' });
    }
  };

  const handleDelete = async (id: string) => {
    const confirmed = await showConfirm({ title: '¿Eliminar producción?', text: 'Esta acción no se puede deshacer. Los cálculos de rentabilidad asociados se perderán.', confirmText: 'Sí, eliminar', danger: true });

    if (confirmed) {
      try {
        await deleteProductionRecord(id);
        showSuccess({ title: 'Eliminado', text: 'El registro de producción ha sido eliminado.' });
      } catch {
        showError({ text: 'No se pudo eliminar la producción.' });
      }
    }
  };

  const getDurationDays = (start: string, end: string): number => {
    const ms = new Date(end).getTime() - new Date(start).getTime();
    return Math.ceil(ms / (1000 * 60 * 60 * 24));
  };

  const columns = [
    {
      key: 'cropName',
      label: 'Cultivo',
      render: (value: unknown) => (
        <span className="font-semibold text-gray-900">{String(value)}</span>
      ),
    },
    {
      key: 'startDate',
      label: 'Inicio',
      render: (value: unknown) => formatDate(String(value)),
    },
    {
      key: 'harvestDate',
      label: 'Cosecha',
      render: (value: unknown) => formatDate(String(value)),
    },
    {
      key: 'kilosProduced',
      label: 'Kilos',
      render: (value: unknown, row: unknown) => {
        const r = row as ProductionRecord;
        return `${Number(value).toFixed(1)} kg (${getDurationDays(r.startDate, r.harvestDate)} días)`;
      },
    },
    {
      key: 'pricePerKilo',
      label: 'Precio/kg',
      render: (value: unknown, row: unknown) =>
        formatCurrency(Number(value), (row as ProductionRecord).currency),
    },
    {
      key: 'id',
      label: 'Ingreso Est.',
      render: (_value: unknown, row: unknown) => {
        const r = row as ProductionRecord;
        return (
          <span className="font-semibold text-green-700">
            {formatCurrency(r.kilosProduced * r.pricePerKilo, r.currency)}
          </span>
        );
      },
    },
    {
      key: '_actions',
      label: '',
      render: (_value: unknown, row: unknown) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleDelete((row as ProductionRecord).id);
          }}
          className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
          title="Eliminar"
        >
          <TrashIcon size={16} />
        </button>
      ),
    },
  ];

  if (productionError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700 text-sm font-inter">
        {productionError}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Actions bar */}
      <div className="flex justify-between items-center">
        <h3 className="text-sm font-semibold text-gray-700 font-inter">
          Registros de Producción ({productionRecords.length})
        </h3>
        <Button variant="primary" size="sm" onClick={() => setShowForm(true)}>
          <span className="flex items-center gap-1">
            <PlusIcon size={16} />
            Nueva Producción
          </span>
        </Button>
      </div>

      {/* Table */}
      {productionRecords.length === 0 && !productionLoading ? (
        <EmptyState
          icon={<PlantIcon size={48} />}
          title="Sin registros de producción"
          description="Registra tu primera cosecha para poder calcular la rentabilidad del sistema."
          action={{ label: 'Registrar Producción', onClick: () => setShowForm(true) }}
        />
      ) : (
        <Table
          columns={columns}
          data={productionRecords}
          emptyMessage="No hay registros de producción"
        />
      )}

      {/* Create form modal */}
      <ProductionForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSubmit={handleCreate}
        isLoading={productionLoading}
      />
    </div>
  );
});

export default ProductionSection;
