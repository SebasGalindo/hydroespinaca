import React, { ReactNode } from 'react';

interface FormSectionProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
  className?: string;
}

const FormSection: React.FC<FormSectionProps> = ({
  title,
  subtitle,
  children,
  className = ''
}) => {
  return (
    <section className={`border border-gray-200 rounded-lg p-6 bg-white ${className}`}>
      <header className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 font-inter">
          {title}
        </h3>
        {subtitle && (
          <p className="text-sm text-gray-600 mt-1 font-inter">
            {subtitle}
          </p>
        )}
      </header>
      <div>
        {children}
      </div>
    </section>
  );
};

export default FormSection;