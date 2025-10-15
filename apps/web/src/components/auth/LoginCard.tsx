import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useLoginForm, useAuthStore } from '@hydroespinaca/shared';
import { EyeIcon, EyeOffIcon } from '@/components/ui/icons/Icons';

interface LoginCardProps {
  className?: string;
}

const LoginCard: React.FC<LoginCardProps> = ({ className }) => {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();
  const { formState, isLoading, error, handleChange, handleSubmit, clearError } = useLoginForm();
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    if (isAuthenticated) {
      // Small delay to allow browser to detect successful login and offer to save credentials
      const timer = setTimeout(() => {
        router.push('/dashboard');
      }, 500);

      return () => clearTimeout(timer);
    }
  }, [isAuthenticated, router]);

  return (
    <div className={`bg-white/95 backdrop-blur-sm shadow-xl rounded-2xl p-8 w-full ${className}`}>
      <div className="flex flex-col items-center mb-6">
        <div className="h-16 w-16 rounded-full bg-green-100 flex items-center justify-center mb-2">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-8 w-8 text-green-800"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
            />
          </svg>
        </div>
        <h1 className="text-2xl font-bold text-gray-900">HydroEspinaca</h1>
      </div>

      <p className="text-center text-gray-700 mb-6">
        Bienvenido de nuevo! Ingresa tus credenciales.
      </p>
      
      {error && (
        <div className="mb-4 p-3 bg-red-100 text-red-700 rounded-md text-sm flex justify-between items-center">
          <span>{error}</span>
          <button
            onClick={clearError}
            className="text-red-700 font-bold"
            aria-label="Cerrar mensaje de error"
          >
            ×
          </button>
        </div>
      )}

      {isAuthenticated && (
        <div className="mb-4 p-3 bg-green-100 text-green-700 rounded-md text-sm flex items-center gap-2">
          <svg className="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span>¡Login exitoso! Redirigiendo...</span>
        </div>
      )}
      
      <form
        className="space-y-4"
        onSubmit={handleSubmit}
        name="login"
        method="post"
        action="/login"
      >
          <div className="mb-4">
            <label htmlFor="email" className="block text-sm font-medium text-gray-800 mb-1">Correo electrónico</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formState.email}
              onChange={handleChange}
              placeholder="correo@ejemplo.com"
              required
              autoComplete="username"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 bg-white text-gray-900"
            />
          </div>

          <div className="mb-4">
            <label htmlFor="password" className="block text-sm font-medium text-gray-800 mb-1">Contraseña</label>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                id="password"
                name="password"
                value={formState.password}
                onChange={handleChange}
                placeholder="********"
                required
                autoComplete="current-password"
                className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 bg-white text-gray-900"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700 focus:outline-none"
                aria-label={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
              >
                {showPassword ? (
                  <EyeOffIcon size={20} />
                ) : (
                  <EyeIcon size={20} />
                )}
              </button>
            </div>
          </div>

          <div className="text-right mb-4">
            <Link href="/forgot-password" className="text-sm text-green-700 hover:text-green-900 font-medium">
              ¿Olvidaste tu contraseña?
            </Link>
          </div>
          
          <button
            type="submit"
            className="w-full bg-green-600 text-white py-3 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 font-medium"
            disabled={isLoading}
          >
            {isLoading ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>
    </div>
  );
};

export default LoginCard;