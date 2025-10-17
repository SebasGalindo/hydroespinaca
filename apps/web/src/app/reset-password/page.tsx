'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { LockIcon, CheckCircleIcon, XCircleIcon, CheckIcon, XIcon } from '@/components/ui/icons/Icons';

interface PasswordRequirement {
  label: string;
  test: (password: string) => boolean;
}

const passwordRequirements: PasswordRequirement[] = [
  { label: 'Al menos 8 caracteres', test: (pwd) => pwd.length >= 8 },
  { label: 'Una letra mayúscula', test: (pwd) => /[A-Z]/.test(pwd) },
  { label: 'Un número', test: (pwd) => /[0-9]/.test(pwd) },
  { label: 'Un carácter especial (!@#$%^&*)', test: (pwd) => /[!@#$%^&*(),.?":{}|<>]/.test(pwd) },
];

export default function ResetPasswordPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    newPassword: '',
    confirmPassword: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [message, setMessage] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const passwordStrength = passwordRequirements.filter(req => req.test(formData.newPassword)).length;
  const isPasswordValid = passwordStrength === passwordRequirements.length;
  const passwordsMatch = formData.newPassword === formData.confirmPassword && formData.confirmPassword !== '';

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!isPasswordValid) {
      setStatus('error');
      setMessage('La contraseña no cumple con todos los requisitos de seguridad.');
      return;
    }

    if (!passwordsMatch) {
      setStatus('error');
      setMessage('Las contraseñas no coinciden.');
      return;
    }

    setIsLoading(true);
    setStatus('idle');

    try {
      // Feature pendiente: Integrar con endpoint de reset de contraseña
      await new Promise(resolve => setTimeout(resolve, 1500));

      setStatus('success');
      setMessage('Tu contraseña ha sido restablecida exitosamente.');

      // Redirect to login after 2 seconds
      setTimeout(() => {
        router.push('/login');
      }, 2000);
    } catch (error) {
      setStatus('error');
      setMessage('Hubo un error al restablecer tu contraseña. Por favor, intenta nuevamente.');
    } finally {
      setIsLoading(false);
    }
  };

  const getStrengthColor = () => {
    if (passwordStrength === 0) return 'bg-gray-200';
    if (passwordStrength <= 1) return 'bg-red-500';
    if (passwordStrength <= 2) return 'bg-yellow-500';
    if (passwordStrength <= 3) return 'bg-blue-500';
    return 'bg-green-500';
  };

  const getStrengthLabel = () => {
    if (passwordStrength === 0) return '';
    if (passwordStrength <= 1) return 'Débil';
    if (passwordStrength <= 2) return 'Regular';
    if (passwordStrength <= 3) return 'Buena';
    return 'Fuerte';
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
            <LockIcon size={32} className="text-green-600" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            Restablecer Contraseña
          </h1>
          <p className="text-center text-gray-600 text-sm">
            Ingresa tu nueva contraseña. Asegúrate de que sea segura.
          </p>
        </div>

        {status !== 'success' ? (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* New Password */}
            <div>
              <label htmlFor="newPassword" className="block text-sm font-medium text-gray-800 mb-1">
                Nueva Contraseña
              </label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  id="newPassword"
                  name="newPassword"
                  value={formData.newPassword}
                  onChange={handleChange}
                  placeholder="••••••••"
                  required
                  className="w-full px-4 py-3 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent bg-white text-gray-900"
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                >
                  {showPassword ? (
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                  ) : (
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>

              {/* Password Strength Indicator */}
              {formData.newPassword && (
                <div className="mt-2">
                  <div className="flex gap-1 mb-2">
                    {[1, 2, 3, 4].map((level) => (
                      <div
                        key={level}
                        className={`h-1 flex-1 rounded ${
                          level <= passwordStrength ? getStrengthColor() : 'bg-gray-200'
                        } transition-colors duration-200`}
                      />
                    ))}
                  </div>
                  {passwordStrength > 0 && (
                    <p className="text-xs text-gray-600">
                      Fortaleza: <span className="font-medium">{getStrengthLabel()}</span>
                    </p>
                  )}
                </div>
              )}

              {/* Password Requirements */}
              {formData.newPassword && (
                <ul className="mt-3 space-y-1">
                  {passwordRequirements.map((req, index) => {
                    const isValid = req.test(formData.newPassword);
                    return (
                      <li key={index} className="flex items-center gap-2 text-sm">
                        {isValid ? (
                          <CheckIcon size={16} className="text-green-600 flex-shrink-0" />
                        ) : (
                          <XIcon size={16} className="text-gray-400 flex-shrink-0" />
                        )}
                        <span className={isValid ? 'text-green-700' : 'text-gray-600'}>
                          {req.label}
                        </span>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>

            {/* Confirm Password */}
            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-800 mb-1">
                Confirmar Contraseña
              </label>
              <div className="relative">
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  id="confirmPassword"
                  name="confirmPassword"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  placeholder="••••••••"
                  required
                  className="w-full px-4 py-3 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent bg-white text-gray-900"
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                >
                  {showConfirmPassword ? (
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                  ) : (
                    <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>

              {formData.confirmPassword && (
                <div className="mt-2 flex items-center gap-2">
                  {passwordsMatch ? (
                    <>
                      <CheckIcon size={16} className="text-green-600" />
                      <span className="text-sm text-green-700">Las contraseñas coinciden</span>
                    </>
                  ) : (
                    <>
                      <XIcon size={16} className="text-red-600" />
                      <span className="text-sm text-red-700">Las contraseñas no coinciden</span>
                    </>
                  )}
                </div>
              )}
            </div>

            {status === 'error' && (
              <div className="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md">
                <XCircleIcon size={20} className="text-red-600 flex-shrink-0 mt-0.5" />
                <p className="text-sm text-red-700">{message}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading || !isPasswordValid || !passwordsMatch}
              className="w-full bg-green-600 text-white py-3 rounded-md hover:bg-green-700 transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Restableciendo...' : 'Restablecer Contraseña'}
            </button>
          </form>
        ) : (
          <div className="space-y-4">
            <div className="flex items-start gap-3 p-4 bg-green-50 border border-green-200 rounded-md">
              <CheckCircleIcon size={24} className="text-green-600 flex-shrink-0" />
              <div>
                <p className="text-sm text-green-800 font-medium mb-1">
                  ¡Contraseña restablecida!
                </p>
                <p className="text-sm text-green-700">
                  {message} Serás redirigido al inicio de sesión...
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}
