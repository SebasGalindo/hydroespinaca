'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import {
  ShieldIcon,
  SproutIcon,
  ChartIcon,
  TrendingUpIcon,
  BrainIcon,
  SettingsIcon,
  ChevronRightIcon,
  BoltIcon,
  ListIcon,
  PlantIcon,
  PowerIcon
} from '@/components/ui/icons/Icons';
import { useAuthStore } from '@hydroespinaca/shared';

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
  const [isLogoHovered, setIsLogoHovered] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState<string | null>(null);
  const [popoverPosition, setPopoverPosition] = useState<{ top: number; left: number }>({ top: 0, left: 0 });

  // State para el menú del usuario cuando está contraído
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [userMenuPosition, setUserMenuPosition] = useState<{ top: number; left: number }>({ top: 0, left: 0 });

  const navigationItems: NavigationItem[] = [
    { href: '/dashboard', label: 'Dashboard', icon: ChartIcon },
    { href: '/analytics', label: 'Análisis de datos', icon: TrendingUpIcon },
  ];

  // Admin menu items (only for Administrador role)
  const adminMenuItems: NavigationItem[] = user?.role === 'Administrador' ? [
    {
      href: '/admin',
      label: 'Administración',
      icon: ShieldIcon,
      children: [
        { href: '/admin/access', label: 'Gestión de Acceso', icon: SettingsIcon },
        { href: '/admin/dashboard', label: 'Dashboard Admin', icon: ChartIcon },
      ]
    },
  ] : [];

  // Combine navigation items
  const allNavigationItems = [...navigationItems, ...adminMenuItems];

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
            {allNavigationItems.map((item) => {
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
            {allNavigationItems
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
                <p className="text-sm font-semibold text-gray-700" title={user?.name}>
                  {user?.name || 'Usuario'}
                </p>
                <p className="text-xs text-gray-500" title={user?.email}>
                  {user?.role || 'Usuario'}
                </p>
              </div>
              <ul className="py-1">
                <li>
                  <Link
                    href="/perfil"
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
                href="/perfil"
                className="flex items-center space-x-3 flex-1 p-2 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <div className="w-10 h-10 bg-green-600 rounded-full flex items-center justify-center flex-shrink-0">
                  {user?.role === 'Administrador' ? (
                    <ShieldIcon size={20} color="white" />
                  ) : (
                    <SproutIcon size={20} color="white" />
                  )}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate" title={user?.name}>
                    {user?.name || 'Usuario'}
                  </p>
                  <p className="text-xs text-gray-500 truncate" title={user?.email}>
                    {user?.role || 'Usuario'}
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
                title={user?.name || 'Menú de usuario'}
                aria-label="Abrir menú de usuario"
              >
                {user?.role === 'Administrador' ? (
                  <ShieldIcon size={20} color="white" />
                ) : (
                  <SproutIcon size={20} color="white" />
                )}
              </button>
            </div>
          )}
        </div>
      </aside>

      {/* Mobile Navigation */}
      <div className="lg:hidden">
        {/* Full Sidebar for "More Options" */}
        {isMobileMoreOpen && (
          <>
            {/* Overlay con backdrop blur y animación de fade-in */}
            <div
              className="fixed inset-0 z-40 bg-black/20 backdrop-blur-sm animate-in fade-in duration-300"
              onClick={() => setMobileMoreOpen(false)}
              aria-label="Cerrar menú"
            />

            {/* Drawer desde la derecha con animación slide-in */}
            <div
              id="mobile-drawer-menu"
              className="fixed right-0 top-0 h-full w-80 max-w-[85vw] bg-white shadow-2xl z-50 overflow-y-auto animate-in slide-in-from-right duration-300 ease-out"
              role="dialog"
              aria-modal="true"
              aria-labelledby="mobile-drawer-title"
            >
              {/* Header del drawer */}
              <div className="sticky top-0 bg-gradient-to-r from-green-600 to-green-700 p-4 border-b border-green-800 shadow-md z-10">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <div className="p-1.5 bg-white/20 rounded-lg backdrop-blur-sm">
                      <PlantIcon size={22} color="white" />
                    </div>
                    <div>
                      <h2 id="mobile-drawer-title" className="text-base font-bold text-white leading-tight">
                        Menú de Navegación
                      </h2>
                      <p className="text-xs text-green-100">HydroEspinaca</p>
                    </div>
                  </div>
                  <button
                    onClick={() => setMobileMoreOpen(false)}
                    className="p-2 text-white hover:bg-white/20 rounded-full transition-all duration-200 active:scale-90"
                    aria-label="Cerrar menú de navegación"
                    title="Cerrar menú"
                  >
                    <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              {/* Content Area */}
              <div className="flex-1 overflow-y-auto px-3 py-4">
                {/* Título de sección */}
                <div className="px-3 mb-3">
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Navegación</p>
                </div>

                <div className="space-y-1">
                  {allNavigationItems.map((item) => {
                    const IconComponent = item.icon;
                    const hasChildren = item.children && item.children.length > 0;
                    const groupExpanded = expandedGroups.includes(item.label);
                    const itemActive = isGroupActive(item);

                    return (
                      <div key={item.href} className="mb-1">
                        {hasChildren ? (
                          <>
                            {/* Parent item con submenú */}
                            <button
                              onClick={() => toggleGroup(item.label)}
                              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left transition-all duration-200 ${
                                itemActive
                                  ? 'bg-gradient-to-r from-green-50 to-green-100 text-green-700 shadow-sm'
                                  : 'text-gray-700 hover:bg-gray-50 active:bg-gray-100'
                              }`}
                            >
                              <div className={`p-1.5 rounded-lg ${itemActive ? 'bg-green-200/50' : 'bg-gray-100'}`}>
                                <IconComponent size={18} />
                              </div>
                              <span className="text-sm font-medium flex-1">{item.label}</span>
                              <ChevronRightIcon
                                size={16}
                                className={`transition-transform duration-200 ${groupExpanded ? 'rotate-90' : ''}`}
                              />
                            </button>

                            {/* Submenú expandible */}
                            {groupExpanded && item.children && (
                              <div className="mt-1 ml-3 pl-6 border-l-2 border-green-200 space-y-1">
                                {item.children.map((child) => {
                                  const ChildIcon = child.icon;
                                  const childActive = pathname === child.href;

                                  return (
                                    <Link
                                      key={child.href}
                                      href={child.href}
                                      onClick={() => setMobileMoreOpen(false)}
                                      className={`flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200 ${
                                        childActive
                                          ? 'bg-green-600 text-white shadow-md'
                                          : 'text-gray-600 hover:bg-gray-50 active:bg-gray-100'
                                      }`}
                                    >
                                      <ChildIcon size={16} />
                                      <span className="text-sm font-medium">{child.label}</span>
                                      {child.status && (
                                        <span className={`ml-auto text-xs px-2 py-1 rounded-full font-medium ${
                                          child.status === 'Conectado'
                                            ? 'bg-green-100 text-green-700'
                                            : 'bg-red-100 text-red-700'
                                        }`}>
                                          {child.status}
                                        </span>
                                      )}
                                      {childActive && (
                                        <div className="ml-auto w-1.5 h-1.5 rounded-full bg-white animate-pulse" />
                                      )}
                                    </Link>
                                  );
                                })}
                              </div>
                            )}
                          </>
                        ) : (
                          /* Item simple sin submenú */
                          <Link
                            href={item.href}
                            onClick={() => setMobileMoreOpen(false)}
                            className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-all duration-200 ${
                              pathname === item.href
                                ? 'bg-gradient-to-r from-green-50 to-green-100 text-green-700 shadow-sm'
                                : 'text-gray-700 hover:bg-gray-50 active:bg-gray-100'
                            }`}
                          >
                            <div className={`p-1.5 rounded-lg ${pathname === item.href ? 'bg-green-200/50' : 'bg-gray-100'}`}>
                              <IconComponent size={18} />
                            </div>
                            <span className="text-sm font-medium flex-1">{item.label}</span>
                            {pathname === item.href && (
                              <div className="w-2 h-2 rounded-full bg-green-600 animate-pulse" />
                            )}
                          </Link>
                        )}
                      </div>
                    );
                  })}

                  {/* Sección de Usuario */}
                  <div className="mt-6">
                    <div className="px-3 mb-3">
                      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Cuenta</p>
                    </div>

                    {/* Card del perfil de usuario */}
                    <div className="bg-gradient-to-br from-green-50 to-green-100/50 rounded-xl p-4 mb-3 border border-green-200/50">
                      <Link
                        href="/perfil"
                        onClick={() => setMobileMoreOpen(false)}
                        className="flex items-center gap-3 group"
                      >
                        <div className="relative">
                          <div className="w-14 h-14 bg-gradient-to-br from-green-600 to-green-700 rounded-xl flex items-center justify-center shadow-lg group-hover:shadow-xl transition-shadow">
                            {user?.role === 'Administrador' ? (
                              <ShieldIcon size={24} color="white" />
                            ) : (
                              <SproutIcon size={24} color="white" />
                            )}
                          </div>
                          {/* Indicador de estado online */}
                          <div className="absolute -bottom-0.5 -right-0.5 w-4 h-4 bg-green-500 rounded-full border-2 border-white shadow-sm" />
                        </div>

                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-bold text-gray-900 truncate mb-0.5" title={user?.name}>
                            {user?.name || 'Usuario'}
                          </p>
                          <p className="text-xs text-gray-600 truncate mb-1" title={user?.email}>
                            {user?.email || 'email@example.com'}
                          </p>
                          <div className="inline-flex items-center gap-1.5 px-2 py-0.5 bg-white/80 rounded-full">
                            <div className="w-1.5 h-1.5 rounded-full bg-green-600" />
                            <span className="text-xs font-medium text-gray-700">
                              {user?.role || 'Usuario'}
                            </span>
                          </div>
                        </div>

                        <ChevronRightIcon size={18} className="text-gray-400 group-hover:text-green-600 transition-colors" />
                      </Link>
                    </div>

                    {/* Botón de logout con diseño destacado */}
                    <button
                      onClick={() => {
                        setMobileMoreOpen(false);
                        handleLogout();
                      }}
                      className="w-full flex items-center justify-center gap-3 px-4 py-3 rounded-lg bg-red-50 text-red-600 hover:bg-red-100 active:bg-red-200 transition-all duration-200 font-semibold shadow-sm hover:shadow group"
                      aria-label="Cerrar sesión"
                    >
                      <PowerIcon size={20} className="group-hover:rotate-12 transition-transform" />
                      <span>Cerrar sesión</span>
                    </button>
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