// Tipos base para el sistema CRUD genérico
import type { IconName } from '../icons/types';

export type EntityStatus = 'active' | 'inactive' | 'deprecated';
export type FieldType = 'text' | 'badge' | 'number' | 'date';
export type ActionType = 'edit' | 'delete' | 'view' | 'custom';
export type ActionVariant = 'primary' | 'secondary' | 'danger';

// Campo de configuración (para definir qué mostrar)
export interface FieldConfig {
  key: string;
  label: string;
  type?: FieldType;
  variant?: 'default' | 'info' | 'success' | 'warning' | 'danger';
}

// Campo de información genérico (con datos)
export interface InfoField {
  key?: string;
  label: string;
  value: string;
  type?: FieldType;
  variant?: 'default' | 'info' | 'success' | 'warning' | 'danger';
}

// Acción disponible en el card
export interface EntityAction {
  id: string;
  type?: string;
  label: string;
  variant?: 'primary' | 'secondary' | 'danger';
  icon?: IconName;
  disabled?: boolean;
  onClick: () => void;
}

// Entidad base genérica
export interface BaseEntity {
  id: string;
  title: string;
  subtitle?: string;
  identifier?: string;
  description?: string;
  status: EntityStatus;
  modifiedDate: string;
  fields: InfoField[];
}

// Configuración específica por tipo de entidad
export interface EntityConfig {
  entityType: 'variable' | 'sensor' | 'actuator' | 'custom';
  titleField?: string;
  subtitleField?: string;
  technicalIdField?: string;
  statusField?: string;
  lastModifiedField?: string;
  infoFields?: FieldConfig[];
  showIdentifier?: boolean;
  showDescription?: boolean;
  showModifiedDate?: boolean;
  customFields?: string[];
  defaultActions?: EntityAction[];
}

// Props para paginación
export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  itemsPerPage: number;
  loading?: boolean;
  onPageChange: (page: number) => void;
  onItemsPerPageChange?: (itemsPerPage: number) => void;
}

// Props para el organismo CrudList
export interface CrudListProps<T extends BaseEntity> {
  title?: string;
  subtitle?: string;
  data: T[];
  config: EntityConfig;
  pagination?: PaginationProps;
  loading?: boolean;
  error?: string;
  emptyMessage?: string;
  onRefresh?: () => void;
  refreshing?: boolean;
  getActions?: (item: T) => EntityAction[];
  onItemPress?: (item: T) => void;
}

// Tipos específicos para diferentes entidades (ejemplos)
export interface Variable extends BaseEntity {
  unit: string;
  type: 'input' | 'output' | 'calculated';
  dataType: 'numeric' | 'boolean' | 'text';
  minValue?: number;
  maxValue?: number;
  isRequired: boolean;
  category: 'environmental' | 'control' | 'system' | 'user';
}

export interface Sensor extends BaseEntity {
  type: 'temperature' | 'humidity' | 'ph' | 'light' | 'other';
  location: string;
  unit: string;
  calibrationDate?: string;
  accuracy?: string;
}

export interface Actuator extends BaseEntity {
  type: 'pump' | 'valve' | 'fan' | 'heater' | 'other';
  location: string;
  powerRating?: string;
  controlType: 'manual' | 'automatic';
}

// Tipo para propiedades de texto deshabilitado
export interface DisabledTextProps {
  textDisabled?: string;
}