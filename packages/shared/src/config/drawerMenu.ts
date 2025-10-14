// Configuración del menú lateral (DrawerMenu) compartida entre web y mobile
import { IconName } from '../icons/types';

export interface DrawerMenuItem {
  id: string;
  label: string;
  iconName: IconName;
  href?: string;
  children?: DrawerMenuItem[];
  status?: 'connected' | 'disconnected' | null;
  isExpandable?: boolean;
}

export const drawerMenuConfig: DrawerMenuItem[] = [
  {
    id: 'info-main',
    label: 'Info-Main',
    iconName: 'chart',
    href: '/dashboard'
  },
  {
    id: 'lecturas',
    label: 'Lecturas',
    iconName: 'book',
    href: '/dashboard/lecturas'
  },
  {
    id: 'dashboard',
    label: 'Dashboard',
    iconName: 'trending-up',
    href: '/dashboard-monitoreo'
  },
  {
    id: 'logica-fuzzy',
    label: 'Lógica Fuzzy',
    iconName: 'brain',
    href: '/dashboard/logica-fuzzy'
  },
  {
    id: 'crear-variable',
    label: 'Crear Variable Manual',
    iconName: 'plus',
    href: '/dashboard/nueva-variable'
  },
  {
    id: 'listas',
    label: 'Listas',
    iconName: 'menu',
    isExpandable: true,
    children: [
      {
        id: 'variables',
        label: 'Variables',
        iconName: 'settings',
        href: '/dashboard/variables-config'
      },
      {
        id: 'sensores',
        label: 'Sensores',
        iconName: 'settings',
        href: '/dashboard/sensores-config'
      },
      {
        id: 'actuadores',
        label: 'Actuadores',
        iconName: 'settings',
        href: '/dashboard/actuadores-config'
      },
      {
        id: 'reglas-fuzzy',
        label: 'Reglas Fuzzy',
        iconName: 'brain',
        href: '/dashboard/reglas-fuzzy'
      }
    ]
  },
  {
    id: 'estado-sistema',
    label: 'Estado del Sistema',
    iconName: 'chart',
    isExpandable: true,
    children: [
      {
        id: 'api-principal',
        label: 'API Principal',
        iconName: 'wifi',
        href: '/dashboard/sistema/api',
        status: 'connected'
      },
      {
        id: 'base-datos',
        label: 'Base de Datos',
        iconName: 'wifi',
        href: '/dashboard/sistema/database',
        status: 'connected'
      },
      {
        id: 'sensores-iot',
        label: 'Sensores IoT',
        iconName: 'wifi',
        href: '/dashboard/sistema/sensors',
        status: 'disconnected'
      }
    ]
  },
  {
    id: 'ajustes',
    label: 'Ajustes',
    iconName: 'settings',
    href: '/dashboard/ajustes'
  },
  {
    id: 'historial',
    label: 'Historial',
    iconName: 'clock',
    href: '/dashboard/historial'
  }
];

// Función para obtener un elemento del drawer por ID
export const getDrawerMenuItem = (id: string): DrawerMenuItem | undefined => {
  const findItem = (items: DrawerMenuItem[]): DrawerMenuItem | undefined => {
    for (const item of items) {
      if (item.id === id) return item;
      if (item.children) {
        const found = findItem(item.children);
        if (found) return found;
      }
    }
    return undefined;
  };
  
  return findItem(drawerMenuConfig);
};

// Función para obtener todos los elementos expandibles
export const getExpandableItems = (): DrawerMenuItem[] => {
  return drawerMenuConfig.filter(item => item.isExpandable);
};

// Función para obtener elementos con estado de conexión
export const getItemsWithStatus = (): DrawerMenuItem[] => {
  const itemsWithStatus: DrawerMenuItem[] = [];
  
  const findItemsWithStatus = (items: DrawerMenuItem[]) => {
    items.forEach(item => {
      if (item.status) {
        itemsWithStatus.push(item);
      }
      if (item.children) {
        findItemsWithStatus(item.children);
      }
    });
  };
  
  findItemsWithStatus(drawerMenuConfig);
  return itemsWithStatus;
};