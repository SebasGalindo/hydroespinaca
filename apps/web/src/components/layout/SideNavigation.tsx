'use client';

import React, { useState, useCallback } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { 
  ChartIcon, 
  BookIcon, 
  TrendingUpIcon, 
  BrainIcon, 
  PlusIcon, 
  SettingsIcon, 
  HistoryIcon,
  ChevronRightIcon,
  BoltIcon,
  ListIcon,
  ExpandIcon,
  CollapseIcon,
  WifiIcon,
  MenuIcon
} from '@/components/ui/icons/Icons';

interface NavigationItem {
  href: string;
  label: string;
  icon: React.ComponentType<any>;
  children?: NavigationItem[];
  status?: string;
}

interface SideNavigationProps {
  toggleTrigger: boolean;
  isMobileMoreOpen: boolean;
  setMobileMoreOpen: (isOpen: boolean) => void;
}

const SideNavigation: React.FC<SideNavigationProps> = ({ toggleTrigger, isMobileMoreOpen, setMobileMoreOpen }) => {
  const pathname = usePathname();
  const [isExpanded, setIsExpanded] = useState(false);
  const [isPermanentlyExpanded, setIsPermanentlyExpanded] = useState(false);
  const [expandedGroups, setExpandedGroups] = useState<string[]>([]);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const navigationItems: NavigationItem[] = [
    { href: '/dashboard', label: 'Info-Main', icon: ChartIcon },
    { href: '/dashboard/lecturas', label: 'Lecturas', icon: BookIcon },
    { href: '/dashboard-monitoreo', label: 'Dashboard', icon: TrendingUpIcon },
    { href: '/dashboard/sistemas-fuzzy', label: 'Sistemas Fuzzy', icon: BrainIcon },
    { href: '/dashboard/nueva-variable', label: 'Crear Variable Manual', icon: PlusIcon },
    {
      href: '/dashboard/listas',
      label: 'Listas',
      icon: ListIcon,
      children: [
        { href: '/dashboard/variables-config', label: 'Variables', icon: BoltIcon },
        { href: '/dashboard/sensores-config', label: 'Sensores', icon: SettingsIcon },
        { href: '/dashboard/actuadores-config', label: 'Actuadores', icon: SettingsIcon },
        { href: '/dashboard/reglas-fuzzy', label: 'Reglas Fuzzy', icon: BrainIcon },
      ]
    },
    {
      href: '/dashboard/sistema',
      label: 'Estado del Sistema',
      icon: WifiIcon,
      children: [
        { href: '/dashboard/sistema/api', label: 'API Principal', icon: WifiIcon, status: 'Conectado' },
        { href: '/dashboard/sistema/database', label: 'Base de Datos', icon: WifiIcon, status: 'Conectado' },
        { href: '/dashboard/sistema/sensors', label: 'Sensores IoT', icon: WifiIcon, status: 'Desconectado' },
      ]
    },
    { href: '/dashboard/ajustes', label: 'Ajustes', icon: SettingsIcon },
    { href: '/dashboard/historial', label: 'Historial', icon: HistoryIcon },
  ];

  const mobileMainItems = navigationItems.slice(0, 5); // Info-Main, Lecturas, Dashboard, Lógica Fuzzy, Crear Variable
  const mobileMoreItems = navigationItems.slice(5, -2); // Listas, Estado del Sistema (sin Ajustes e Historial)

  const toggleGroup = (label: string) => {
    setExpandedGroups(prev => 
      prev.includes(label) 
        ? prev.filter(group => group !== label)
        : [...prev, label]
    );
  };

  const handleMouseEnter = () => {
    if (!isPermanentlyExpanded) {
      setIsExpanded(true);
    }
  };

  const handleMouseLeave = (event: React.MouseEvent) => {
    // Si el ratón se va por el borde izquierdo extremo de la ventana, no hagas nada.
    if (event.clientX <= 0) {
      return;
    }
    
    if (!isPermanentlyExpanded) {
      setIsExpanded(false);
    }
  };

  const togglePermanentExpansion = useCallback(() => {
    setIsPermanentlyExpanded((prev) => {
      const next = !prev;
      setIsExpanded(next);
      return next;
    });
  }, []);

  // Effect to handle external toggle trigger
  React.useEffect(() => {
    if (toggleTrigger !== undefined) {
      setIsPermanentlyExpanded((prev) => {
        const next = !prev;
        setIsExpanded(next);
        return next;
      });
    }
  }, [toggleTrigger]);

  const isActive = (href: string) => {
    if (!pathname) return false;
    if (href === '/dashboard') {
      return pathname === '/dashboard';
    }
    return pathname.startsWith(href);
  };

  const isGroupActive = (item: NavigationItem) => {
    if (item.children) {
      return item.children.some(child => isActive(child.href));
    }
    return isActive(item.href);
  };

  return (
    <>
      {/* Desktop Navigation */}
      <aside 
        className={`hidden lg:block fixed left-0 top-0 h-full bg-white border-r border-gray-200 z-50 transition-all duration-300 ease-in-out overflow-x-hidden scrollbar-hidden ${
          isExpanded || isPermanentlyExpanded ? 'w-80' : 'w-16'
        }`}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
      >

        {/* Toggle button */}
        <div className="p-4 border-b border-gray-200">
          <button
            onClick={togglePermanentExpansion}
            className="w-full flex items-center justify-center p-2 text-gray-600 hover:text-green-600 hover:bg-gray-100 rounded transition-colors"
            title={isPermanentlyExpanded ? 'Colapsar menú' : 'Expandir menú'}
          >
            {isPermanentlyExpanded ? (
              <CollapseIcon size={20} />
            ) : (
              <ExpandIcon size={20} />
            )}
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 py-4 overflow-y-auto overflow-x-hidden">
          <ul className="space-y-1">
            {navigationItems.map((item) => {
              const IconComponent = item.icon;
              const hasChildren = item.children && item.children.length > 0;
              const groupExpanded = expandedGroups.includes(item.label);
              const itemActive = isGroupActive(item);

              return (
                <li key={item.href}>
                  {hasChildren ? (
                    <>
                      {/* Parent item with children */}
                      <button
                        onClick={() => toggleGroup(item.label)}
                        className={`w-full flex items-center px-4 py-3 text-left transition-colors duration-200 ${
                          itemActive
                            ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                            : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                        }`}
                      >
                        <IconComponent size={20} className="flex-shrink-0" />
                        {(isExpanded || isPermanentlyExpanded) && (
                          <>
                            <span className="ml-3 font-medium whitespace-nowrap">{item.label}</span>
                            <ChevronRightIcon 
                              size={16} 
                              className={`ml-auto transition-transform duration-200 flex-shrink-0 ${
                                groupExpanded ? 'rotate-90' : ''
                              }`}
                            />
                          </>
                        )}
                      </button>
                      
                      {/* Children items */}
                      {(isExpanded || isPermanentlyExpanded) && groupExpanded && item.children && (
                        <ul className="ml-4 mt-1 space-y-1">
                          {item.children.map((child) => {
                            const ChildIconComponent = child.icon;
                            const childActive = isActive(child.href);
                            
                            return (
                              <li key={child.href}>
                                <Link
                                  href={child.href}
                                  className={`flex items-center px-4 py-2 text-sm transition-colors duration-200 ${
                                    childActive
                                      ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                                      : 'text-gray-600 hover:text-green-600 hover:bg-green-50'
                                  }`}
                                >
                                  <ChildIconComponent size={16} className="flex-shrink-0" />
                                  <span className="ml-3 whitespace-nowrap">{child.label}</span>
                                  {child.status && (
                                    <span className={`ml-auto text-xs px-2 py-1 rounded-full ${
                                      child.status === 'Conectado' 
                                        ? 'bg-green-100 text-green-600' 
                                        : 'bg-red-100 text-red-600'
                                    }`}>
                                      {child.status}
                                    </span>
                                  )}
                                </Link>
                              </li>
                            );
                          })}
                        </ul>
                      )}
                    </>
                  ) : (
                    /* Regular navigation item */
                    <Link
                      href={item.href}
                      className={`flex items-center px-4 py-3 transition-colors duration-200 ${
                        itemActive
                          ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                          : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                      }`}
                    >
                      <IconComponent size={20} className="flex-shrink-0" />
                      {(isExpanded || isPermanentlyExpanded) && (
                        <span className="ml-3 font-medium whitespace-nowrap">{item.label}</span>
                      )}
                    </Link>
                  )}
                </li>
              );
            })}
          </ul>
        </nav>
      </aside>

      {/* Mobile Navigation */}
      <div className="lg:hidden">


        {/* Mobile menu overlay with blur effect */}
        {isMobileMenuOpen && (
          <div className="fixed inset-0 z-40 bg-black bg-opacity-50" onClick={() => setIsMobileMenuOpen(false)} />
        )}

        {/* Mobile menu */}
        {isMobileMenuOpen && (
          <div className="fixed left-0 top-0 h-full w-80 bg-white shadow-lg z-50 transform transition-transform">
            <div className="p-4 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-800">Navegación</h2>
                <button
                  onClick={() => setIsMobileMenuOpen(false)}
                  className="p-2 text-gray-600 hover:text-gray-800"
                >
                  ×
                </button>
              </div>
            </div>
            
            <div className="flex-1 overflow-y-auto">
              {/* Main mobile items */}
              <div className="p-4 space-y-2">
                {mobileMainItems.map((item) => (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={() => setIsMobileMenuOpen(false)}
                    className={`flex items-center px-4 py-3 rounded-md text-gray-700 hover:bg-gray-100 transition-colors ${
                      pathname === item.href ? 'bg-green-50 text-green-600' : ''
                    }`}
                  >
                    <item.icon size={20} className="mr-3" />
                    <span className="text-sm font-medium">{item.label}</span>
                  </Link>
                ))}
                
                {/* More Options button - opens full sidebar */}
                <button
                  onClick={() => {
                    setIsMobileMenuOpen(false);
                    setMobileMoreOpen(true);
                  }}
                  className="w-full flex items-center px-4 py-3 rounded-md text-gray-700 hover:bg-gray-100 transition-colors"
                >
                  <MenuIcon size={20} className="mr-3" />
                  <span className="text-sm font-medium">Más opciones</span>
                  <ChevronRightIcon size={16} className="ml-auto" />
                </button>
                

              </div>
            </div>
          </div>
        )}

        {/* Full Sidebar for "More Options" */}
        {isMobileMoreOpen && (
          <>
            {/* Overlay */}
            <div className="fixed inset-0 z-40 bg-transparent backdrop-blur-xs" onClick={() => setMobileMoreOpen(false)} />
            
            {/* Full Sidebar */}
            <div className="fixed left-0 top-0 h-full w-100 bg-white shadow-lg z-50 transform transition-transform overflow-y-auto scrollbar-hidden">
              <div className="p-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h2 className="text-lg font-semibold text-gray-800">Todas las opciones</h2>
                  <button
                    onClick={() => setMobileMoreOpen(false)}
                    className="p-2 text-gray-600 hover:text-gray-800"
                  >
                    ×
                  </button>
                </div>
              </div>
              
              <div className="flex-1 overflow-y-auto">
                <div className="p-4 space-y-2">
                  {navigationItems.map((item) => {
                    const IconComponent = item.icon;
                    const hasChildren = item.children && item.children.length > 0;
                    const groupExpanded = expandedGroups.includes(item.label);
                    const itemActive = isGroupActive(item);

                    return (
                      <div key={item.href}>
                        {hasChildren ? (
                          <>
                            <button
                              onClick={() => toggleGroup(item.label)}
                              className={`w-full flex items-center px-4 py-3 rounded-md text-left transition-colors ${
                                itemActive
                                  ? 'text-green-600 bg-green-50'
                                  : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                              }`}
                            >
                              <IconComponent size={20} className="mr-3" />
                              <span className="text-sm font-medium">{item.label}</span>
                              <ChevronRightIcon 
                                size={16} 
                                className={`ml-auto transition-transform ${
                                  groupExpanded ? 'rotate-90' : ''
                                }`} 
                              />
                            </button>
                            
                            {groupExpanded && item.children && (
                              <div className="ml-8 space-y-1">
                                {item.children.map((child) => (
                                  <Link
                                    key={child.href}
                                    href={child.href}
                                    onClick={() => setMobileMoreOpen(false)}
                                    className={`flex items-center px-4 py-2 text-xs text-gray-500 hover:bg-gray-100 rounded transition-colors ${
                                      pathname === child.href ? 'bg-green-50 text-green-600' : ''
                                    }`}
                                  >
                                    <child.icon size={14} className="mr-2" />
                                    <span>{child.label}</span>
                                    {child.status && (
                                      <span className={`ml-auto text-xs px-2 py-1 rounded-full ${
                                        child.status === 'Conectado' 
                                          ? 'bg-green-100 text-green-600' 
                                          : 'bg-red-100 text-red-600'
                                      }`}>
                                        {child.status}
                                      </span>
                                    )}
                                  </Link>
                                ))}
                              </div>
                            )}
                          </>
                        ) : (
                          <Link
                            href={item.href}
                            onClick={() => setMobileMoreOpen(false)}
                            className={`flex items-center px-4 py-3 rounded-md text-gray-700 hover:bg-gray-100 transition-colors ${
                              pathname === item.href ? 'bg-green-50 text-green-600' : ''
                            }`}
                          >
                            <IconComponent size={20} className="mr-3" />
                            <span className="text-sm font-medium">{item.label}</span>
                          </Link>
                        )}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  );
};

export default SideNavigation;