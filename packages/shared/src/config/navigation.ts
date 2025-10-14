// Configuración de navegación compartida entre web y mobile
import { IconName } from '../icons/types';

export interface NavigationItem {
  id: string;
  href: string;
  label: string;
  iconName: IconName;
  order: number;
  isMainTab?: boolean; // Para identificar las pestañas principales del bottom navigation
}

export const navigationConfig: NavigationItem[] = [
  {
    id: 'info-main',
    href: '/dashboard',
    label: 'Info-Main',
    iconName: 'chart',
    order: 1,
    isMainTab: true
  },
  {
    id: 'lecturas',
    href: '/dashboard/lecturas',
    label: 'Lecturas',
    iconName: 'book',
    order: 2,
    isMainTab: true
  },
  {
    id: 'dashboard',
    href: '/dashboard-monitoreo',
    label: 'Dashboard',
    iconName: 'trending-up',
    order: 3,
    isMainTab: true
  },
  {
    id: 'logica-fuzzy',
    href: '/dashboard/logica-fuzzy',
    label: 'Lógica Fuzzy',
    iconName: 'brain',
    order: 4,
    isMainTab: true
  },
  {
    id: 'crear-variable',
    href: '/dashboard/nueva-variable',
    label: 'Crear Variable',
    iconName: 'plus',
    order: 5,
    isMainTab: true
  },
  {
    id: 'mas',
    href: '#',
    label: 'Más',
    iconName: 'menu',
    order: 6,
    isMainTab: true
  }
];

// Función para obtener solo las pestañas principales
export const getMainTabs = (): NavigationItem[] => {
  return navigationConfig.filter(item => item.isMainTab).sort((a, b) => a.order - b.order);
};

// Función para obtener un elemento por ID
export const getNavigationItem = (id: string): NavigationItem | undefined => {
  return navigationConfig.find(item => item.id === id);
};