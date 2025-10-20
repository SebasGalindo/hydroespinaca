'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { AdminRoute } from '@/components/auth/AdminRoute';
import PageLayout from '@/components/layout/PageLayout';
import { SessionsTable } from '@/components/admin/SessionsTable';
import { adminService } from '@hydroespinaca/shared';
import Swal from 'sweetalert2';
import type { UserSessionsDto } from '@hydroespinaca/shared';

const REFRESH_INTERVAL = 30000; // 30 seconds

export default function AdminDashboardPage() {
  const [sessions, setSessions] = useState<UserSessionsDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  const loadSessions = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const sessionsData = await adminService.getActiveSessions();
      setSessions(sessionsData);
      setLastRefresh(new Date());
    } catch (err: any) {
      setError(err.message || 'Error al cargar las sesiones activas');
      console.error('Error loading sessions:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSessions();

    // Auto-refresh every 30 seconds
    const interval = setInterval(loadSessions, REFRESH_INTERVAL);

    return () => clearInterval(interval);
  }, [loadSessions]);

  const handleRevokeSession = async (sessionId: string) => {
    try {
      await adminService.revokeSession(sessionId);

      // Update local state to mark session as revoked
      setSessions(prevSessions =>
        prevSessions.map(userSessions => ({
          ...userSessions,
          sessions: userSessions.sessions.map(session =>
            session.sessionId === sessionId
              ? { ...session, revoked: true, revokedAt: new Date().toISOString() }
              : session
          )
        }))
      );

      await Swal.fire({
        title: '¡Revocada!',
        text: 'La sesión ha sido revocada exitosamente.',
        icon: 'success',
        confirmButtonColor: '#16a34a',
        timer: 2000,
        showConfirmButton: false
      });
    } catch (err: any) {
      await Swal.fire({
        title: 'Error',
        text: `Error al revocar sesión: ${err.message}`,
        icon: 'error',
        confirmButtonColor: '#16a34a'
      });
      throw err;
    }
  };

  const formatLastRefresh = () => {
    if (!lastRefresh) return '';
    return lastRefresh.toLocaleTimeString('es-ES', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  };

  return (
    <AdminRoute>
      <PageLayout>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {/* Page Header */}
          <div className="mb-8">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-3xl font-bold text-gray-900">Dashboard Administrativo</h1>
                <p className="mt-2 text-sm text-gray-600">
                  Monitorea las sesiones activas y gestiona el acceso de los usuarios
                </p>
              </div>
              <button
                onClick={loadSessions}
                disabled={isLoading}
                className="inline-flex items-center px-4 py-2 bg-white text-gray-700 text-sm font-medium rounded-lg border border-gray-300 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-colors disabled:opacity-50"
              >
                <svg className={`w-5 h-5 mr-2 ${isLoading ? 'animate-spin' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                Refrescar
              </button>
            </div>

            {/* Stats Cards */}
            <div className="mt-6 grid grid-cols-1 gap-5 sm:grid-cols-3">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <svg className="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                      </svg>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">Sesiones Activas</dt>
                        <dd className="text-lg font-semibold text-gray-900">
                          {sessions.reduce((total, userSessions) => total + userSessions.sessions.length, 0)}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <svg className="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">Última Actualización</dt>
                        <dd className="text-lg font-semibold text-gray-900">{formatLastRefresh() || '-'}</dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <svg className="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">Auto-Refresh</dt>
                        <dd className="text-lg font-semibold text-gray-900">30s</dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="ml-3">
                  <p className="text-sm text-red-600">{error}</p>
                  <button
                    onClick={loadSessions}
                    className="mt-2 text-sm font-medium text-red-600 hover:text-red-800 underline"
                  >
                    Reintentar
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Sessions Table */}
          <div className="mt-6">
            <SessionsTable
              sessions={sessions}
              onRevokeSession={handleRevokeSession}
              isLoading={isLoading}
            />
          </div>
        </div>
      </PageLayout>
    </AdminRoute>
  );
}
