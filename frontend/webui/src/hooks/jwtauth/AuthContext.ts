import { createContext } from "react";
interface AuthContextType {
  roles: string[];
  isAdmin: boolean;
}

export const AuthContext = createContext<AuthContextType | null>(null);