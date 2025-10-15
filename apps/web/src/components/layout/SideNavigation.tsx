'use client';

import React, { useState, useCallback } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  ChartIcon,
  BookIcon,
  TrendingUpIcon,
  BrainIcon,
  SettingsIcon,
  ChevronRightIcon,
  BoltIcon,
  ListIcon,
  PlantIcon,
  MenuIcon,
  PowerIcon,
  UserIcon,
  ChevronDownIcon
} from '@/components/ui/icons/Icons';
import { useAuthStore } from '@hydroespinaca/shared';
import { useRouter } from 'next/navigation';

interface NavigationItem {
  href: string;
  label: string;
  icon: React.ComponentType<any>;
  children?: NavigationItem[];
  status?: string;
}

interface SideNavigationProps {
  isExpanded: boolean;
  onToggleExpand: (expanded: boolean) => void;
  isMobileMoreOpen: boolean;
  setMobileMoreOpen: (isOpen: boolean) => void;
}

const SideNavigation: React.FC<SideNavigationProps> = ({
  isExpanded,
  onToggleExpand,
  isMobileMoreOpen,
  setMobileMoreOpen
}) => {
  const pathname = usePathname();
  const router = useRouter();
  const { logout, user } = useAuthStore();
  const [expandedGroups, setExpandedGroups] = useState<string[]>([]);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isLogoHovered, setIsLogoHovered] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState<string | null>(null);
  const [popoverPosition, setPopoverPosition] = useState<{ top: number; left: number }>({ top: 0, left: 0 });

  // State para el menú del usuario cuando está contraído
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [userMenuPosition, setUserMenuPosition] = useState<{ top: number; left: number }>({ top: 0, left: 0 });

  const navigationItems: NavigationItem[] = [
    { href: '/dashboard', label: 'Dashboard', icon: ChartIcon },
    { href: '/dashboard/lecturas', label: 'Lecturas', icon: BookIcon },
    { href: '/dashboard-monitoreo', label: 'Análisis de datos', icon: TrendingUpIcon },
    { href: '/dashboard/sistemas-fuzzy', label: 'Sistemas Fuzzy', icon: BrainIcon },
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
  ];

  // En móvil solo se muestra el Dashboard principal
  const mobileMainItems = navigationItems.slice(0, 1); // Solo Dashboard
  const mobileMoreItems = navigationItems.slice(1); // Resto de items

  const toggleGroup = (label: string) => {
    setExpandedGroups(prev =>
      prev.includes(label)
        ? prev.filter(group => group !== label)
        : [...prev, label]
    );
  };

  const toggleSidebar = () => {
    onToggleExpand(!isExpanded);
  };

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

  const handleLogout = async () => {
    try {
      await logout();
    } finally {
      router.push('/login');
    }
  };


  const handlePopoverToggle = (label: string, event: React.MouseEvent<HTMLButtonElement>) => {
    if (!isExpanded) {
      const rect = event.currentTarget.getBoundingClientRect();
      setPopoverPosition({
        top: rect.top,
        left: rect.right + 8 // 8px gap from sidebar
      });
      setPopoverOpen(popoverOpen === label ? null : label);
    }
  };

  const handlePopoverClose = () => {
    setPopoverOpen(null);
  };

  const handleUserMenuToggle = (event: React.MouseEvent<HTMLButtonElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const menuHeight = 140; // Altura aproximada del menú

    // Calcular posición centrada verticalmente con el botón
    let topPosition = rect.top + (rect.height / 2) - (menuHeight / 2);

    // Asegurar que no se salga por arriba
    topPosition = Math.max(8, topPosition);

    // Asegurar que no se salga por abajo
    const maxTop = window.innerHeight - menuHeight - 8;
    topPosition = Math.min(topPosition, maxTop);

    setUserMenuPosition({
      top: topPosition,
      left: rect.right + 8 // 8px gap from sidebar
    });
    setUserMenuOpen(!userMenuOpen);
  };

  const handleUserMenuClose = () => {
    setUserMenuOpen(false);
  };

  return (
    <>
      {/* Desktop Navigation */}
      <aside
        className={`hidden lg:flex lg:flex-col fixed left-0 top-0 h-full bg-white border-r border-gray-200 z-50 transition-all duration-300 ease-in-out ${isExpanded ? 'w-64' : 'w-16'
          }`}
      >
        {/* Header con Logo y Botón Colapsar */}
        <div className="h-16 border-b border-gray-200 flex items-center justify-center px-3">
          {isExpanded ? (
            <div className="flex items-center justify-between w-full">
              <Link href="/dashboard" className="flex items-center space-x-2">
                <PlantIcon size={24} color="#16a34a" />
                <span className="text-green-600 font-bold text-lg whitespace-nowrap">
                  HydroEspinaca
                </span>
              </Link>
              <button
                onClick={toggleSidebar}
                className="p-2 text-gray-600 hover:text-green-600 hover:bg-gray-100 rounded transition-colors"
                title="Colapsar menú"
                aria-label="Colapsar menú lateral"
              >
                <ChevronRightIcon size={20} />
              </button>
            </div>
          ) : (
            <div
              className="relative w-full h-full flex items-center justify-center cursor-pointer"
              onMouseEnter={() => setIsLogoHovered(true)}
              onMouseLeave={() => setIsLogoHovered(false)}
              onClick={toggleSidebar}
            >
              {isLogoHovered ? (
                <ChevronRightIcon size={24} className="text-green-600 rotate-180" />
              ) : (
                <PlantIcon size={28} color="#16a34a" />
              )}
            </div>
          )}
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
                        onClick={(e) => {
                          if (isExpanded) {
                            toggleGroup(item.label);
                          } else {
                            handlePopoverToggle(item.label, e);
                          }
                        }}
                        className={`relative w-full flex items-center px-4 py-3 text-left transition-colors duration-200 ${itemActive
                            ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                            : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                          } ${!isExpanded && popoverOpen === item.label ? 'bg-green-50' : ''}`}
                      >
                        <IconComponent size={20} className="flex-shrink-0" />
                        {isExpanded && (
                          <>
                            <span className="ml-3 font-medium whitespace-nowrap">{item.label}</span>
                            <ChevronRightIcon
                              size={16}
                              className={`ml-auto transition-transform duration-200 flex-shrink-0 ${groupExpanded ? 'rotate-90' : ''
                                }`}
                            />
                          </>
                        )}
                      </button>

                      {/* Children items */}
                      {isExpanded && groupExpanded && item.children && (
                        <ul className="ml-4 mt-1 space-y-1">
                          {item.children.map((child) => {
                            const ChildIconComponent = child.icon;
                            const childActive = isActive(child.href);

                            return (
                              <li key={child.href}>
                                <Link
                                  href={child.href}
                                  className={`flex items-center px-4 py-2 text-sm transition-colors duration-200 ${childActive
                                      ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                                      : 'text-gray-600 hover:text-green-600 hover:bg-green-50'
                                    }`}
                                >
                                  <ChildIconComponent size={16} className="flex-shrink-0" />
                                  <span className="ml-3 whitespace-nowrap">{child.label}</span>
                                  {child.status && (
                                    <span className={`ml-auto text-xs px-2 py-1 rounded-full ${child.status === 'Conectado'
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
                      className={`flex items-center px-4 py-3 transition-colors duration-200 ${itemActive
                          ? 'text-green-600 bg-green-50 border-r-2 border-green-600'
                          : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                        }`}
                    >
                      <IconComponent size={20} className="flex-shrink-0" />
                      {isExpanded && (
                        <span className="ml-3 font-medium whitespace-nowrap">{item.label}</span>
                      )}
                    </Link>
                  )}
                </li>
              );
            })}
          </ul>
        </nav>

        {/* Floating Popover for Collapsed Submenus */}
        {!isExpanded && popoverOpen && (
          <>
            {/* Overlay to close popover */}
            <div
              className="fixed inset-0 z-40"
              onClick={handlePopoverClose}
            />
            {/* Popover content */}
            {navigationItems
              .filter(item => item.label === popoverOpen && item.children)
              .map(item => (
                <div
                  key={item.label}
                  className="fixed z-50 bg-white rounded-lg shadow-xl border border-gray-200 py-2 min-w-[200px] animate-in fade-in slide-in-from-left-2 duration-200"
                  style={{
                    top: `${popoverPosition.top}px`,
                    left: `${popoverPosition.left}px`,
                  }}
                >
                  <div className="px-3 py-2 border-b border-gray-200">
                    <p className="text-sm font-semibold text-gray-700">{item.label}</p>
                  </div>
                  <ul className="py-1">
                    {item.children?.map(child => {
                      const ChildIconComponent = child.icon;
                      const childActive = isActive(child.href);
                      return (
                        <li key={child.href}>
                          <Link
                            href={child.href}
                            onClick={handlePopoverClose}
                            className={`flex items-center px-3 py-2 text-sm transition-colors duration-200 ${childActive
                                ? 'text-green-600 bg-green-50'
                                : 'text-gray-700 hover:text-green-600 hover:bg-gray-50'
                              }`}
                          >
                            <ChildIconComponent size={16} className="flex-shrink-0" />
                            <span className="ml-3 whitespace-nowrap">{child.label}</span>
                          </Link>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              ))}
          </>
        )}

        {/* Floating User Menu for Collapsed Sidebar */}
        {!isExpanded && userMenuOpen && (
          <>
            {/* Overlay to close menu */}
            <div
              className="fixed inset-0 z-40"
              onClick={handleUserMenuClose}
            />
            {/* User menu content */}
            <div
              className="fixed z-50 bg-white rounded-lg shadow-xl border border-gray-200 py-2 min-w-[220px] animate-in fade-in scale-in-95 duration-200"
              style={{
                top: `${userMenuPosition.top}px`,
                left: `${userMenuPosition.left}px`,
              }}
            >
              <div className="px-3 py-2 border-b border-gray-200">
                <p className="text-sm font-semibold text-gray-700">
                  {user?.name || 'Usuario'}
                </p>
                <p className="text-xs text-gray-500">
                  {user?.role || 'Administrador'}
                </p>
              </div>
              <ul className="py-1">
                <li>
                  <Link
                    href="/dashboard/perfil"
                    onClick={handleUserMenuClose}
                    className="flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors"
                  >
                    <span className="mr-3">👤</span>
                    <span>Configuración de usuario</span>
                  </Link>
                </li>
                <li>
                  <button
                    onClick={() => {
                      handleUserMenuClose();
                      handleLogout();
                    }}
                    className="w-full flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-red-50 hover:text-red-600 transition-colors"
                  >
                    <span className="mr-3">🚪</span>
                    <span>Cerrar sesión</span>
                  </button>
                </li>
              </ul>
            </div>
          </>
        )}

        {/* User Profile Section */}
        <div className="border-t border-gray-200 p-3">
          {isExpanded ? (
            <div className="flex items-center justify-between">
              <Link
                href="/dashboard/perfil"
                className="flex items-center space-x-3 flex-1 p-2 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <div className="w-10 h-10 bg-green-600 rounded-full flex items-center justify-center flex-shrink-0">
                  <UserIcon size={20} color="white" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {user?.name || 'Usuario'}
                  </p>
                  <p className="text-xs text-gray-500 truncate">
                    {user?.role || 'Administrador'}
                  </p>
                </div>
              </Link>
              <button
                onClick={handleLogout}
                className="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                title="Cerrar sesión"
                aria-label="Cerrar sesión"
              >
                <PowerIcon size={20} />
              </button>
            </div>
          ) : (
            <div className="flex flex-col items-center">
              <button
                onClick={handleUserMenuToggle}
                className={`w-10 h-10 bg-green-600 rounded-full flex items-center justify-center hover:bg-green-700 transition-colors ${userMenuOpen ? 'ring-2 ring-green-300' : ''
                  }`}
                title="Menú de usuario"
                aria-label="Abrir menú de usuario"
              >
                <UserIcon size={20} color="white" />
              </button>
            </div>
          )}
        </div>
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
                    className={`flex items-center px-4 py-3 rounded-md text-gray-700 hover:bg-gray-100 transition-colors ${pathname === item.href ? 'bg-green-50 text-green-600' : ''
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
                              className={`w-full flex items-center px-4 py-3 rounded-md text-left transition-colors ${itemActive
                                  ? 'text-green-600 bg-green-50'
                                  : 'text-gray-700 hover:text-green-600 hover:bg-green-50'
                                }`}
                            >
                              <IconComponent size={20} className="mr-3" />
                              <span className="text-sm font-medium">{item.label}</span>
                              <ChevronRightIcon
                                size={16}
                                className={`ml-auto transition-transform ${groupExpanded ? 'rotate-90' : ''
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
                                    className={`flex items-center px-4 py-2 text-xs text-gray-500 hover:bg-gray-100 rounded transition-colors ${pathname === child.href ? 'bg-green-50 text-green-600' : ''
                                      }`}
                                  >
                                    <child.icon size={14} className="mr-2" />
                                    <span>{child.label}</span>
                                    {child.status && (
                                      <span className={`ml-auto text-xs px-2 py-1 rounded-full ${child.status === 'Conectado'
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
                            className={`flex items-center px-4 py-3 rounded-md text-gray-700 hover:bg-gray-100 transition-colors ${pathname === item.href ? 'bg-green-50 text-green-600' : ''
                              }`}
                          >
                            <IconComponent size={20} className="mr-3" />
                            <span className="text-sm font-medium">{item.label}</span>
                          </Link>
                        )}
                      </div>
                    );
                  })}

                  {/* Mobile User Profile Section */}
                  <div className="mt-4 pt-4 border-t border-gray-200">
                    <div className="flex items-center justify-between">
                      <Link
                        href="/dashboard/perfil"
                        onClick={() => setMobileMoreOpen(false)}
                        className="flex items-center space-x-3 flex-1 p-2 rounded-lg hover:bg-gray-100 transition-colors"
                      >
                        <div className="w-10 h-10 bg-green-600 rounded-full flex items-center justify-center flex-shrink-0">
                          <UserIcon size={20} color="white" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900 truncate">
                            {user?.name || 'Usuario'}
                          </p>
                          <p className="text-xs text-gray-500 truncate">
                            {user?.role || 'Administrador'}
                          </p>
                        </div>
                      </Link>
                      <button
                        onClick={handleLogout}
                        className="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                        title="Cerrar sesión"
                        aria-label="Cerrar sesión"
                      >
                        <PowerIcon size={20} />
                      </button>
                    </div>
                  </div>
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