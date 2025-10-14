'use client';

import React, { ReactNode, useState } from 'react';
import DashboardHeader from '@/components/dashboard/DashboardHeader';
import SideNavigation from '@/components/layout/SideNavigation';
import BottomNavigation from '@/components/layout/BottomNavigation';

interface PageLayoutProps {
  children: ReactNode;
  title?: string;
  subtitle?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  showHeader?: boolean;
  showBottomNav?: boolean;
  className?: string;
}

const PageLayout: React.FC<PageLayoutProps> = ({
  children,
  title,
  subtitle,
  maxWidth = 'xl',
  showHeader = true,
  showBottomNav = true,
  className = ''
}) => {
  const [sidebarToggle, setSidebarToggle] = useState(false);
  const [isMobileMoreOpen, setIsMobileMoreOpen] = useState(false);

  const handleToggleSidebar = () => {
    setSidebarToggle(!sidebarToggle);
  };

  // CAMBIO 1: La función ahora solo abre el menú, no lo alterna.
  const handleMoreClick = () => {
    setIsMobileMoreOpen(true);
  };

  const getMaxWidthClass = () => {
    switch (maxWidth) {
      case 'sm': return 'max-w-2xl';
      case 'md': return 'max-w-4xl';
      case 'lg': return 'max-w-6xl';
      case 'xl': return 'max-w-7xl';
      case 'full': return 'max-w-full';
      default: return 'max-w-7xl';
    }
  };

  // CAMBIO 2: Las clases de margen ahora son dinámicas y responden al estado `sidebarToggle`.
  const contentMarginClass = sidebarToggle ? 'lg:ml-64' : 'lg:ml-16';

  return (
    <div className="min-h-screen bg-gradient-to-br from-green-50 to-green-100">
      {/* Side Navigation */}
      <SideNavigation 
        toggleTrigger={sidebarToggle} 
        isMobileMoreOpen={isMobileMoreOpen} 
        setMobileMoreOpen={setIsMobileMoreOpen} 
      />
      
      {/* Contenedor principal para el contenido que se desplaza */}
      <div className={`transition-all duration-300 ease-in-out ${contentMarginClass}`}>
        {/* Header */}
        {showHeader && (
          <DashboardHeader onToggleSidebar={handleToggleSidebar} />
        )}
        
        {/* Main Content */}
        <main className={`px-4 pb-20 lg:pb-8 ${showHeader ? 'pt-0' : 'pt-6'} ${className}`} role="main">
          <div className={`${getMaxWidthClass()} mx-auto`}>
            {/* Título principal */}
            {(title || subtitle) && (
              <header className="text-center mb-8 pt-6">
                {title && (
                  <h1 className="text-2xl lg:text-3xl font-bold text-green-800 mb-2 font-inter">
                    {title}
                  </h1>
                )}
                {subtitle && (
                  <p className="text-gray-600 font-inter">
                    {subtitle}
                  </p>
                )}
              </header>
            )}
            
            {/* Contenido */}
            {children}
          </div>
        </main>
      </div>
      
      {/* Bottom Navigation */}
      {/* CAMBIO 3 (EL MÁS IMPORTANTE): Se renderiza condicionalmente basado en isMobileMoreOpen */}
      {showBottomNav && !isMobileMoreOpen && (
        <BottomNavigation onMoreClick={handleMoreClick} />
      )}
    </div>
  );
};

export default PageLayout;