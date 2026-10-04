import React from 'react';
import { Navigate, useLocation, Outlet } from 'react-router-dom';
import { authService } from "@/hooks/jwtauth/AuthService";

export const ProtectedRoute: React.FC = () => {
  const location = useLocation();
  const hasAccessToken = authService.getToken();
  const hasRefreshToken = authService.getRefreshToken();
  if (!hasAccessToken && !hasRefreshToken) {
    console.log("User has no tokens. Sending to login page");
    
    const currentPath = location.pathname + location.search;
    //return <Navigate to={`/login?redirectTo=${encodeURIComponent(currentPath)}`} replace />;
  }

  // Outlet автоматично відрендерить той вкладений Route, на який намагається зайти користувач
  return <Outlet />;
};