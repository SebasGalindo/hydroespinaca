'use client';

import React from 'react';
import { XIcon } from './icons/Icons';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl';
}

const Modal: React.FC<ModalProps> = ({ 
  isOpen, 
  onClose, 
  title, 
  children, 
  maxWidth = 'lg' 
}) => {
  if (!isOpen) return null;

  const getMaxWidthClass = () => {
    switch (maxWidth) {
      case 'sm': return 'max-w-sm';
      case 'md': return 'max-w-md';
      case 'lg': return 'max-w-lg';
      case 'xl': return 'max-w-xl';
      case '2xl': return 'max-w-2xl';
      default: return 'max-w-lg';
    }
  };

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div 
        className="fixed inset-0 z-50 flex items-center justify-center p-4 modal-overlay"
        onClick={handleBackdropClick}
      >
      <div 
        className={`bg-white rounded-lg shadow-xl w-full ${getMaxWidthClass()} max-h-[90vh] flex flex-col`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">{title}</h2>
          <button
            onClick={onClose}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <XIcon className="w-5 h-5" />
          </button>
        </div>

        {/* Content with custom scrollbar */}
        <div 
          className="flex-1 overflow-y-auto p-6"
          style={{
            scrollbarWidth: 'thin',
            scrollbarColor: '#10b981 #d1fae5'
          }}
        >
          <style jsx>{`
            div::-webkit-scrollbar {
              width: 8px;
            }
            div::-webkit-scrollbar-track {
              background: #d1fae5;
              border-radius: 4px;
            }
            div::-webkit-scrollbar-thumb {
              background: #10b981;
              border-radius: 4px;
            }
            div::-webkit-scrollbar-thumb:hover {
              background: #059669;
            }
          `}</style>
          {children}
        </div>
      </div>
    </div>
  );
};

export default Modal;