'use client';

import React from 'react';

interface ToggleProps {
  enabled: boolean;
  onChange: () => void;
  disabled?: boolean;
}

/**
 * Generic toggle switch — reusable UI primitive.
 */
const Toggle = React.memo(function Toggle({ enabled, onChange, disabled }: ToggleProps) {
  return (
    <button
      onClick={onChange}
      disabled={disabled}
      className={`relative w-11 h-6 rounded-full transition-colors ${
        enabled ? 'bg-green-500' : 'bg-gray-300'
      } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
    >
      <span
        className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
          enabled ? 'translate-x-5' : 'translate-x-0'
        }`}
      />
    </button>
  );
});

export default Toggle;
