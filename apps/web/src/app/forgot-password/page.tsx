'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { EmailIcon, CheckCircleIcon, XCircleIcon } from '@/components/ui/icons/Icons';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setStatus('idle');

    try {
      // TODO: Replace with actual API call to Supabase or custom endpoint
      // const response = await fetch('/api/auth/forgot-password', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({ email })
      // });

      // Simulating API call
      await new Promise(resolve => setTimeout(resolve, 1500));

      setStatus('success');
      setMessage('Se ha enviado un correo con instrucciones para recuperar tu contraseña.');
    } catch (error) {
      setStatus('error');
      setMessage('Hubo un error al procesar tu solicitud. Por favor, intenta nuevamente.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="flex items-center justify-center min-h-screen bg-gradient-to-br from-green-50 to-green-100 p-4">
      {/* Back to Login Link */}
      <Link
        href="/login"
        className="absolute top-4 left-4 text-gray-500 hover:text-green-600 transition-colors flex items-center gap-2"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-5 w-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M10 19l-7-7m0 0l7-7m-7 7h18"
          />
        </svg>
        <span className="text-sm font-medium">Volver al inicio de sesión</span>
      </Link>

      <div className="bg-white rounded-2xl shadow-lg p-8 w-full max-w-md">
        <div className="flex flex-col items-center mb-6">
          <div className="h-16 w-16 rounded-full bg-green-100 flex items-center justify-center mb-4">
            <EmailIcon size={32} className="text-green-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            Recuperar Contraseña
          </h1>
          <p className="text-center text-gray-600 text-sm">
            Ingresa tu correo electrónico y te enviaremos instrucciones para restablecer tu contraseña.
          </p>
        </div>

        {status !== 'success' ? (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-800 mb-1">
                Correo electrónico
              </label>
              <input
                type="email"
                id="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="tu@correo.com"
                required
                className="w-full px-4 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent bg-white text-gray-900"
                disabled={isLoading}
              />
            </div>

            {status === 'error' && (
              <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md">
                <XCircleIcon size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
                <p className="text-sm text-red-700">{message}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-green-600 text-white py-3 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Enviando...' : 'Enviar instrucciones'}
            </button>
          </form>
        ) : (
          <div className="space-y-4">
            <div className="flex items-start gap-3 p-4 bg-green-50 border border-green-200 rounded-md">
              <CheckCircleIcon size={24} className="text-green-600 flex-shrink-0" />
              <div>
                <p className="text-sm text-green-800 font-medium mb-1">
                  ¡Correo enviado con éxito!
                </p>
                <p className="text-sm text-green-700">
                  {message}
                </p>
              </div>
            </div>
            <Link
              href="/login"
              className="block w-full text-center bg-gray-100 text-gray-700 py-3 rounded-md hover:bg-gray-200 transition-colors font-medium"
            >
              Volver al inicio de sesión
            </Link>
          </div>
        )}

        <div className="mt-6 text-center">
          <p className="text-sm text-gray-600">
            ¿Recordaste tu contraseña?{' '}
            <Link href="/login" className="text-green-600 hover:text-green-800 font-medium">
              Iniciar sesión
            </Link>
          </p>
        </div>
      </div>
    </main>
  );
}
