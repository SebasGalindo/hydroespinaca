'use client';

import React, { useState } from 'react';
import { 
   PlusIcon 
 } from '@/components/ui/icons/Icons';
import { useRouter } from 'next/navigation';
import ActuadorCard from './ActuadorCard';
import ActuadoresTable from './ActuadoresTable';
import ActuadorForm from './ActuadorForm';
import PageHeader from '@/components/ui/PageHeader';
import Section from '@/components/ui/Section';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';
import { useActuatorStore, ActuadorData } from '@hydroespinaca/shared';

// Data is now managed by the store

export default function ActuadoresConfigForm() {
  const router = useRouter();
  const { actuadores, addActuador, updateActuador, removeActuador } = useActuatorStore();
  const [activeTab, setActiveTab] = useState('Actuadores');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingActuador, setEditingActuador] = useState<ActuadorData | undefined>(undefined);



  const handleActuadorEdit = (actuadorId: string) => {
    const actuador = actuadores.find(a => a.id === actuadorId);
    if (actuador) {
      setEditingActuador(actuador);
      setIsModalOpen(true);
    }
  };

  const handleActuadorDelete = (actuadorId: string) => {
    removeActuador(actuadorId);
  };

  const handleAddActuador = () => {
    setEditingActuador(undefined);
    setIsModalOpen(true);
  };

  const getCurrentDate = (): string => {
    return new Date().toISOString().split('T')[0] || new Date().toLocaleDateString('en-CA');
  };

  const handleFormSubmit = (actuadorData: Omit<ActuadorData, 'id' | 'createdAt' | 'lastModified'>) => {
    if (editingActuador) {
      // Editar actuador existente
      updateActuador(editingActuador.id, {
        ...actuadorData,
        lastModified: getCurrentDate()
      });
    } else {
      // Crear nuevo actuador
      const newActuador: ActuadorData = {
        id: `ACT-${String(actuadores.length + 1).padStart(3, '0')}`,
        ...actuadorData,
        createdAt: getCurrentDate(),
        lastModified: getCurrentDate()
      };
      addActuador(newActuador);
    }
    setIsModalOpen(false);
    setEditingActuador(undefined);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingActuador(undefined);
  };

  // Eliminada la navegación por pestañas - ahora se maneja en el SideNavigation

  return (
    <div className="bg-white min-h-screen">
      {/* Header con título y botón agregar */}
      <div className="bg-green-50 px-6 py-6">
        <div className="flex flex-col lg:flex-row lg:justify-between lg:items-center gap-4 mb-6">
          <PageHeader 
            title="Configuración de Actuadores"
            subtitle="Administra y controla los actuadores del invernadero"
            alignment="left"
            className="text-center lg:text-left"
          />
          <button 
            onClick={handleAddActuador}
            className="
    flex items-center justify-center gap-2
    px-6 py-3 rounded-lg w-fit font-semibold
    bg-green-200 text-green-800
    hover:bg-green-300 transition-colors
    mx-auto lg:mx-0
  "
            onMouseEnter={(e) => e .currentTarget.style.backgroundColor = '#A8E6A8'}
            onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#BEEEBE'}
          >
            <PlusIcon className="w-5 h-5 icon-hidro-green" />
            AGREGAR ACTUADOR
          </button>
        </div>

        {/* Navegación por pestañas eliminada - ahora se maneja en el SideNavigation */}
      </div>

      {/* Contenido principal */}
      <div className="p-4 sm:p-8">
        {/* Vista de tarjetas para móvil */}
        <div className="grid grid-cols-1 lg:hidden gap-6">
          {actuadores.map((actuador) => (
            <ActuadorCard
              key={actuador.id}
              actuador={actuador}
              onEdit={handleActuadorEdit}
              onDelete={handleActuadorDelete}
            />
          ))}
        </div>

        {/* Vista de tabla para escritorio */}
        <div className="hidden lg:block">
          <ActuadoresTable 
            actuadores={actuadores} 
            onEdit={handleActuadorEdit} 
            onDelete={handleActuadorDelete} 
          />
        </div>
      </div>

      {/* Modal para crear/editar actuador */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        title={editingActuador ? 'Editar Actuador' : 'Crear Nuevo Actuador'}
        maxWidth="xl"
      >
        <ActuadorForm
          {...(editingActuador && { actuador: editingActuador })}
          onSubmit={handleFormSubmit}
          onCancel={handleModalClose}
          isEditing={!!editingActuador}
        />
      </Modal>
    </div>
  );
}