'use client';

import React from 'react';
import Link from 'next/link';
import LoginCard from '@/components/auth/LoginCard';

export default function LoginPage() {
  return (
    <main className="flex items-center justify-center min-h-screen bg-cover bg-center relative" style={{ backgroundImage: 'url(/images/hydroponics.jpg)' }}>
      {/* Overlay for better readability */}
      <div className="absolute inset-0 bg-black/40" />

      {/* Back to Home Button */}
      <Link
        href="/"
        className="absolute top-4 left-4 z-20 text-white hover:text-green-300 transition-colors flex items-center gap-2 bg-black/30 backdrop-blur-sm px-4 py-2 rounded-lg hover:bg-black/50"
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
        <span className="text-sm font-medium">Volver al inicio</span>
      </Link>

      {/* Login Card - Centered */}
      <div className="relative z-10 w-full max-w-md px-4">
        <LoginCard />
      </div>
    </main>
  );
}