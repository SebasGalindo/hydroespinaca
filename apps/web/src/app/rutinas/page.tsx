import type { Metadata } from 'next';
import RoutineListPage from '@/components/fuzzy/RoutineListPage';

export const metadata: Metadata = {
  title: 'Rutinas Fuzzy | HydroEspinaca',
  description:
    'Gestiona, duplica y experimenta con múltiples rutinas de control difuso para tu cultivo hidropónico.',
};

export default function RutinasPage() {
  return <RoutineListPage />;
}
