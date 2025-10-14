'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import SensorCard from './SensorCard';
import SensorsTable from './SensorsTable';
import SensorForm from './SensorForm';
import PageHeader from '@/components/ui/PageHeader';
import Section from '@/components/ui/Section';
import Button from '@/components/ui/Button';
import Modal from '@/components/ui/Modal';

// Tipos de datos para sensores
export interface SensorData {
  id: string;
  physicalId: string;
  location: string;
  esp32Id: string;
  readFrequency: number;
  variables: string[];
  status: 'active' | 'inactive' | 'error' | 'calibrating';
  type: 'temperature' | 'humidity' | 'ph' | 'conductivity' | 'light' | 'water_level';
  currentValue: string;
  unit: string;
  lastCalibration: string;
  calibrationDue: boolean;
  minRange: number;
  maxRange: number;
  optimalMin: number;
  optimalMax: number;
  minValue?: number;
  maxValue?: number;
}

// Datos de ejemplo para sensores
const initialSensors: SensorData[] = [
  {
    id: 'bh1750-A1',
    physicalId: 'BH1750-A1',
    location: 'rack-1',
    esp32Id: '6883fff7b079...',
    readFrequency: 30,
    variables: ['Intensidad Lumínica'],
    status: 'active',
    type: 'light',
    currentValue: '850',
    unit: 'lux',
    lastCalibration: '2024-01-18',
    calibrationDue: false,
    minRange: 0,
    maxRange: 2000,
    optimalMin: 800,
    optimalMax: 1200
  },
  {
    id: 'dht22-B2',
    physicalId: 'DHT22-B2',
    location: 'rack-2',
    esp32Id: '6883fff7b079...',
    readFrequency: 60,
    variables: ['Temperatura', 'Humedad'],
    status: 'active',
    type: 'temperature',
    currentValue: '25.2',
    unit: '°C',
    lastCalibration: '2024-01-15',
    calibrationDue: false,
    minRange: 0,
    maxRange: 50,
    optimalMin: 20,
    optimalMax: 28
  },
  {
    id: 'ds18b20-C3',
    physicalId: 'DS18B20-C3',
    location: 'soil-1',
    esp32Id: '6883fff7b079...',
    readFrequency: 120,
    variables: ['Intensidad Lumínica'],
    status: 'active',
    type: 'light',
    currentValue: '900',
    unit: 'lux',
    lastCalibration: '2024-01-18',
    calibrationDue: false,
    minRange: 0,
    maxRange: 2000,
    optimalMin: 800,
    optimalMax: 1200
  },
  {
    id: 'ph4502c-D4',
    physicalId: 'PH4502C-D4',
    location: 'water-1',
    esp32Id: '6883fff7b079...',
    readFrequency: 300,
    variables: ['pH'],
    status: 'active',
    type: 'ph',
    currentValue: '6.5',
    unit: 'pH',
    lastCalibration: '2024-01-20',
    calibrationDue: false,
    minRange: 0,
    maxRange: 14,
    optimalMin: 5.5,
    optimalMax: 7.0
  },
  {
    id: 'ec-sensor-E5',
    physicalId: 'EC-SENSOR-E5',
    location: 'water-2',
    esp32Id: '6883fff7b079...',
    readFrequency: 300,
    variables: ['NO HAY MAS?'],
    status: 'inactive',
    type: 'conductivity',
    currentValue: '--',
    unit: 'mS/cm',
    lastCalibration: '2024-01-05',
    calibrationDue: true,
    minRange: 0,
    maxRange: 5,
    optimalMin: 1.0,
    optimalMax: 2.5
  }
];

export default function SensorsConfigForm() {
  const router = useRouter();
  const [sensors, setSensors] = useState<SensorData[]>(initialSensors);
  const [activeTab, setActiveTab] = useState('Sensores');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSensor, setEditingSensor] = useState<SensorData | undefined>(undefined);

  const handleSensorToggle = (sensorId: string) => {
    setSensors(prev => prev.map(sensor => 
      sensor.id === sensorId 
        ? { ...sensor, status: sensor.status === 'active' ? 'inactive' : 'active' }
        : sensor
    ));
  };

  const handleSensorEdit = (sensorId: string) => {
    const sensor = sensors.find(s => s.id === sensorId);
    if (sensor) {
      setEditingSensor(sensor);
      setIsModalOpen(true);
    }
  };

  const handleSensorDelete = (sensorId: string) => {
    setSensors(prev => prev.filter(sensor => sensor.id !== sensorId));
  };

  const handleAddSensor = () => {
    setEditingSensor(undefined);
    setIsModalOpen(true);
  };

  const getCurrentDate = (): string => {
    return new Date().toISOString().split('T')[0] || new Date().toLocaleDateString('en-CA');
  };

  const handleFormSubmit = (sensorData: Omit<SensorData, 'id'>) => {
    if (editingSensor) {
      // Editar sensor existente
      setSensors(prev => prev.map(sensor => 
        sensor.id === editingSensor.id 
          ? { ...sensor, ...sensorData }
          : sensor
      ));
    } else {
      // Crear nuevo sensor
      const newSensor: SensorData = {
        id: `SENS-${String(sensors.length + 1).padStart(3, '0')}`,
        ...sensorData,
        lastCalibration: getCurrentDate(),
        calibrationDue: false,
        currentValue: '0',
        unit: 'unit'
      };
      setSensors(prev => [...prev, newSensor]);
    }
    setIsModalOpen(false);
    setEditingSensor(undefined);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingSensor(undefined);
  };

  // Eliminada la navegación por pestañas - ahora se maneja en el SideNavigation

  return (
    <div className="bg-white min-h-screen">
      {/* Header con título y botón agregar */}
      <div className="bg-green-50 px-6 py-6">
        <div className="flex flex-col lg:flex-row lg:justify-between lg:items-center gap-4 mb-6">
          <PageHeader 
            title="Configuración de Sensores"
            subtitle="Administra y monitorea los sensores del invernadero"
            alignment="left"
            className="text-center lg:text-left"
          />
          <button
              onClick={handleAddSensor}
              className="px-6 py-3 rounded-lg flex items-center justify-center gap-2 transition-colors mx-auto lg:mx-0 w-fit font-semibold btn-hidro-add"
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#A8E6A8'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#BEEEBE'}
            >
            <span className="text-xl font-bold flex items-center justify-center icon-hidro-green">+</span>
            AGREGAR SENSOR
          </button>
        </div>

        {/* Navegación por pestañas eliminada - ahora se maneja en el SideNavigation */}
      </div>

      {/* Contenido principal */}
      <div className="p-4 sm:p-8">
        {/* Vista de tarjetas para móvil */}
        <div className="grid grid-cols-1 lg:hidden gap-6">
          {sensors.map((sensor) => (
            <SensorCard
              key={sensor.id}
              sensor={sensor}
              onSelect={() => handleSensorEdit(sensor.id)}
              onToggle={() => handleSensorToggle(sensor.id)}
              onCalibrate={() => handleSensorEdit(sensor.id)}
              isSelected={false}
              isCalibrating={false}
              isMobile={true}
            />
          ))}
        </div>

        {/* Vista de tabla para escritorio */}
        <div className="hidden lg:block">
          <SensorsTable 
            sensors={sensors} 
            onEdit={handleSensorEdit} 
            onDelete={handleSensorToggle} 
          />
        </div>
      </div>

      {/* Modal para crear/editar sensor */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleModalClose}
        title={editingSensor ? 'Editar Sensor' : 'Crear Nuevo Sensor'}
        maxWidth="xl"
      >
        <SensorForm
          {...(editingSensor && { sensor: editingSensor })}
          onSubmit={handleFormSubmit}
          onCancel={handleModalClose}
          isEditing={!!editingSensor}
        />
      </Modal>
    </div>
  );
}