import React, { ReactNode } from 'react';
import Link from 'next/link';
import { useAuthStore } from '@hidroespinaca/shared';
import { UserIcon, LightningIcon, PlantIcon } from '@/components/ui/icons/Icons';

interface MainLayoutProps {
  children: ReactNode;
}

const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const { isAuthenticated, logout } = useAuthStore();

  return (
    <div className="flex flex-col min-h-screen font-inter">
      {/* Header - Solo para páginas no autenticadas */}
      {!isAuthenticated && (
        <header className="bg-white shadow-sm" role="banner">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between h-16 items-center">
              <div className="flex items-center">
                <Link href="/" className="flex-shrink-0 flex items-center">
                  <PlantIcon size={24} color="#16a34a" className="mr-2" />
                  <span className="text-green-600 font-bold text-xl font-inter">Hidro Espinaca</span>
                </Link>
              </div>
              <nav className="flex items-center space-x-4" role="navigation" aria-label="Navegación principal">
                <Link href="/login" className="text-gray-700 hover:text-green-600 flex items-center gap-1 font-inter">
                  <LightningIcon size={18} />
                  Iniciar sesión
                </Link>
                <Link 
                  href="/signup"
                  className="hidro-button-primary flex items-center gap-1 font-inter"
                >
                  <UserIcon size={18} />
                  Registrarse
                </Link>
              </nav>
            </div>
          </div>
        </header>
      )}
      
      {/* Main Content */}
      <main className="flex-1" role="main">
        {children}
      </main>
      
      {/* Footer - Solo para páginas no autenticadas */}
      {!isAuthenticated && (
        <footer className="bg-gray-50 border-t border-gray-200" role="contentinfo">
          <div className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
              <div className="col-span-1 md:col-span-2">
                <div className="flex items-center mb-4">
                  <PlantIcon size={24} color="#16a34a" className="mr-2" />
                  <span className="text-green-600 font-bold text-xl font-inter">Hidro Espinaca</span>
                </div>
                <p className="text-gray-600 text-sm font-inter">
                  Sistema inteligente de monitoreo hidropónico para el cultivo de espinacas.
                  Tecnología avanzada para agricultura sostenible.
                </p>
              </div>
              
              <div>
                <h3 className="text-sm font-semibold text-gray-900 tracking-wider uppercase mb-4 font-inter">
                  Enlaces rápidos
                </h3>
                <ul className="space-y-2">
                  <li>
                    <Link href="/about" className="text-gray-600 hover:text-green-600 text-sm font-inter">
                      Acerca de
                    </Link>
                  </li>
                  <li>
                    <Link href="/features" className="text-gray-600 hover:text-green-600 text-sm font-inter">
                      Características
                    </Link>
                  </li>
                  <li>
                    <Link href="/contact" className="text-gray-600 hover:text-green-600 text-sm font-inter">
                      Contacto
                    </Link>
                  </li>
                </ul>
              </div>
              
              <div>
                <h3 className="text-sm font-semibold text-gray-900 tracking-wider uppercase mb-4 font-inter">
                  Contacto
                </h3>
                <div className="space-y-2 text-sm text-gray-600 font-inter">
                  <p>Email: info@hidroespinaca.com</p>
                  <p>Teléfono: +1 (555) 123-4567</p>
                </div>
              </div>
            </div>
            
            <div className="mt-8 pt-8 border-t border-gray-200">
              <p className="text-center text-sm text-gray-500 font-inter">
                © 2024 Hidro Espinaca. Todos los derechos reservados.
              </p>
            </div>
          </div>
        </footer>
      )}
    </div>
  );
};

export default MainLayout;