'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { 
  UserIcon, 
  PlantIcon, 
  BellIcon,
  MenuIcon
} from '@/components/ui/icons/Icons';

interface DashboardHeaderProps {
  onToggleSidebar?: () => void;
}

const DashboardHeader: React.FC<DashboardHeaderProps> = ({ onToggleSidebar }) => {
  const pathname = usePathname();


  return (
    <header className="bg-white shadow-sm border-b border-gray-200 w-full" role="banner">
      <div className="w-full px-4 sm:px-6 lg:px-8">
        
        {/* Única fila: Logo + Menú + Notificaciones/Usuario */}
        <div className="flex justify-between items-center h-16">
          {/* Logo y Menú */}
          <div className="flex items-center space-x-4">
            <button
              onClick={onToggleSidebar}
              className="w-8 h-8 bg-green-600 rounded-md flex items-center justify-center mr-3 hover:bg-green-700 transition-colors cursor-pointer"
              title="Alternar menú lateral"
            >
              <span className="text-white font-bold text-lg">H</span>
            </button>
            <Link href="/dashboard" className="flex-shrink-0 flex items-center">
              <span className="text-green-600 font-bold text-xl font-inter whitespace-nowrap min-w-[140px]">HydroEspinaca</span>
            </Link>
          </div>

          {/* Notificaciones + Usuario */}
          <div className="flex items-center space-x-4">
            {/* Usuario */}
            <div className="flex items-center space-x-2">
              <div className="w-8 h-8 bg-gray-800 rounded-full flex items-center justify-center">
                <UserIcon size={16} color="white" />
              </div>
              <div className="hidden md:block">
                <p className="text-sm font-medium text-gray-900 font-inter">John Doe</p>
                <p className="text-xs text-gray-500 font-inter">Farm Manager</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};

export default DashboardHeader;