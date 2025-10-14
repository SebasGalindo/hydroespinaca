'use client';

import React from 'react';
import LoginCard from '@/components/auth/LoginCard';

export default function LoginPage() {
  return (
    <main className="lg:flex min-h-screen">
      {/* Left side (form) */}
      <div className="lg:w-1/2 w-full flex items-center justify-center p-8 relative lg:bg-white">
        {/* Mobile background */}
        <div className="lg:hidden absolute inset-0 bg-cover bg-center bg-hidroespinaca">
          <div className="absolute inset-0 bg-black opacity-50" />
        </div>
        <div className="relative z-10">
          <LoginCard />
        </div>
      </div>

      {/* Right side (image) */}
      <div className="hidden lg:block lg:w-1/2 bg-cover bg-center bg-hidroespinaca" />
    </main>
  );
}