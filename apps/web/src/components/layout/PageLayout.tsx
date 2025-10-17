'use client';

import React, { ReactNode, useState, useEffect } from 'react';
import SideNavigation from '@/components/layout/SideNavigation';
import BottomNavigation from '@/components/layout/BottomNavigation';

interface PageLayoutProps {
  children: ReactNode;
  title?: string;
  subtitle?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | 'full';
  showBottomNav?: boolean;
  className?: string;
}

const PageLayout: React.FC<PageLayoutProps> = ({
  children,
  title,
  subtitle,
  maxWidth = 'xl',
  showBottomNav = true,
  className = ''
}) => {
  // Inicializar el estado del sidebar con valor por defecto
  // Se sincroniza con localStorage después del montaje del componente
  const [sidebarExpanded, setSidebarExpanded] = useState(false);
  const [isMobileMoreOpen, setIsMobileMoreOpen] = useState(false);
  const [mounted, setMounted] = useState(false);

  // Cargar el estado del sidebar desde localStorage después del montaje (cliente)
  useEffect(() => {
    setMounted(true);
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('sidebarExpanded');
      if (saved !== null) {
        setSidebarExpanded(JSON.parse(saved));
      }
    }
  }, []);

  // Guardar el estado del sidebar en localStorage cuando cambie (solo después del montaje)
  useEffect(() => {
    if (mounted && typeof window !== 'undefined') {
      localStorage.setItem('sidebarExpanded', JSON.stringify(sidebarExpanded));
    }
  }, [sidebarExpanded, mounted]);

  // Toggle del menú móvil (abrir/cerrar)
  const handleMoreToggle = () => {
    setIsMobileMoreOpen(prev => !prev);
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

  const contentMarginClass = sidebarExpanded ? 'lg:ml-64' : 'lg:ml-16';

  return (
    <div className="min-h-screen bg-gradient-to-br from-green-50 to-green-100">
      {/* Side Navigation */}
      <SideNavigation
        isExpanded={sidebarExpanded}
        onToggleExpand={setSidebarExpanded}
        isMobileMoreOpen={isMobileMoreOpen}
        setMobileMoreOpen={setIsMobileMoreOpen}
      />

      {/* Contenedor principal para el contenido */}
      <div className={`transition-all duration-300 ease-in-out ${contentMarginClass}`}>
        {/* Main Content */}
        <main className={`px-4 py-6 pb-20 lg:pb-8 ${className}`} role="main">
          <div className={`${getMaxWidthClass()} mx-auto`}>
            {/* Título principal */}
            {(title || subtitle) && (
              <header className="text-center mb-8">
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

      {/* Bottom Navigation - Móvil */}
      {showBottomNav && (
        <BottomNavigation
          onMoreClick={handleMoreToggle}
          isMoreMenuOpen={isMobileMoreOpen}
        />
      )}
    </div>
  );
};

export default PageLayout;