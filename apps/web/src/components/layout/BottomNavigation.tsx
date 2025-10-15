'use client';

import React from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  ChartIcon,
  BookIcon,
  TrendingUpIcon,
  BrainIcon,
  MenuIcon
} from '@/components/ui/icons/Icons';

interface BottomNavigationProps {
  onMoreClick: () => void;
}

const BottomNavigation: React.FC<BottomNavigationProps> = ({ onMoreClick }) => {
  const pathname = usePathname();
  
  const navItems = [
    {
      href: '/dashboard',
      icon: ChartIcon,
      label: 'Info-Main',
      active: pathname === '/dashboard'
    },
    {
      href: '/lecturas',
      icon: BookIcon,
      label: 'Lecturas',
      active: pathname === '/lecturas'
    },
    {
      href: '/dashboard-monitoreo',
      icon: TrendingUpIcon,
      label: 'Dashboard',
      active: pathname === '/dashboard-monitoreo'
    },
    {
      href: '/dashboard/sistemas-fuzzy',
      icon: BrainIcon,
      label: 'Sistemas Fuzzy',
      active: pathname === '/dashboard/sistemas-fuzzy'
    }
  ];

  return (
    <nav 
      className="lg:hidden fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 px-2 py-2 z-50" 
      role="navigation" 
      aria-label="Navegación móvil"
    >
      <div className="flex justify-around items-center w-full">
        {navItems.map((item) => {
          const IconComponent = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={`flex flex-col items-center space-y-1 px-1 py-1 rounded-md transition-colors duration-200 flex-1 max-w-[14.28%] ${
                item.active
                  ? 'text-green-600'
                  : 'text-gray-500 hover:text-green-600'
              }`}
            >
              <IconComponent size={18} className="flex-shrink-0" />
              <span className="text-[10px] sm:text-xs font-inter font-medium leading-tight text-center break-words hyphens-auto">
                {item.label}
              </span>
            </Link>
          );
        })}
        <button
          onClick={onMoreClick}
          className={`flex flex-col items-center space-y-1 px-1 py-1 rounded-md transition-colors duration-200 flex-1 max-w-[14.28%] text-gray-500 hover:text-green-600`}
        >
          <MenuIcon size={18} className="flex-shrink-0" />
          <span className="text-[10px] sm:text-xs font-inter font-medium leading-tight text-center break-words hyphens-auto">
            Más
          </span>
        </button>
      </div>
    </nav>
  );
};

export default BottomNavigation;