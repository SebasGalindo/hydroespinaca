'use client';

import React from 'react';

interface SectionProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
  titleClassName?: string;
  subtitleClassName?: string;
  spacing?: 'sm' | 'md' | 'lg';
  id?: string;
}

const Section: React.FC<SectionProps> = ({
  title,
  subtitle,
  children,
  className = '',
  titleClassName = '',
  subtitleClassName = '',
  spacing = 'md',
  id
}) => {
  const spacingClasses = {
    sm: 'mb-4',
    md: 'mb-8',
    lg: 'mb-12'
  };

  return (
    <section 
      className={`${spacingClasses[spacing]} ${className}`} 
      aria-labelledby={id ? `${id}-heading` : undefined}
    >
      {title && (
        <h2 
          id={id ? `${id}-heading` : undefined}
          className={`text-xl font-bold text-green-800 mb-4 font-inter ${titleClassName}`}
        >
          {title}
        </h2>
      )}
      {subtitle && (
        <p className={`text-gray-600 font-inter mb-4 ${subtitleClassName}`}>
          {subtitle}
        </p>
      )}
      {children}
    </section>
  );
};

export default Section;