'use client';

import { useEffect } from 'react';
import Link from 'next/link';

export default function ErrorPage({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error('Application error:', error);
  }, [error]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full text-center">
        <div className="mb-8">
          <h1 className="text-6xl font-bold text-red-600 mb-4">500</h1>
          <h2 className="text-2xl font-semibold text-gray-700 mb-2">
            Error del servidor
          </h2>
          <p className="text-gray-600 mb-4">
            Algo salió mal. Por favor, inténtalo de nuevo.
          </p>
          {error.digest && (
            <p className="text-sm text-gray-500 mb-4">
              ID del error: {error.digest}
            </p>
          )}
        </div>
        
        <div className="space-y-4">
          <button
            onClick={reset}
            className="inline-block bg-red-600 text-white px-6 py-3 rounded-md hover:bg-red-700 transition duration-300 mr-4"
          >
            Intentar de nuevo
          </button>
          
          <Link
            href="/"
            className="inline-block bg-gray-600 text-white px-6 py-3 rounded-md hover:bg-gray-700 transition duration-300"
          >
            Volver al inicio
          </Link>
        </div>
      </div>
    </div>
  );
}