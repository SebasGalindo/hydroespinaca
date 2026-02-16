'use client';

import React, { useState } from 'react';
import Button from '@/components/ui/Button';
import Swal from 'sweetalert2';
import type { FuzzySystem, FuzzySystemExport } from '@hydroespinaca/shared';

interface SystemActionButtonsProps {
  system: FuzzySystem;
  onActivate: (id: string) => Promise<void>;
  onClone: (id: string) => Promise<void>;
  onDelete: (id: string) => Promise<void>;
  onExport: (id: string) => Promise<FuzzySystemExport>;
  disabled?: boolean;
}

const SystemActionButtons: React.FC<SystemActionButtonsProps> = ({
  system,
  onActivate,
  onClone,
  onDelete,
  onExport,
  disabled = false,
}) => {
  const [loadingAction, setLoadingAction] = useState<string | null>(null);

  const isActive = system.status === 'ACTIVE';
  const canDelete = system.status === 'DRAFT' || system.status === 'INACTIVE';

  const handleActivate = async () => {
    if (isActive) return;
    const result = await Swal.fire({
      title: '¿Activar esta rutina?',
      html: `Se activará <strong>${system.name}</strong> y se desactivarán las demás rutinas activas.`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Sí, activar',
      cancelButtonText: 'Cancelar',
    });
    if (!result.isConfirmed) return;

    setLoadingAction('activate');
    try {
      await onActivate(system.id);
      Swal.fire({
        title: '¡Activada!',
        text: `${system.name} es ahora la rutina activa.`,
        icon: 'success',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire('Error', 'No se pudo activar la rutina.', 'error');
    } finally {
      setLoadingAction(null);
    }
  };

  const handleClone = async () => {
    const { value: name } = await Swal.fire({
      title: 'Duplicar rutina',
      input: 'text',
      inputLabel: 'Nombre para la copia',
      inputValue: `Copia de ${system.name}`,
      showCancelButton: true,
      confirmButtonColor: '#16a34a',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Duplicar',
      cancelButtonText: 'Cancelar',
      inputValidator: (value) => (!value ? 'El nombre no puede estar vacío' : null),
    });
    if (!name) return;

    setLoadingAction('clone');
    try {
      await onClone(system.id);
      Swal.fire({
        title: '¡Duplicada!',
        text: `Se creó "${name}" como copia.`,
        icon: 'success',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire('Error', 'No se pudo duplicar la rutina.', 'error');
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

      Swal.fire({
        title: '¡Exportado!',
        text: 'El archivo JSON se descargó correctamente.',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire('Error', 'No se pudo exportar la rutina.', 'error');
    } finally {
      setLoadingAction(null);
    }
  };

  const handleDelete = async () => {
    if (!canDelete) return;
    const result = await Swal.fire({
      title: '¿Eliminar esta rutina?',
      html: `Se eliminará <strong>${system.name}</strong> de forma permanente.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#dc2626',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
    });
    if (!result.isConfirmed) return;

    setLoadingAction('delete');
    try {
      await onDelete(system.id);
      Swal.fire({
        title: '¡Eliminada!',
        text: 'La rutina fue eliminada correctamente.',
        icon: 'success',
        timer: 2000,
        showConfirmButton: false,
      });
    } catch {
      Swal.fire('Error', 'No se pudo eliminar la rutina.', 'error');
    } finally {
      setLoadingAction(null);
    }
  };

  return (
    <div className="flex flex-wrap gap-2">
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
};

export default SystemActionButtons;
