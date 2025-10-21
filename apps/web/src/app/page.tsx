'use client';

import React from 'react';
import Link from 'next/link';
import MainLayout from '@/components/layout/MainLayout';
import { useAuthStore } from '@hydroespinaca/shared';

export default function HomePage() {
  const { isAuthenticated } = useAuthStore();

  return (
    <MainLayout>
      {/* Hero Section */}
      <section className="bg-green-50 py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-12 items-center">
            <div>
              <h1 className="text-4xl md:text-5xl font-bold text-gray-900 mb-6">
                Soluciones inteligentes para cultivos hidropónicos
              </h1>
              <p className="text-lg text-gray-600 mb-8">
                Gestiona tus cultivos con nuestra plataforma de monitoreo y control para sistemas hidropónicos.
              </p>
            </div>
            <div className="relative h-64 md:h-96 rounded-lg overflow-hidden shadow-xl">
              <div
                className="absolute inset-0 bg-cover bg-center"
                style={{ backgroundImage: 'url(/images/hidroespinaca-bg.jpg)' }}
              ></div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-16 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-bold text-gray-900 mb-4">Características principales</h2>
            <p className="text-lg text-gray-600 max-w-3xl mx-auto">
              Nuestra plataforma ofrece herramientas avanzadas para el monitoreo y control de tus cultivos hidropónicos.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Feature 1 */}
            <div className="bg-green-50 p-6 rounded-lg">
              <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mb-4">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Monitoreo</h3>
              <p className="text-gray-600">
                Supervisa los parámetros críticos de tus cultivos y los actuadores activos.
              </p>
            </div>

            {/* Feature 2 */}
            <div className="bg-green-50 p-6 rounded-lg">
              <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mb-4">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Análisis de datos</h3>
              <p className="text-gray-600">
                Obtén insights valiosos con análisis detallados y reportes sobre el estado de tus cultivos.
              </p>
            </div>
          </div>
        </div>
      </section>
    </MainLayout>
  );
}
