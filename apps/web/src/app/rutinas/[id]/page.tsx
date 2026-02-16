'use client';

import { useParams } from 'next/navigation';
import RoutineDetailPage from '@/components/fuzzy/RoutineDetailPage';

export default function RutinaDetailRoute() {
  const params = useParams();
  const id = params?.id as string;

  if (!id) return null;

  return <RoutineDetailPage systemId={id} />;
}
