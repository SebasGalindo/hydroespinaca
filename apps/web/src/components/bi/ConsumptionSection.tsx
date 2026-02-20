'use client';

import React, { useState, useCallback, useEffect } from 'react';
import { useBiStore, formatCurrency, formatDate, CONSUMPTION_TYPE_LABELS } from '@hydroespinaca/shared';
import type { ConsumptionType, ManualConsumptionEntry } from '@hydroespinaca/shared';
import Table from '@/components/ui/Table';
import Button from '@/components/ui/Button';
import Badge from '@/components/ui/Badge';
import EmptyState from '@/components/ui/EmptyState';
import DateRangeFilter from '@/components/ui/DateRangeFilter';
import ConsumptionForm from './ConsumptionForm';
import ConsumptionSummary from './ConsumptionSummary';
import { PlusIcon, TrashIcon, DatabaseIcon } from '@/components/ui/icons/Icons';
import Swal from 'sweetalert2';

const typeVariantMap: Record<ConsumptionType, 'warning' | 'info' | 'success'> = {
  1: 'warning',
  2: 'info',
  3: 'success',
};

const ConsumptionSection: React.FC = () => {
  const {
    consumptionEntries,
    consumptionSummary,
    consumptionLoading,
    consumptionError,
    fetchConsumptionEntries,
    createConsumptionEntry,
    deleteConsumptionEntry,
    fetchConsumptionSummary,
  } = useBiStore();

  const [showForm, setShowForm] = useState(false);
  const [from, setFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0] ?? '';
  });
  const [to, setTo] = useState(() => new Date().toISOString().split('T')[0] ?? '');
  const [typeFilter, setTypeFilter] = useState('');
  const [loaded, setLoaded] = useState(false);

  const loadData = useCallback(async () => {
    const fromISO = `${from}T00:00:00Z`;
    const toISO = `${to}T23:59:59Z`;
    await Promise.all([
      fetchConsumptionEntries(fromISO, toISO, typeFilter || undefined),
      fetchConsumptionSummary(fromISO, toISO),
    ]);
    setLoaded(true);
  }, [from, to, typeFilter, fetchConsumptionEntries, fetchConsumptionSummary]);

  // Auto-load the last 30 days on mount
  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCreate = async (request: Parameters<typeof createConsumptionEntry>[0]) => {
    try {
      await createConsumptionEntry(request);
      Swal.fire({
        icon: 'success',
        title: 'Consumo registrado',
        text: 'El registro de consumo ha sido creado exitosamente.',
        timer: 2000,
        showConfirmButton: false,
      });
      if (loaded) loadData();
    } catch {
      Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo registrar el consumo.' });
    }
  };

  const handleDelete = async (id: string) => {
    const result = await Swal.fire({
      title: '¿Eliminar registro?',
      text: 'Esta acción no se puede deshacer.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
    });

    if (result.isConfirmed) {
      try {
        await deleteConsumptionEntry(id);
        Swal.fire({
          icon: 'success',
          title: 'Eliminado',
          text: 'El registro ha sido eliminado.',
          timer: 1500,
          showConfirmButton: false,
        });
        if (loaded) loadData();
      } catch {
        Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudo eliminar el registro.' });
      }
    }
  };

  const columns = [
    {
      key: 'dateFrom',
      label: 'Fecha',
      render: (value: unknown, row: unknown) => {
        const entry = row as ManualConsumptionEntry;
        const from = formatDate(String(value));
        if (entry.dateFrom === entry.dateTo) return from;
        return `${from} — ${formatDate(entry.dateTo)}`;
      },
    },
    {
      key: 'type',
      label: 'Tipo',
      render: (value: unknown) => (
        <Badge variant={typeVariantMap[Number(value) as ConsumptionType] || 'default'} size="sm">
          {CONSUMPTION_TYPE_LABELS[Number(value) as ConsumptionType] || 'Desconocido'}
        </Badge>
      ),
    },
    {
      key: 'amount',
      label: 'Cantidad',
      render: (value: unknown, row: unknown) => {
        const entry = row as ManualConsumptionEntry;
        const units: Record<number, string> = { 1: 'kWh', 2: 'L', 3: 'L' };
        return `${Number(value).toFixed(2)} ${units[entry.type] || ''}`;
      },
    },
    {
      key: 'costAmount',
      label: 'Costo',
      render: (value: unknown, row: unknown) =>
        formatCurrency(Number(value), (row as ManualConsumptionEntry).currencySnapshot),
    },
    {
      key: 'note',
      label: 'Nota',
      render: (value: unknown) => (value ? String(value) : '—'),
    },
    {
      key: 'id',
      label: 'Acciones',
      render: (_value: unknown, row: unknown) => (
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleDelete((row as ManualConsumptionEntry).id);
          }}
          className="p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
          title="Eliminar"
        >
          <TrashIcon size={16} />
        </button>
      ),
    },
  ];

  if (consumptionError) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700 text-sm font-inter">
        {consumptionError}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Filters */}
      <DateRangeFilter
        from={from}
        to={to}
        onFromChange={setFrom}
        onToChange={setTo}
        onApply={loadData}
        isLoading={consumptionLoading}
      >
        {/* Type filter */}
        <div className="flex items-center gap-2 mt-2">
          <span className="text-xs text-gray-500 font-inter">Tipo:</span>
          {[
            { value: '', label: 'Todos' },
            { value: '1', label: '⚡ Electricidad' },
            { value: '2', label: '💧 Agua' },
            { value: '3', label: '🌱 Nutrientes' },
          ].map((opt) => (
            <button
              key={opt.value}
              onClick={() => setTypeFilter(opt.value)}
              className={`px-3 py-1 text-xs font-medium rounded-full transition-colors font-inter ${
                typeFilter === opt.value
                  ? 'bg-green-600 text-white'
                  : 'bg-gray-100 text-gray-600 hover:bg-green-100 hover:text-green-700'
              }`}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </DateRangeFilter>

      {/* Summary cards */}
      {consumptionSummary && loaded && (
        <ConsumptionSummary summary={consumptionSummary} />
      )}

      {/* Actions bar */}
      <div className="flex justify-between items-center">
        <h3 className="text-sm font-semibold text-gray-700 font-inter">
          Registros de Consumo {loaded && `(${consumptionEntries.length})`}
        </h3>
        <Button variant="primary" size="sm" onClick={() => setShowForm(true)}>
          <span className="flex items-center gap-1">
            <PlusIcon size={16} />
            Nuevo Consumo
          </span>
        </Button>
      </div>

      {/* Table */}
      {!loaded ? (
        <EmptyState
          icon={<DatabaseIcon size={48} />}
          title="Selecciona un rango de fechas"
          description="Usa los filtros de arriba para cargar los registros de consumo."
        />
      ) : consumptionEntries.length === 0 ? (
        <EmptyState
          icon={<DatabaseIcon size={48} />}
          title="Sin registros de consumo"
          description="No hay registros de consumo en el período seleccionado."
          action={{ label: 'Registrar Consumo', onClick: () => setShowForm(true) }}
        />
      ) : (
        <Table
          columns={columns}
          data={consumptionEntries}
          emptyMessage="No hay registros"
        />
      )}

      {/* Create form modal */}
      <ConsumptionForm
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        onSubmit={handleCreate}
        isLoading={consumptionLoading}
      />
    </div>
  );
};

export default ConsumptionSection;
