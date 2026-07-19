import { jwtDecode} from "jwt-decode";

interface JwtPayload {
  roles?: string[];
}

function getToken() { return localStorage.getItem("access_token"); }
// Внутрішня функція для рефрешу за аналогією
function getRefreshToken() { return localStorage.getItem("refreshAccessToken"); }

export const authService = {
  getUserRoles: () => {
    const token = getToken();
    if (!token) return [];
  
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      return decoded.roles ?? [];
    } catch {
      return [];
    }
  },

  setToken: (token: string) => {
    localStorage.setItem('access_token', token);
  },

  getToken: () => {
    return getToken();
  },

  // --- ДОДАЄМО НОВІ МЕТОДИ ДЛЯ REFRESH TOKEN ---
  
  getRefreshToken: () => {
    return getRefreshToken();
  },

  setRefreshToken: (refreshToken: string) => {
    localStorage.setItem('refreshAccessToken', refreshToken);
    
  },

  clearTokens: () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refreshAccessToken');
  }
};