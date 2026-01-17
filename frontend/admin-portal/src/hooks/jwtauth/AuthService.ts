import { jwtDecode} from "jwt-decode";
interface JwtPayload {
  roles?: string[];
}

function getToken(){ return localStorage.getItem("access_token");}

export const authService = {
    getUserRoles: () =>
    {
      const token = getToken();
      if (!token) return [];
    
      try {
        const decoded = jwtDecode<JwtPayload>(token);
        return decoded.roles ?? [];
      } catch {
        return [];
      }
    
    },

    setToken: (token: string) =>
        {
            localStorage.setItem('access_token', token);
        },

    getToken: () =>
    {
        const token = getToken();
        return token;
    }

};