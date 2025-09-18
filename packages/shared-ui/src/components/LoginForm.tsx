import React, { useState } from 'react';

export interface LoginFormProps {
  onSubmit: (email: string, password: string) => Promise<void>;
  isLoading?: boolean;
  error?: string | null;
  onClearError?: () => void;
  style?: 'web' | 'mobile';
}

export const LoginForm: React.FC<LoginFormProps> = ({
  onSubmit,
  isLoading = false,
  error,
  onClearError,
  style = 'web'
}) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault();
    }
    
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

  if (style === 'mobile') {
    // For React Native, we need to use a different approach
    // This will be used in React Native with TextInput components
    // The actual React Native components will be rendered by the mobile app
    
    // Return a structured object that mobile can use
    return React.createElement('div', {
      'data-mobile-form': true,
      'data-email': email,
      'data-password': password,
      'data-loading': isLoading,
      'data-error': error,
      'data-handlers': {
        setEmail,
        setPassword,
        handleSubmit: () => handleSubmit(),
        clearError: onClearError
      }
    }, 'Mobile form placeholder');
  }

  const containerClass = 'login-form-web';

  return (
    <div className={containerClass}>
      <form onSubmit={handleSubmit}>
        <h2>Iniciar Sesión</h2>
        
        {error && (
          <div className="error-message">
            {error}
          </div>
        )}
        
        <div className="form-group">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="tu@email.com"
            required
            disabled={isLoading}
          />
        </div>
        
        <div className="form-group">
          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            required
            disabled={isLoading}
          />
        </div>
        
        <button 
          type="submit" 
          disabled={isLoading || !email.trim() || !password.trim()}
          className="submit-button"
        >
          {isLoading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
        </button>
      </form>
    </div>
  );
};