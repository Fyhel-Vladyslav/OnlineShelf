import axios from "axios";
import { authService } from "@/hooks/jwtauth/AuthService";

export const httpClient = axios.create({
  baseURL: "https://localhost:44300",
  headers: {
    "Content-Type": "application/json",
  },
});

httpClient.interceptors.request.use((config) => {
  const token = authService.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});