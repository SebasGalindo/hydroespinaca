'use client';

import React, { useEffect, useState, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import PageLayout from '@/components/layout/PageLayout';
import PageHeader from '@/components/ui/PageHeader';
import BaseCard from '@/components/ui/BaseCard';
import Button from '@/components/ui/Button';
import Input from '@/components/ui/Input';
import Badge from '@/components/ui/Badge';
import FuzzySystemCard from '@/components/ui/FuzzySystemCard';
import { SystemStatusBadge } from '@/components/fuzzy';
import Swal from 'sweetalert2';
import {
  useFuzzyStore,
  type FuzzySystem,
  type FuzzySystemStatus,
  type FuzzySystemExport,
  FUZZY_STATUS_LABELS,
} from '@hydroespinaca/shared';

type StatusFilter = FuzzySystemStatus | 'ALL';

const STATUS_FILTERS: { value: StatusFilter; label: string }[] = [
  { value: 'ALL', label: 'Todos' },
  { value: 'ACTIVE', label: 'Activos' },
  { value: 'DRAFT', label: 'Borradores' },
  { value: 'INACTIVE', label: 'Inactivos' },
  { value: 'TESTING', label: 'Pruebas' },
];

const RoutineListPage: React.FC = () => {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const {
    systems,
    systemsLoading,
    systemsError,
    operationLoading,
    fetchSystems,
    activateSystem,
    cloneSystem,
    deleteSystem,
    exportSystem,
    importSystem,
    clearErrors,
  } = useFuzzyStore();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('ALL');

  useEffect(() => {
    fetchSystems();
    return () => clearErrors();
  }, []);

  // ─── Filters ───────────────────────────────────────────────

  const filtered = systems.filter((s) => {
    if (statusFilter !== 'ALL' && s.status !== statusFilter) return false;
    if (search && !s.name.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  // ─── Card actions ──────────────────────────────────────────

  const handleNavigate = (id: string) => router.push(`/rutinas/${id}`);

  const handleActivate = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const result = await Swal.fire({
      title: '¿Activar esta rutina?',
      html: `<strong>${sys.name}</strong> será la rutina activa. Las demás se desactivarán.`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      confirmButtonText: 'Sí, activar',
      cancelButtonText: 'Cancelar',
    });
    if (!result.isConfirmed) return;
    try {
      await activateSystem(id);
      Swal.fire({ title: '¡Activada!', icon: 'success', timer: 1500, showConfirmButton: false });
    } catch {
      Swal.fire('Error', 'No se pudo activar.', 'error');
    }
  }, [systems, activateSystem]);

  const handleClone = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const { value: name } = await Swal.fire({
      title: 'Duplicar rutina',
      input: 'text',
      inputLabel: 'Nombre para la copia',
      inputValue: `Copia de ${sys.name}`,
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      confirmButtonText: 'Duplicar',
      cancelButtonText: 'Cancelar',
      inputValidator: (v) => (!v ? 'Nombre requerido' : null),
    });
    if (!name) return;
    try {
      await cloneSystem(id, { name });
      Swal.fire({ title: '¡Duplicada!', icon: 'success', timer: 1500, showConfirmButton: false });
    } catch {
      Swal.fire('Error', 'No se pudo duplicar.', 'error');
    }
  }, [systems, cloneSystem]);

  const handleDelete = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const result = await Swal.fire({
      title: '¿Eliminar esta rutina?',
      html: `<strong>${sys.name}</strong> se eliminará permanentemente.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#dc2626',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
    });
    if (!result.isConfirmed) return;
    try {
      await deleteSystem(id);
      Swal.fire({ title: '¡Eliminada!', icon: 'success', timer: 1500, showConfirmButton: false });
    } catch {
      Swal.fire('Error', 'No se pudo eliminar.', 'error');
    }
  }, [systems, deleteSystem]);

  const handleExport = useCallback(async (id: string): Promise<FuzzySystemExport> => {
    const data = await exportSystem(id);
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    const sys = systems.find((s) => s.id === id);
    a.href = url;
    a.download = `${(sys?.name ?? 'rutina').replace(/\s+/g, '_').toLowerCase()}_export.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    return data;
  }, [systems, exportSystem]);

  // ─── Import from JSON file ─────────────────────────────────

  const handleImportClick = () => fileInputRef.current?.click();

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const data = JSON.parse(text) as FuzzySystemExport;
      if (!data.version || !data.system) {
        Swal.fire('Formato inválido', 'El archivo no parece ser una exportación válida.', 'error');
        return;
      }
      await importSystem(data);
      Swal.fire({ title: '¡Importada!', text: 'La rutina fue importada correctamente.', icon: 'success', timer: 2000, showConfirmButton: false });
    } catch {
      Swal.fire('Error', 'No se pudo importar el archivo.', 'error');
    } finally {
      // reset the file input
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  // ─── Count badges ──────────────────────────────────────────

  const countByStatus = (status: FuzzySystemStatus) => systems.filter((s) => s.status === status).length;

  // ─── Render ────────────────────────────────────────────────

  return (
    <PageLayout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <PageHeader
          title="Rutinas Fuzzy"
          subtitle="Gestiona, compara y experimenta con diferentes configuraciones de control difuso"
          alignment="left"
        >
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" size="sm" onClick={handleImportClick}>
              📥 Importar JSON
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              accept=".json"
              className="hidden"
              onChange={handleFileSelected}
            />
          </div>
        </PageHeader>

        {/* Status summary */}
        <div className="flex flex-wrap gap-3 mb-6">
          {(['ACTIVE', 'DRAFT', 'INACTIVE', 'TESTING'] as FuzzySystemStatus[]).map((st) => {
            const count = countByStatus(st);
            return (
              <button
                key={st}
                onClick={() => setStatusFilter(statusFilter === st ? 'ALL' : st)}
                className={`transition-all ${statusFilter === st ? 'ring-2 ring-green-400 rounded-full' : ''}`}
              >
                <SystemStatusBadge status={st} size="md" />
                <span className="ml-1 text-xs text-gray-500 font-inter">{count}</span>
              </button>
            );
          })}
        </div>

        {/* Search + filter row */}
        <div className="flex flex-col sm:flex-row gap-3 mb-6">
          <div className="flex-1">
            <Input
              placeholder="Buscar por nombre..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              icon={<span className="text-sm">🔍</span>}
            />
          </div>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
            className="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          >
            {STATUS_FILTERS.map((f) => (
              <option key={f.value} value={f.value}>{f.label}</option>
            ))}
          </select>
        </div>

        {/* Error */}
        {systemsError && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-700">{systemsError}</p>
            <Button variant="ghost" size="sm" onClick={fetchSystems} className="mt-2">
              Reintentar
            </Button>
          </div>
        )}

        {/* Loading */}
        {systemsLoading && (
          <div className="flex items-center justify-center py-16">
            <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-green-600" />
          </div>
        )}

        {/* Grid */}
        {!systemsLoading && filtered.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {filtered.map((system) => (
              <FuzzySystemCard
                key={system.id}
                system={system}
                onClick={() => handleNavigate(system.id)}
                onActivate={handleActivate}
                onClone={handleClone}
                onDelete={handleDelete}
                onExport={handleExport}
                actionsDisabled={operationLoading}
              />
            ))}
          </div>
        )}

        {/* Empty */}
        {!systemsLoading && filtered.length === 0 && !systemsError && (
          <div className="text-center py-16">
            <div className="w-20 h-20 mx-auto bg-gray-100 rounded-full flex items-center justify-center mb-4">
              <span className="text-3xl">🧠</span>
            </div>
            <h3 className="text-lg font-semibold text-gray-800 font-inter mb-2">
              {systems.length === 0 ? 'No hay rutinas creadas' : 'Sin resultados'}
            </h3>
            <p className="text-sm text-gray-500 font-inter mb-4">
              {systems.length === 0
                ? 'Importa una rutina desde un archivo JSON para comenzar.'
                : 'Ajusta los filtros para ver más resultados.'}
            </p>
            {systems.length === 0 && (
              <Button variant="outline" onClick={handleImportClick}>
                📥 Importar rutina
              </Button>
            )}
          </div>
        )}
      </div>
    </PageLayout>
  );
};

export default RoutineListPage;
