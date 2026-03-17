'use client';

import React, { useState } from 'react';
import Button from '@/components/ui/Button';
import { showSuccess, showError, showConfirm, showInput } from '@/lib/swal';
import type { FuzzySystem, FuzzySystemExport } from '@hydroespinaca/shared';

interface SystemActionButtonsProps {
  system: FuzzySystem;
  onActivate: (id: string) => Promise<void>;
  onClone: (id: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onExport: (id: string) => Promise<FuzzySystemExport>;
  onEdit?: () => void;
  disabled?: boolean;
}

const SystemActionButtons = React.memo(function SystemActionButtons({
  system,
  onActivate,
  onClone,
  onDelete,
  onExport,
  onEdit,
  disabled = false,
}: SystemActionButtonsProps) {
  const [loadingAction, setLoadingAction] = useState<string | null>(null);

  const isActive = system.status === 'ACTIVE';
  const canDelete = system.status === 'DRAFT' || system.status === 'INACTIVE';

  const handleActivate = async () => {
    if (isActive) return;
    const confirmed = await showConfirm({
      title: '¿Activar esta rutina?',
      html: `Se activará <strong>${system.name}</strong> y se desactivarán las demás rutinas activas.`,
      icon: 'question',
      confirmText: 'Sí, activar',
    });
    if (!confirmed) return;

    setLoadingAction('activate');
    try {
      await onActivate(system.id);
      showSuccess({ title: '¡Activada!', text: `${system.name} es ahora la rutina activa.` });
    } catch {
      showError({ title: 'Error', text: 'No se pudo activar la rutina.' });
    } finally {
      setLoadingAction(null);
    }
  };

  const handleClone = async () => {
    const name = await showInput({
      title: 'Duplicar rutina',
      label: 'Nombre para la copia',
      initialValue: `Copia de ${system.name}`,
      confirmText: 'Duplicar',
    });
    if (!name) return;

    setLoadingAction('clone');
    try {
      await onClone(system.id);
      showSuccess({ title: '¡Duplicada!', text: `Se creó "${name}" como copia.` });
    } catch {
      showError({ title: 'Error', text: 'No se pudo duplicar la rutina.' });
    } finally {
      setLoadingAction(null);
    }
  };

  const handleExport = async () => {
    setLoadingAction('export');
    try {
      const data = await onExport(system.id);
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${system.name.replace(/\s+/g, '_').toLowerCase()}_export.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      showSuccess({ title: '¡Exportado!', text: 'El archivo JSON se descargó correctamente.' });
    } catch {
      showError({ title: 'Error', text: 'No se pudo exportar la rutina.' });
    } finally {
      setLoadingAction(null);
    }
  };

  const handleDelete = async () => {
    if (!canDelete) return;
    const confirmed = await showConfirm({
      title: '¿Eliminar esta rutina?',
      html: `Se eliminará <strong>${system.name}</strong> de forma permanente.`,
      confirmText: 'Sí, eliminar',
      danger: true,
    });
    if (!confirmed) return;

    setLoadingAction('delete');
    try {
      await onDelete(system.id);
      showSuccess({ title: '¡Eliminada!', text: 'La rutina fue eliminada correctamente.' });
    } catch {
      showError({ title: 'Error', text: 'No se pudo eliminar la rutina.' });
    } finally {
      setLoadingAction(null);
    }
  };

  return (
    <div className="flex flex-wrap gap-2">
      {/* Edit */}
      {onEdit && (
        <Button
          variant="outline"
          size="sm"
          onClick={onEdit}
          disabled={disabled || loadingAction !== null}
        >
          ✏️ Editar
        </Button>
      )}

      {/* Activate */}
      {!isActive && (
        <Button
          variant="primary"
          size="sm"
          onClick={handleActivate}
          isLoading={loadingAction === 'activate'}
          disabled={disabled || loadingAction !== null}
        >
          ⚡ Activar
        </Button>
      )}

      {/* Clone */}
      <Button
        variant="outline"
        size="sm"
        onClick={handleClone}
        isLoading={loadingAction === 'clone'}
        disabled={disabled || loadingAction !== null}
      >
        📋 Duplicar
      </Button>

      {/* Export */}
      <Button
        variant="ghost"
        size="sm"
        onClick={handleExport}
        isLoading={loadingAction === 'export'}
        disabled={disabled || loadingAction !== null}
      >
        📤 Exportar
      </Button>

      {/* Delete */}
      {canDelete && (
        <Button
          variant="ghost"
          size="sm"
          onClick={handleDelete}
          isLoading={loadingAction === 'delete'}
          disabled={disabled || loadingAction !== null}
          className="text-red-600 hover:bg-red-50"
        >
          🗑️ Eliminar
        </Button>
      )}
    </div>
  );
});

export default SystemActionButtons;
