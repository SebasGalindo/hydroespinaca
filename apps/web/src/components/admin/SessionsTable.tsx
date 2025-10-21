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
  const [currentTime, setCurrentTime] = useState(new Date());

  // Get token expiry configuration from environment
  const ACCESS_TOKEN_EXPIRY_MINUTES = Number(process.env.NEXT_PUBLIC_ACCESS_TOKEN_EXPIRY_MINUTES) || 60;
  const REFRESH_TOKEN_EXPIRY_DAYS = Number(process.env.NEXT_PUBLIC_REFRESH_TOKEN_EXPIRY_DAYS) || 7;

  const MS_PER_MINUTE = 60 * 1000;
  const MS_PER_DAY = 24 * 60 * 60 * 1000;

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

  const getTimeUntilExpiration = (expiresAt: string): { text: string; color: string; expired: boolean } => {
    const exp = new Date(expiresAt).getTime();
    const diff = exp - currentTime.getTime();

    if (diff <= 0) {
      return { text: 'Expirada', color: 'bg-gray-100 text-gray-800', expired: true };
    }

    const days = Math.floor(diff / MS_PER_DAY);
    const hours = Math.floor((diff % MS_PER_DAY) / (60 * 60 * 1000));
    const minutes = Math.floor((diff % (60 * 60 * 1000)) / MS_PER_MINUTE);

    if (days > 1) {
      return {
        text: `${days}d ${hours}h`,
        color: 'bg-green-100 text-green-800',
        expired: false
      };
    } else if (days === 1) {
      return {
        text: `1d ${hours}h`,
        color: 'bg-green-100 text-green-800',
        expired: false
      };
    } else if (hours > 3) {
      return {
        text: `${hours}h ${minutes}m`,
        color: 'bg-green-100 text-green-800',
        expired: false
      };
    } else if (hours > 0) {
      return {
        text: `${hours}h ${minutes}m`,
        color: 'bg-yellow-100 text-yellow-800',
        expired: false
      };
    } else {
      return {
        text: `${minutes}m`,
        color: 'bg-red-100 text-red-800',
        expired: false
      };
    }
  };

  const getAccessTokenExpiration = (lastActivity: string): { text: string; color: string; expired: boolean } => {
    const lastActivityDate = new Date(lastActivity);
    const expirationTime = new Date(lastActivityDate.getTime() + ACCESS_TOKEN_EXPIRY_MINUTES * MS_PER_MINUTE);
    const diff = expirationTime.getTime() - currentTime.getTime();

    if (diff <= 0) {
      return { text: 'Expirado', color: 'text-gray-500', expired: true };
    }

    const minutes = Math.floor(diff / MS_PER_MINUTE);
    const seconds = Math.floor((diff % MS_PER_MINUTE) / 1000);

    if (minutes > 0) {
      return { text: `${minutes}m ${seconds}s`, color: 'text-gray-600', expired: false };
    } else {
      return { text: `${seconds}s`, color: 'text-gray-600', expired: false };
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
              const sessionExpiry = getTimeUntilExpiration(session.expiresAt);
              const accessTokenExpiry = getAccessTokenExpiration(session.lastActivity);
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

                      <div className="grid grid-cols-3 gap-4 text-sm">
                        <div>
                          <p className="text-gray-500">Creada</p>
                          <p className="text-gray-900 font-medium">{formatDate(session.createdAt)}</p>
                        </div>

                        <div>
                          <p className="text-gray-500">Última actividad</p>
                          <p className="text-gray-900 font-medium">{formatDate(session.lastActivity)}</p>
                        </div>

                        <div>
                          <p className="text-gray-500">Token refresca en</p>
                          <p className={`font-medium ${accessTokenExpiry.color}`}>
                            {accessTokenExpiry.text}
                          </p>
                        </div>
                      </div>

                      {isRevoked && !accessTokenExpiry.expired && (
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
                                Esta sesión será cerrada automáticamente cuando el token actual expire en{' '}
                                <span className="font-semibold">{accessTokenExpiry.text}</span>
                              </p>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>

                    <div className="ml-4 flex flex-col items-end gap-2">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${sessionExpiry.color}`}>
                        {isRevoked ? 'Revocada' : sessionExpiry.text}
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
