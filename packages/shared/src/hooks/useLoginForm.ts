import { useState, FormEvent } from 'react';
import { useAuthStore } from '../store/authStore';

interface LoginFormState {
  email: string;
  password: string;
}

interface UseLoginFormReturn {
  formState: LoginFormState;
  isLoading: boolean;
  error: string | null;
  handleChange: (e: React.ChangeEvent<HTMLInputElement> | { target: { name: string; value: string } }) => void;
  handleSubmit: (e: FormEvent | { preventDefault: () => void }) => Promise<void>;
  clearError: () => void;
}

export const useLoginForm = (): UseLoginFormReturn => {
  const [formState, setFormState] = useState<LoginFormState>({
    email: '',
    password: '',
  });
  
  const { login, isLoading, error, clearError } = useAuthStore();
  
  const handleChange = (e: React.ChangeEvent<HTMLInputElement> | { target: { name: string; value: string } }) => {
    const { name, value } = e.target as { name: string; value: string };
    setFormState(prev => ({ ...prev, [name]: value }));
  };
  
  const handleSubmit = async (e: FormEvent | { preventDefault: () => void }) => {
    e.preventDefault();
    await login(formState.email, formState.password);
  };
  
  return {
    formState,
    isLoading,
    error,
    handleChange,
    handleSubmit,
    clearError,
  };
};