'use client';

import React from 'react';
import { CalendarIcon, ClockIcon, SensorIcon, FilterIcon } from '@/components/ui/icons/Icons';

interface FilterControlsProps {
  selectedDate: string;
  setSelectedDate: (date: string) => void;
  selectedTime: string;
  setSelectedTime: (time: string) => void;
  selectedSensor: string;
  setSelectedSensor: (sensor: string) => void;
  filterValue: string;
  setFilterValue: (value: string) => void;
}

export default function FilterControls({ 
  selectedDate, 
  setSelectedDate, 
  selectedTime, 
  setSelectedTime, 
  selectedSensor, 
  setSelectedSensor, 
  filterValue, 
  setFilterValue 
}: FilterControlsProps) {
  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Filtro de Fecha */}
        <div className="flex flex-col space-y-2">
          <div className="flex items-center bg-gray-100 px-3 py-2 rounded-md">
            <CalendarIcon size={16} className="text-gray-700 mr-2" />
            <span className="text-sm font-medium text-gray-800">Fecha</span>
          </div>
          <input
            type="date"
            value={selectedDate}
            onChange={(e) => setSelectedDate(e.target.value)}
            className="border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-green-500"
          />
        </div>

        {/* Filtro de Hora */}
        <div className="flex flex-col space-y-2">
          <div className="flex items-center bg-gray-100 px-3 py-2 rounded-md">
            <ClockIcon size={16} className="text-gray-700 mr-2" />
            <span className="text-sm font-medium text-gray-800">Hora</span>
          </div>
          <input
            type="time"
            value={selectedTime}
            onChange={(e) => setSelectedTime(e.target.value)}
            className="border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-green-500"
          />
        </div>

        {/* Filtro de Sensor */}
        <div className="flex flex-col space-y-2">
          <div className="flex items-center bg-gray-100 px-3 py-2 rounded-md">
            <SensorIcon size={16} className="text-gray-800 mr-2" />
            <span className="hidro-text-primary text-sm">Sensor</span>
          </div>
          <select
            value={selectedSensor}
            onChange={(e) => setSelectedSensor(e.target.value)}
            className="hidro-select text-sm"
          >
            <option value="">Todos</option>
            <option value="temperatura">Temperatura</option>
            <option value="humedad">Humedad</option>
            <option value="luz">Intensidad Lumínica</option>
            <option value="conductividad">Conductividad Eléctrica</option>
          </select>
        </div>

        {/* Filtro de Valor */}
        <div className="flex flex-col space-y-2">
          <div className="flex items-center bg-gray-100 px-3 py-2 rounded-md">
            <FilterIcon size={16} className="text-gray-800 mr-2" />
            <span className="hidro-text-primary text-sm">Valor</span>
          </div>
          <div className="flex flex-col space-y-2">
            <input
              type="text"
              value={filterValue}
              onChange={(e) => setFilterValue(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-800 focus:outline-none focus:ring-2 focus:ring-green-500"
              placeholder="Ingrese valor"
            />
            <div className="flex flex-wrap gap-1">
              <button className="px-2 py-1 text-sm font-bold bg-green-200 text-green-800 rounded hover:bg-green-300 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors">&gt;</button>
              <button className="px-2 py-1 text-sm font-bold bg-green-200 text-green-800 rounded hover:bg-green-300 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors">&gt;=</button>
              <button className="px-2 py-1 text-sm font-bold bg-green-200 text-green-800 rounded hover:bg-green-300 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors">=</button>
              <button className="px-2 py-1 text-sm font-bold bg-green-200 text-green-800 rounded hover:bg-green-300 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors">&lt;=</button>
              <button className="px-2 py-1 text-sm font-bold bg-green-200 text-green-800 rounded hover:bg-green-300 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors">&lt;</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}