import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';

export const ProtectedRoute: React.FC = () => {
  const location = useLocation();
  const isAuthenticated = !!localStorage.getItem('access_token'); 

console.log(isAuthenticated);

  if (!isAuthenticated) {
    const currentPath = location.pathname + location.search;
    return <Navigate to={`/login?redirectTo=${encodeURIComponent(currentPath)}`} replace />;
  }

  // Outlet автоматично відрендерить той вкладений Route, на який намагається зайти користувач
  return <Outlet />;
};