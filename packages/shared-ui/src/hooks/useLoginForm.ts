import { useState, useCallback } from 'react';

export interface UseLoginFormReturn {
  email: string;
  password: string;
  setEmail: (email: string) => void;
  setPassword: (password: string) => void;
  handleSubmit: () => Promise<void>;
  canSubmit: boolean;
}

export interface UseLoginFormProps {
  onSubmit: (email: string, password: string) => Promise<void>;
  onClearError?: () => void;
}

export const useLoginForm = ({ onSubmit, onClearError }: UseLoginFormProps): UseLoginFormReturn => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = useCallback(async () => {
    if (onClearError) {
      onClearError();
    }
    
    if (!email.trim() || !password.trim()) {
      return;
    }

    try {
      await onSubmit(email.trim(), password);
    } catch (error) {
      // Error handling is done in the useAuth hook
    }
  }, [email, password, onSubmit, onClearError]);

  const canSubmit = !!(email.trim() && password.trim());

  return {
    email,
    password,
    setEmail,
    setPassword,
    handleSubmit,
    canSubmit,
  };
};