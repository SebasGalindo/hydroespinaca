// Cross-platform Typography component types
export interface TypographyProps {
  children: React.ReactNode;
  variant?: 'body' | 'caption' | 'subtitle' | 'overline';
  style?: any;
  color?: string;
}