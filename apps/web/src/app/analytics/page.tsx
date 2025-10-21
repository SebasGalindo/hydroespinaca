import { Metadata } from 'next';
import AnalyticsPage from '@/components/analytics/AnalyticsPage';

export const metadata: Metadata = {
  title: 'Análisis de Datos | HydroEspinaca',
  description: 'Monitoreo ambiental, control y correlaciones - Sistema de cultivo hidropónico',
};

export default function Analytics() {
  return <AnalyticsPage />;
}
