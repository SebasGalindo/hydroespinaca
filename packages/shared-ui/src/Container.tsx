// Cross-platform Container component types
export interface ContainerProps {
  children: React.ReactNode;
  style?: any;
  scrollable?: boolean;
  padding?: number | 'none' | 'sm' | 'md' | 'lg' | 'xl';
  backgroundColor?: string;
}