import { jwtDecode} from "jwt-decode";

interface JwtPayload {
  roles?: string[];
  nameid?: string;
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

  // Id користувача з claim `nameid` (JwtRegisteredClaimNames.NameId на бекенді)
  getUserId: (): string | null => {
    const token = getToken();
    if (!token) return null;

    try {
      return jwtDecode<JwtPayload>(token).nameid ?? null;
    } catch {
      return null;
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