'use client';

import React, { useEffect, useState } from 'react';
import { authService } from '@hydroespinaca/shared';
import type { TermsContent } from '@hydroespinaca/shared';

interface TermsModalProps {
  onAccept?: () => void;
  onClose?: () => void;
  readOnly?: boolean;
}

export function TermsModal({ onAccept, onClose, readOnly = false }: TermsModalProps) {
  const [terms, setTerms] = useState<TermsContent | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    authService.getTerms()
      .then(setTerms)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 shrink-0">
          <h2 className="text-lg font-bold text-gray-900">
            {terms?.title ?? 'Términos y Condiciones'}
          </h2>
          {(readOnly || onClose) && (
            <button
              onClick={onClose}
              className="text-gray-500 hover:text-gray-700 focus:outline-none"
              aria-label="Cerrar"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
          {loading && (
            <div className="flex justify-center py-8">
              <div className="animate-spin h-8 w-8 border-4 border-green-600 border-t-transparent rounded-full" />
            </div>
          )}

          {!loading && terms && (
            <>
              <p className="text-xs text-gray-400">Última actualización: {terms.lastUpdated}</p>
              {terms.sections.map((section, idx) => (
                <div key={idx}>
                  <h3 className="font-semibold text-gray-800 mb-1">{section.title}</h3>
                  <p className="text-sm text-gray-600 leading-relaxed">{section.content}</p>
                </div>
              ))}
            </>
          )}

          {!loading && !terms && (
            <p className="text-sm text-red-600">No se pudieron cargar los términos. Intenta de nuevo.</p>
          )}
        </div>

        {/* Footer */}
        {!readOnly && (
          <div className="px-6 py-4 border-t border-gray-200 flex justify-end gap-3 shrink-0">
            {onClose && (
              <button
                onClick={onClose}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg"
              >
                Cancelar
              </button>
            )}
            <button
              onClick={onAccept}
              disabled={loading}
              className="px-5 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-lg disabled:opacity-50"
            >
              Acepto los términos y condiciones
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
