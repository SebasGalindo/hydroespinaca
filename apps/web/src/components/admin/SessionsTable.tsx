'use client';

import React, { useState, useEffect } from 'react';
import type { UserSessionsDto, SessionMonitorDto } from '@hydroespinaca/shared';

interface SessionsTableProps {
  sessions: UserSessionsDto[];
  onRevokeSession: (sessionId: string) => Promise<void>;
  isLoading?: boolean;
}

export const SessionsTable: React.FC<SessionsTableProps> = ({
  sessions,
  onRevokeSession,
  isLoading = false
}) => {
  const [revoking, setRevoking] = useState<Set<string>>(new Set());
  const [, setCurrentTime] = useState(new Date());

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  const formatDate = (dateString: string): string => {
    try {
      const date = new Date(dateString);
      return date.toLocaleString('es-ES', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  };

  const calculateExpirationTime = (lastActivity: string): Date => {
    const lastActivityDate = new Date(lastActivity);
    return new Date(lastActivityDate.getTime() + 60 * 60 * 1000); // +1 hour
  };

  const getTimeRemaining = (expirationDate: Date): { text: string; color: string; expired: boolean } => {
    const now = new Date();
    const diffMs = expirationDate.getTime() - now.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffSeconds = Math.floor((diffMs % 60000) / 1000);

    if (diffMs <= 0) {
      return { text: 'Expirando...', color: 'bg-gray-100 text-gray-800', expired: true };
    } else if (diffMinutes < 5) {
      return {
        text: `${diffMinutes}m ${diffSeconds}s`,
        color: 'bg-red-100 text-red-800',
        expired: false
      };
    } else if (diffMinutes < 15) {
      return { text: `${diffMinutes}m`, color: 'bg-yellow-100 text-yellow-800', expired: false };
    } else {
      const hours = Math.floor(diffMinutes / 60);
      const mins = diffMinutes % 60;
      return { text: `${hours}h ${mins}m`, color: 'bg-green-100 text-green-800', expired: false };
    }
  };

  const handleRevoke = async (sessionId: string) => {
    setRevoking(prev => new Set(prev).add(sessionId));
    try {
      await onRevokeSession(sessionId);
    } catch (error) {
      setRevoking(prev => {
        const newSet = new Set(prev);
        newSet.delete(sessionId);
        return newSet;
      });
    }
  };

  const truncateSessionId = (sessionId: string): string => {
    if (sessionId.length <= 12) return sessionId;
    return `${sessionId.substring(0, 8)}...${sessionId.substring(sessionId.length - 4)}`;
  };

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-16 bg-gray-200 rounded"></div>
        <div className="h-16 bg-gray-200 rounded"></div>
        <div className="h-16 bg-gray-200 rounded"></div>
      </div>
    );
  }

  if (sessions.length === 0) {
    return (
      <div className="text-center py-12 bg-gray-50 rounded-lg border border-gray-200">
        <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-gray-900">No hay sesiones activas</h3>
        <p className="mt-1 text-sm text-gray-500">No se encontraron usuarios conectados en este momento.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {sessions.map((userSessions: UserSessionsDto) => (
        <div key={userSessions.userId} className="bg-white rounded-lg shadow overflow-hidden">
          <div className="bg-gray-50 px-6 py-4 border-b border-gray-200">
            <div className="flex items-center">
              <div className="flex-shrink-0 h-12 w-12 rounded-full bg-purple-100 flex items-center justify-center">
                <span className="text-purple-600 font-semibold text-lg">
                  {userSessions.userName.charAt(0).toUpperCase()}
                </span>
              </div>
              <div className="ml-4">
                <h3 className="text-lg font-semibold text-gray-900">{userSessions.userName}</h3>
                <p className="text-sm text-gray-500">
                  {userSessions.sessions.length} sesión{userSessions.sessions.length !== 1 ? 'es' : ''} activa{userSessions.sessions.length !== 1 ? 's' : ''}
                </p>
              </div>
            </div>
          </div>

          <div className="divide-y divide-gray-200">
            {userSessions.sessions.map((session: SessionMonitorDto) => {
              const expirationTime = calculateExpirationTime(session.lastActivity);
              const timeRemaining = getTimeRemaining(expirationTime);
              const isRevoked = session.revoked || revoking.has(session.sessionId);

              return (
                <div key={session.sessionId} className="px-6 py-4 hover:bg-gray-50 transition-colors">
                  <div className="flex items-start justify-between">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-3 mb-2">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-blue-100 text-blue-800">
                          {session.clientId}
                        </span>
                        <span className="text-xs font-mono text-gray-500" title={session.sessionId}>
                          {truncateSessionId(session.sessionId)}
                        </span>
                      </div>

                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <p className="text-gray-500">Creada</p>
                          <p className="text-gray-900 font-medium">{formatDate(session.createdAt)}</p>
                        </div>
                        <div>
                          <p className="text-gray-500">Última actividad</p>
                          <p className="text-gray-900 font-medium">{formatDate(session.lastActivity)}</p>
                        </div>
                      </div>

                      {isRevoked && !timeRemaining.expired && (
                        <div className="mt-3 p-3 bg-orange-50 border border-orange-200 rounded-lg">
                          <div className="flex items-start gap-2">
                            <svg className="w-5 h-5 text-orange-600 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <div className="flex-1">
                              <p className="text-sm font-medium text-orange-900">
                                Sesión revocada
                              </p>
                              <p className="text-xs text-orange-700 mt-1">
                                Esta sesión será cerrada automáticamente cuando caduque el token actual en{' '}
                                <span className="font-semibold">{timeRemaining.text}</span>
                              </p>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>

                    <div className="ml-4 flex flex-col items-end gap-2">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${timeRemaining.color}`}>
                        {isRevoked ? 'Revocada' : timeRemaining.text}
                      </span>
                      {!isRevoked && (
                        <button
                          onClick={() => handleRevoke(session.sessionId)}
                          className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-red-700 bg-red-50 border border-red-200 rounded-lg hover:bg-red-100 transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                          title="Revocar sesión"
                        >
                          <svg className="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                          </svg>
                          Revocar
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
};
