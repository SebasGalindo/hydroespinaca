import React from 'react';
import Link from 'next/link';
import { useLoginForm } from '@hidroespinaca/shared';

interface LoginCardProps {
  className?: string;
}

const LoginCard: React.FC<LoginCardProps> = ({ className }) => {
  const { formState, isLoading, error, handleChange, handleSubmit, clearError } = useLoginForm();

  return (
    <div className={`lg:bg-transparent lg:shadow-none bg-white rounded-lg shadow-md p-8 w-full max-w-md ${className}`}>
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
        <h1 className="text-2xl font-bold text-gray-800">Hidro Espinaca</h1>
      </div>
      
      <p className="text-center text-gray-600 mb-6">
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
      
      <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="mb-4">
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">Correo electrónico</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formState.email}
              onChange={handleChange}
              placeholder="ingeniero@ejemplo.com"
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
            />
          </div>
          
          <div className="mb-4">
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">Contraseña</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formState.password}
              onChange={handleChange}
              placeholder="********"
              required
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
            />
          </div>
          
          <div className="text-right mb-4">
            <Link href="/forgot-password" className="text-sm text-green-600 hover:text-green-800">
              Olvidaste tu contraseña?
            </Link>
          </div>
          
          <button
            type="submit"
            className="w-full bg-green-600 text-white py-3 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 font-medium"
            disabled={isLoading}
          >
            {isLoading ? 'Iniciando sesión...' : 'Sign in'}
          </button>
        </form>
      
      <div className="mt-6 text-center">
        <p className="text-sm text-gray-600">
          No tienes una cuenta?{' '}
          <Link href="/signup" className="text-green-600 hover:text-green-800">
            Regístrate ahora
          </Link>
        </p>
      </div>
    </div>
  );
};

export default LoginCard;