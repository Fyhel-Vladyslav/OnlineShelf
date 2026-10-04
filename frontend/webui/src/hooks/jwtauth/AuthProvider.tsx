
import { useMemo } from "react";
import {AuthContext} from "./AuthContext";
import { getUserRoles } from "./getUserRoles";

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
const roles = useMemo(() => getUserRoles(), []);
  return (
    <AuthContext.Provider
      value={{
        roles,
        isAdmin: roles.includes("Admin"),
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}