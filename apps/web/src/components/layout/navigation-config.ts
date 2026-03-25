import {
  ChartIcon,
  TrendingUpIcon,
  CalculatorIcon,
  BrainIcon,
  CloudSunIcon,
  BellIcon,
  ShieldIcon,
  SettingsIcon,
} from '@/components/ui/icons/Icons';

export interface NavigationItem {
  href: string;
  label: string;
  icon: React.ComponentType<{ size?: number; className?: string; color?: string }>;
  children?: NavigationItem[];
  status?: string;
}

/** Main navigation items (visible to all authenticated users) */
export const NAVIGATION_ITEMS: NavigationItem[] = [
  { href: '/dashboard', label: 'Dashboard', icon: ChartIcon },
  { href: '/analytics', label: 'Análisis de datos', icon: TrendingUpIcon },
  { href: '/consumo', label: 'Consumo y Costos', icon: CalculatorIcon },
  { href: '/rutinas', label: 'Rutinas Fuzzy', icon: BrainIcon },
  { href: '/clima', label: 'Clima y Alertas', icon: CloudSunIcon },
  { href: '/notificaciones', label: 'Notificaciones', icon: BellIcon },
];

/** Admin-only navigation items (submenu) */
export const ADMIN_NAVIGATION: NavigationItem = {
  href: '/admin',
  label: 'Administración',
  icon: ShieldIcon,
  children: [
    { href: '/admin/access', label: 'Gestión de Acceso', icon: SettingsIcon },
    { href: '/admin/dashboard', label: 'Dashboard Admin', icon: ChartIcon },
  ],
};

/** Build the full navigation array based on user role */
export function getNavigationItems(role?: string): NavigationItem[] {
  if (role === 'Administrador') {
    return [...NAVIGATION_ITEMS, ADMIN_NAVIGATION];
  }
  return NAVIGATION_ITEMS;
}
