import React, { useState } from 'react';
import type { LoginFormProps } from './LoginForm';
import { Button } from './Button';
import { Input } from './Input';
import { theme } from './theme';

const styles = {
  container: {
    maxWidth: '400px',
    margin: '0 auto',
    padding: theme.spacing[8],
    background: theme.colors.white,
    borderRadius: theme.borderRadius.lg,
    boxShadow: theme.shadows.md,
  },
  title: {
    textAlign: 'center' as const,
    color: theme.colors.secondary[800],
    marginBottom: theme.spacing[6],
    fontSize: theme.typography.fontSize['2xl'],
    fontWeight: theme.typography.fontWeight.semibold,
    fontFamily: theme.typography.fontFamily.primary,
  },
  errorMessage: {
    backgroundColor: theme.colors.error[50],
    border: `1px solid ${theme.colors.error[500]}`,
    color: theme.colors.error[800],
    padding: theme.spacing[3],
    borderRadius: theme.borderRadius.base,
    marginBottom: theme.spacing[4],
    fontSize: theme.typography.fontSize.sm,
  },
  formGroup: {
    marginBottom: theme.spacing[4],
  },
};

export const LoginForm: React.FC<LoginFormProps> = ({
  onSubmit,
  isLoading = false,
  error,
  onClearError,
}) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
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
  };

  return (
    <div style={styles.container}>
      <form onSubmit={handleSubmit}>
        <h2 style={styles.title}>Iniciar Sesión</h2>
        
        {error && (
          <div style={styles.errorMessage} role="alert">
            {error}
          </div>
        )}
        
        <div style={styles.formGroup}>
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="tu@email.com"
            required
            disabled={isLoading}
            autoComplete="email"
            fullWidth
            size="md"
          />
        </div>
        
        <div style={styles.formGroup}>
          <Input
            label="Contraseña"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            required
            disabled={isLoading}
            autoComplete="current-password"
            fullWidth
            size="md"
          />
        </div>
        
        <Button
          type="submit"
          variant="primary"
          size="md"
          disabled={isLoading || !email.trim() || !password.trim()}
          loading={isLoading}
          fullWidth
        >
          {isLoading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
        </Button>
      </form>
    </div>
  );
};