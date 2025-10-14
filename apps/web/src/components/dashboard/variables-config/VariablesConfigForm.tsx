'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useVariableStore } from '@hidroespinaca/shared';
import VariableCard from './VariableCard';
import VariablesTable from './VariablesTable';
import VariableForm from './VariableForm';
import PageHeader from '@/components/ui/PageHeader';
import Section from '@/components/ui/Section';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';

import type { VariableData } from '@hidroespinaca/shared';

// Data is now managed by the store

export default function VariablesConfigForm() {
  const router = useRouter();
  const { variables, addVariable, updateVariable, removeVariable } = useVariableStore();
  const [activeTab, setActiveTab] = useState('Variables');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingVariable, setEditingVariable] = useState<VariableData | undefined>(undefined);



  const handleVariableEdit = (variableId: string) => {
    const variable = variables.find(v => v.id === variableId);
    if (variable) {
      setEditingVariable(variable);
      setIsModalOpen(true);
    }
  };

  const handleVariableDelete = (variableId: string) => {
    removeVariable(variableId);
  };

  const handleAddVariable = () => {
    setEditingVariable(undefined);
    setIsModalOpen(true);
  };

  const getCurrentDate = (): string => {
    return new Date().toISOString().split('T')[0] || new Date().toLocaleDateString('en-CA');
  };

  const handleFormSubmit = (variableData: Omit<VariableData, 'id' | 'createdAt' | 'lastModified'>) => {
    if (editingVariable) {
      // Editar variable existente
      updateVariable(editingVariable.id, {
        ...variableData,
        lastModified: getCurrentDate()
      });
    } else {
      // Crear nueva variable
      const newVariable: VariableData = {
        id: `VAR-${String(variables.length + 1).padStart(3, '0')}`,
        ...variableData,
        createdAt: getCurrentDate(),
        lastModified: getCurrentDate()
      };
      addVariable(newVariable);
    }
    setIsModalOpen(false);
    setEditingVariable(undefined);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingVariable(undefined);
  };

  // Eliminada la navegación por pestañas - ahora se maneja en el SideNavigation

  return (
    <div className="bg-white min-h-screen">
      {/* Header con título y botón agregar */}
      <div className="bg-green-50 px-6 py-6">
        <div className="flex flex-col lg:flex-row lg:justify-between lg:items-center gap-4 mb-6">
          <PageHeader 
            title="Configuración de Variables"
            subtitle="Administra las variables del sistema de control"
            alignment="left"
            className="text-center lg:text-left"
          />
          <button
              onClick={handleAddVariable}
              className="px-6 py-3 rounded-lg flex items-center justify-center gap-2 transition-colors mx-auto lg:mx-0 w-fit font-semibold btn-hidro-add"
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#A8E6A8'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#BEEEBE'}
            >
            <span className="text-xl font-bold flex items-center justify-center icon-hidro-green">+</span>
            AGREGAR VARIABLE
          </button>
        </div>

        {/* Navegación por pestañas eliminada - ahora se maneja en el SideNavigation */}
      </div>

      {/* Contenido principal */}
      <div className="p-4 sm:p-8">
        {/* Vista de tarjetas para móvil */}
        <div className="grid grid-cols-1 lg:hidden gap-6">
          {variables.map((variable) => (
            <VariableCard
              key={variable.id}
              variable={variable}
              onEdit={handleVariableEdit}
              onDelete={handleVariableDelete}
              isMobile={true}
            />
          ))}
        </div>

        {/* Vista de tabla para escritorio */}
        <div className="hidden lg:block">
          <VariablesTable 
            variables={variables} 
            onEdit={handleVariableEdit} 
            onDelete={handleVariableDelete} 
          />
        </div>
      </div>

      {/* Modal para crear/editar variable */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        title={editingVariable ? 'Editar Variable' : 'Crear Nueva Variable'}
        maxWidth="xl"
      >
        <VariableForm
          {...(editingVariable && { variable: editingVariable })}
          onSubmit={handleFormSubmit}
          onCancel={handleModalClose}
          isEditing={!!editingVariable}
        />
      </Modal>
    </div>
  );
}