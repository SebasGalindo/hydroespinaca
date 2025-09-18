import React, { useMemo } from 'react';
import { useAuth } from '@hydroespinaca/shared-ui';

export const ProtectedPage: React.FC = () => {
  // Memoize the config object to prevent useAuth from re-running on every render
  const authConfig = useMemo(() => ({ platform: 'web' as const }), []);
  const { session, logout, isLoading } = useAuth(authConfig);


  const handleLogout = async () => {
    try {
      await logout();
      window.location.href = '/';
    } catch (error) {
      console.error('Logout failed:', error);
      // Even if logout fails, redirect to public page
      window.location.href = '/';
    }
  };

  if (isLoading) {
    return <div className="loading">Cargando...</div>;
  }

  if (!session) {
    return <div>No hay sesión activa. Redirigiendo...</div>;
  }

  return (
    <div className="protected-page">
      <header className="page-header">
        <h1>Panel de Control - HydroEspinaca</h1>
        <div className="user-info">
          <span>Sesión activa</span>
          <button onClick={handleLogout} className="btn btn-secondary">
            Cerrar Sesión
          </button>
        </div>
      </header>

      <main className="dashboard">
        <div className="dashboard-section">
          <h2>Estado del Sistema</h2>
          <div className="status-cards">
            <div className="status-card">
              <h3>Sensores</h3>
              <p className="status-ok">✓ Funcionando</p>
              <small>Última actualización: hace 2 min</small>
            </div>
            
            <div className="status-card">
              <h3>Actuadores</h3>
              <p className="status-ok">✓ Funcionando</p>
              <small>Bomba de nutrientes activa</small>
            </div>
            
            <div className="status-card">
              <h3>Conectividad</h3>
              <p className="status-ok">✓ En línea</p>
              <small>MQTT conectado</small>
            </div>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Lecturas Recientes</h2>
          <div className="sensor-readings">
            <div className="reading">
              <span className="sensor-name">Temperatura</span>
              <span className="sensor-value">22.5°C</span>
            </div>
            <div className="reading">
              <span className="sensor-name">Humedad</span>
              <span className="sensor-value">65%</span>
            </div>
            <div className="reading">
              <span className="sensor-name">pH</span>
              <span className="sensor-value">6.8</span>
            </div>
            <div className="reading">
              <span className="sensor-name">Nutrientes</span>
              <span className="sensor-value">850 ppm</span>
            </div>
          </div>
        </div>

        <div className="dashboard-section">
          <h2>Acciones Rápidas</h2>
          <div className="actions">
            <button className="btn btn-primary">Activar Riego</button>
            <button className="btn btn-primary">Añadir Nutrientes</button>
            <button className="btn btn-secondary">Ver Histórico</button>
            <button className="btn btn-secondary">Configurar Alertas</button>
          </div>
        </div>
      </main>
    </div>
  );
};