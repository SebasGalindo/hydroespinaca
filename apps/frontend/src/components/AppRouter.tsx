import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from '@hydroespinaca/shared-hooks';
import { PublicPage } from '../pages/PublicPage';
import { LoginPage } from '../pages/LoginPage';
import { ProtectedPage } from '../pages/ProtectedPage';
import { WEB_ROUTES } from '@hydroespinaca/shared-utils';

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public Route */}
        <Route 
          path={WEB_ROUTES.PUBLIC} 
          element={<PublicPage />} 
        />
        
        {/* Login Route */}
        <Route 
          path={WEB_ROUTES.LOGIN} 
          element={<LoginPage />} 
        />
        
        {/* Protected Routes */}
        <Route 
          path={WEB_ROUTES.PROTECTED} 
          element={
            <ProtectedRoute redirectTo={WEB_ROUTES.LOGIN}>
              <ProtectedPage />
            </ProtectedRoute>
          } 
        />
        
        <Route 
          path={WEB_ROUTES.DASHBOARD} 
          element={
            <ProtectedRoute redirectTo={WEB_ROUTES.LOGIN}>
              <ProtectedPage />
            </ProtectedRoute>
          } 
        />
        
        {/* Fallback - Redirect to home */}
        <Route 
          path="*" 
          element={<Navigate to={WEB_ROUTES.PUBLIC} replace />} 
        />
      </Routes>
    </BrowserRouter>
  );
};