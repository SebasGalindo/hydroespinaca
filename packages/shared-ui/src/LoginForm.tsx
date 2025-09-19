// Cross-platform LoginForm types and interfaces
export interface LoginFormProps {
  onSubmit: (email: string, password: string) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
  onClearError?: () => void;
}

// Platform-specific imports - Metro will resolve the correct one
export { LoginForm } from './LoginForm.native';