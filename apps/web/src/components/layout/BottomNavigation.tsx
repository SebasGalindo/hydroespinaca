'use client';

import React from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  ChartIcon,
  TrendingUpIcon,
  BrainIcon,
  MenuIcon
} from '@/components/ui/icons/Icons';

interface BottomNavigationProps {
  onMoreClick: () => void;
  isMoreMenuOpen?: boolean;
}

const BottomNavigation: React.FC<BottomNavigationProps> = ({
  onMoreClick,
  isMoreMenuOpen = false
}) => {
  const pathname = usePathname();

  // Sincronizado con navigationItems de SideNavigation (Web)
  const navItems = [
    {
      href: '/dashboard',
      icon: ChartIcon,
      label: 'Dashboard',
      active: pathname === '/dashboard'
    },
    {
      href: '/analytics',
      icon: TrendingUpIcon,
      label: 'Análisis',
      active: pathname === '/analytics'
    },
    {
      href: '/dashboard/sistemas-fuzzy',
      icon: BrainIcon,
      label: 'Fuzzy',
      active: pathname?.startsWith('/dashboard/sistemas-fuzzy')
    }
  ];

  return (
    <nav
      className="lg:hidden fixed bottom-0 left-0 right-0 bg-white/95 backdrop-blur-md border-t border-gray-200 shadow-lg z-50"
      role="navigation"
      aria-label="Navegación móvil"
    >
      {/* Safe area padding for devices with bottom notch */}
      <div className="pb-safe">
        <div className="flex items-center justify-around px-2 py-2 max-w-md mx-auto">
          {navItems.map((item) => {
            const IconComponent = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`relative flex flex-col items-center gap-1 px-4 py-2 rounded-xl transition-all duration-300 flex-1 group ${
                  item.active
                    ? 'text-green-600'
                    : 'text-gray-500 hover:text-green-600 active:scale-95'
                }`}
              >
                {/* Active indicator bar */}
                {item.active && (
                  <div className="absolute -top-[9px] left-1/2 -translate-x-1/2 w-8 h-1 bg-gradient-to-r from-green-400 to-green-600 rounded-full shadow-lg" />
                )}

                {/* Icon with background on active */}
                <div className={`relative transition-all duration-300 ${
                  item.active ? 'scale-110' : 'group-hover:scale-105'
                }`}>
                  {item.active && (
                    <div className="absolute inset-0 bg-green-100 rounded-lg -z-10 scale-150" />
                  )}
                  <IconComponent size={22} className="flex-shrink-0" />
                </div>

                <span className={`text-[10px] font-inter font-medium leading-tight text-center transition-all duration-300 ${
                  item.active ? 'font-semibold' : ''
                }`}>
                  {item.label}
                </span>
              </Link>
            );
          })}

          {/* Botón "Más" con toggle y feedback visual */}
          <button
            onClick={onMoreClick}
            className={`relative flex flex-col items-center gap-1 px-4 py-2 rounded-xl transition-all duration-300 flex-1 active:scale-95 group ${
              isMoreMenuOpen
                ? 'text-green-600'
                : 'text-gray-500 hover:text-green-600'
            }`}
            aria-label="Menú de navegación"
            aria-expanded={isMoreMenuOpen}
            aria-controls="mobile-drawer-menu"
          >
            {/* Active indicator bar cuando el menú está abierto */}
            {isMoreMenuOpen && (
              <div className="absolute -top-[9px] left-1/2 -translate-x-1/2 w-8 h-1 bg-gradient-to-r from-green-400 to-green-600 rounded-full shadow-lg" />
            )}

            <div className={`relative transition-all duration-300 ${
              isMoreMenuOpen ? 'scale-110' : 'group-hover:scale-105'
            }`}>
              {isMoreMenuOpen && (
                <div className="absolute inset-0 bg-green-100 rounded-lg -z-10 scale-150" />
              )}
              <MenuIcon size={22} className="flex-shrink-0" />
            </div>

            <span className={`text-[10px] font-inter font-medium leading-tight text-center transition-all duration-300 ${
              isMoreMenuOpen ? 'font-semibold' : ''
            }`}>
              Más
            </span>
          </button>
        </div>
      </div>
    </nav>
  );
};

export default BottomNavigation;