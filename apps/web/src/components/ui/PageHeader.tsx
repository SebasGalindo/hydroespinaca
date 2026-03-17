'use client';

import React from 'react';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  children?: React.ReactNode;
  alignment?: 'left' | 'center' | 'right';
  className?: string;
  titleClassName?: string;
  subtitleClassName?: string;
}

const PageHeader = React.memo(function PageHeader({
  title,
  subtitle,
  children,
  alignment = 'center',
  className = '',
  titleClassName = '',
  subtitleClassName = ''
}: PageHeaderProps) {
  const alignmentClasses = {
    left: 'text-left',
    center: 'text-center',
    right: 'text-right'
  };

  return (
    <header className={`mb-8 pt-6 ${alignmentClasses[alignment]} ${className}`}>
      <h1 className={`text-2xl lg:text-3xl font-bold text-green-800 mb-2 font-inter ${titleClassName}`}>
        {title}
      </h1>
      {subtitle && (
        <p className={`text-gray-600 font-inter ${subtitleClassName}`}>
          {subtitle}
        </p>
      )}
      {children && (
        <div className="mt-4">
          {children}
        </div>
      )}
    </header>
  );
});

export default PageHeader;