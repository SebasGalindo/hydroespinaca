import type { Metadata } from 'next';
import { BiPage } from '@/components/bi';

export const metadata: Metadata = {
  title: 'Consumo y Costos | HydroEspinaca',
  description: 'Gestión de costos, consumo manual, producción y análisis de rentabilidad del sistema hidropónico.',
};

export default function ConsumoPage() {
  return <BiPage />;
}
