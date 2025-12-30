import { jwtDecode} from "jwt-decode";
interface JwtPayload {
  roles?: string[];
}
export function getUserRoles(): string[] {
  const token = localStorage.getItem("access_token");
  if (!token) return [];

  try {
    const decoded = jwtDecode<JwtPayload>(token);
    return decoded.roles ?? [];
  } catch {
    return [];
  }
}