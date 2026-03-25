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
import { SystemStatusBadge, SystemForm } from '@/components/fuzzy';
import { showError, showSuccess, showConfirm, showInput } from '@/lib/swal';
import {
  useFuzzyStore,
  type FuzzySystem,
  type FuzzySystemStatus,
  type FuzzySystemExport,
  type CreateFuzzySystemRequest,
  type UpdateFuzzySystemRequest,
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

const RoutineListPage = React.memo(function RoutineListPage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const {
    systems,
    systemsLoading,
    systemsError,
    operationLoading,
    importLoading,
    crudLoading,
    fetchSystems,
    createSystem,
    activateSystem,
    cloneSystem,
    deleteSystem,
    exportSystem,
    importSystem,
    clearErrors,
  } = useFuzzyStore();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('ALL');
  const [showSystemForm, setShowSystemForm] = useState(false);
  const [loadingMessage, setLoadingMessage] = useState<string | null>(null);

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

  const handleCreateSystem = useCallback(async (request: CreateFuzzySystemRequest | UpdateFuzzySystemRequest) => {
    try {
      const created = await createSystem(request as CreateFuzzySystemRequest);
      showSuccess({ title: '¡Sistema creado!' });
      router.push(`/rutinas/${created.id}`);
    } catch {
      showError({ text: 'No se pudo crear el sistema.' });
    }
  }, [createSystem, router]);

  const handleActivate = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const confirmed = await showConfirm({
      title: '¿Activar esta rutina?',
      html: `<strong>${sys.name}</strong> será la rutina activa. Las demás se desactivarán.`,
      icon: 'question',
      confirmText: 'Sí, activar',
    });
    if (!confirmed) return;
    try {
      await activateSystem(id);
      showSuccess({ title: '¡Activada!' });
    } catch {
      showError({ text: 'No se pudo activar.' });
    }
  }, [systems, activateSystem]);

  const handleClone = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const name = await showInput({
      title: 'Duplicar rutina',
      label: 'Nombre para la copia',
      initialValue: `Copia de ${sys.name}`,
      confirmText: 'Duplicar',
    });
    if (!name) return;
    try {
      setLoadingMessage('Duplicando rutina… esto puede tardar unos segundos');
      await cloneSystem(id, { name });
      showSuccess({ title: '¡Duplicada!' });
    } catch {
      showError({ text: 'No se pudo duplicar.' });
    } finally {
      setLoadingMessage(null);
    }
  }, [systems, cloneSystem]);

  const handleDelete = useCallback(async (id: string) => {
    const sys = systems.find((s) => s.id === id);
    if (!sys) return;
    const confirmed = await showConfirm({
      title: '¿Eliminar esta rutina?',
      html: `<strong>${sys.name}</strong> se eliminará permanentemente.`,
      confirmText: 'Sí, eliminar',
      danger: true,
    });
    if (!confirmed) return;
    try {
      await deleteSystem(id);
      showSuccess({ title: '¡Eliminada!' });
    } catch {
      showError({ text: 'No se pudo eliminar.' });
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
        showError({ title: 'Formato inválido', text: 'El archivo no parece ser una exportación válida.' });
        return;
      }
      setLoadingMessage('Importando rutina… esto puede tardar unos segundos');
      await importSystem(data);
      showSuccess({ title: '¡Importada!', text: 'La rutina fue importada correctamente.' });
    } catch {
      showError({ text: 'No se pudo importar el archivo.' });
    } finally {
      setLoadingMessage(null);
      // reset the file input
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  // ─── Count badges ──────────────────────────────────────────

  const countByStatus = (status: FuzzySystemStatus) => systems.filter((s) => s.status === status).length;

  // ─── Render ────────────────────────────────────────────────

  return (
    <PageLayout>
      {/* Loading overlay for clone/import */}
      {(loadingMessage || operationLoading || importLoading) && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl px-8 py-6 flex flex-col items-center gap-4 max-w-sm mx-4">
            <div className="animate-spin rounded-full h-10 w-10 border-4 border-green-200 border-t-green-600" />
            <p className="text-sm font-medium text-gray-700 text-center font-inter">
              {loadingMessage ?? 'Procesando…'}
            </p>
          </div>
        </div>
      )}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <PageHeader
          title="Rutinas Fuzzy"
          subtitle="Gestiona, compara y experimenta con diferentes configuraciones de control difuso"
          alignment="left"
        >
          <div className="flex flex-wrap gap-2">
            <Button variant="primary" size="sm" onClick={() => setShowSystemForm(true)}>
              ➕ Crear Sistema
            </Button>
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
              <div className="flex gap-3 justify-center">
                <Button variant="primary" onClick={() => setShowSystemForm(true)}>
                  ➕ Crear sistema
                </Button>
                <Button variant="outline" onClick={handleImportClick}>
                  📥 Importar rutina
                </Button>
              </div>
            )}
          </div>
        )}

        {/* Create system modal */}
        <SystemForm
          isOpen={showSystemForm}
          onClose={() => setShowSystemForm(false)}
          onSubmit={handleCreateSystem}
          isLoading={crudLoading}
        />
      </div>
    </PageLayout>
  );
});

export default RoutineListPage;
