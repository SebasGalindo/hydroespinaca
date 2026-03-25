import React, { ReactNode } from 'react';

interface BaseCardProps {
  children: ReactNode;
  className?: string;
  padding?: 'sm' | 'md' | 'lg';
  hover?: boolean;
  onClick?: (e: React.MouseEvent) => void;
}

const BaseCard = React.memo(function BaseCard({
  children,
  className = '',
  padding = 'md',
  hover = true,
  onClick
}: BaseCardProps) {
  const getPaddingClass = () => {
    switch (padding) {
      case 'sm':
        return 'p-4';
      case 'md':
        return 'p-6';
      case 'lg':
        return 'p-8';
      default:
        return 'p-6';
    }
  };

  const baseClasses = `hidro-card ${getPaddingClass()}`;
  const hoverClasses = hover ? 'cursor-pointer' : '';
  const clickableClasses = onClick ? 'cursor-pointer' : '';

  return (
    <article 
      className={`${baseClasses} ${hoverClasses} ${clickableClasses} ${className}`}
      onClick={onClick}
    >
      {children}
    </article>
  );
});

export default BaseCard;