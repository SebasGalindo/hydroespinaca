'use client';

import React, { useState } from 'react';
import { useAuthStore } from '@hydroespinaca/shared';
import { TermsModal } from './TermsModal';

export function TermsGuard() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const session = useAuthStore((s) => s.session);
  const acceptTerms = useAuthStore((s) => s.acceptTerms);
  const [accepting, setAccepting] = useState(false);

  const needsAcceptance = isAuthenticated && session !== null && !session.hasAcceptedTerms;

  if (!needsAcceptance) return null;

  const handleAccept = async () => {
    setAccepting(true);
    try {
      await acceptTerms();
    } finally {
      setAccepting(false);
    }
  };

  return (
    <TermsModal
      onAccept={accepting ? undefined : handleAccept}
      readOnly={false}
    />
  );
}
