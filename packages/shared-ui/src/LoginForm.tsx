// Cross-platform LoginForm types and interfaces
export interface LoginFormProps {
  onSubmit: (email: string, password: string) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
  onClearError?: () => void;
}