import React, { ReactNode } from 'react';
import Link from 'next/link';
import { useAuthStore } from '@hydroespinaca/shared';
import { UserIcon, LightningIcon, PlantIcon } from '@/components/ui/icons/Icons';

interface MainLayoutProps {
  children: ReactNode;
}

const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const { isAuthenticated, logout } = useAuthStore();

  return (
    <div className="flex flex-col min-h-screen font-inter">
      {/* Header */}
      <header className="bg-white shadow-sm" role="banner">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16 items-center">
            <div className="flex items-center">
              <Link href="/" className="flex-shrink-0 flex items-center">
                <PlantIcon size={24} color="#16a34a" className="mr-2" />
                <span className="text-green-600 font-bold text-xl font-inter">HydroEspinaca</span>
              </Link>
            </div>
            <nav className="flex items-center space-x-4" role="navigation" aria-label="Navegación principal">
              {isAuthenticated ? (
                <Link href="/dashboard" className="hidro-button-primary flex items-center gap-1 font-inter">
                  <LightningIcon size={18} />
                  Ir al panel
                </Link>
              ) : (
                <Link href="/login" className="hidro-button-primary flex items-center gap-1 font-inter">
                  <LightningIcon size={18} />
                  Iniciar sesión
                </Link>
              )}
            </nav>
          </div>
        </div>
      </header>
      
      {/* Main Content */}
      <main className="flex-1" role="main">
        {children}
      </main>

      {/* Footer */}
      <footer className="bg-gray-50 border-t border-gray-200" role="contentinfo">
        <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
            <p className="text-center text-sm text-gray-500 font-inter">
              © 2025 HydroEspinaca. Todos los derechos reservados.
            </p>
        </div>
      </footer>
    </div>
  );
};

export default MainLayout;