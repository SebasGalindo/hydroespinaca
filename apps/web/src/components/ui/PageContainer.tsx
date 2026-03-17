'use client';

import React from 'react';

interface PageContainerProps {
  children: React.ReactNode;
  className?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '4xl' | '6xl' | '7xl' | 'full';
  padding?: 'none' | 'sm' | 'md' | 'lg';
  background?: 'white' | 'gray' | 'green' | 'gradient';
}

const PageContainer = React.memo(function PageContainer({
  children,
  className = '',
  maxWidth = '7xl',
  padding = 'md',
  background = 'gradient'
}: PageContainerProps) {
  const maxWidthClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg',
    xl: 'max-w-xl',
    '2xl': 'max-w-2xl',
    '4xl': 'max-w-4xl',
    '6xl': 'max-w-6xl',
    '7xl': 'max-w-7xl',
    full: 'max-w-full'
  };

  const paddingClasses = {
    none: '',
    sm: 'px-2 pb-16 lg:pb-6',
    md: 'px-4 pb-20 lg:pb-8',
    lg: 'px-6 pb-24 lg:pb-10'
  };

  const backgroundClasses = {
    white: 'bg-white',
    gray: 'bg-gray-50',
    green: 'bg-green-50',
    gradient: 'bg-gradient-to-br from-green-50 to-green-100'
  };

  return (
    <div className={`min-h-screen ${backgroundClasses[background]}`}>
      <main className={`${paddingClasses[padding]} ${className}`} role="main">
        <div className={`${maxWidthClasses[maxWidth]} mx-auto`}>
          {children}
        </div>
      </main>
    </div>
  );
});

export default PageContainer;