import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { authService } from "@/hooks/jwtauth/AuthService";
import type { TokenResponse } from './userApi'; // Імпортуємо новий тип відповіді

export const httpClient = axios.create({
  baseURL: "http://localhost:5000",
  headers: {
    "Content-Type": "application/json",
  },
});

interface RetryQueueItem {
  resolve: (value: string) => void;
  reject: (error: any) => void;
}

let isRefreshing = false;
let failedQueue: RetryQueueItem[] = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

// 1. Request Interceptor
httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = authService.getToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// 2. Response Interceptor
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    // Якщо помилка не 401 або цей запит вже є повторним після рефрешу
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Запобігаємо нескінченному циклу, якщо сам запит на оновлення токена падає з 401
    if (originalRequest.url?.includes("/users/refresh-token")) {
      handleLogout();
      return Promise.reject(error);
    }

    // Якщо рефреш вже виконується іншим запитом, ставимо цей запит у чергу
    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((token) => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return httpClient(originalRequest);
        })
        .catch((err) => Promise.reject(err));
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const currentRefreshToken = authService.getRefreshToken();
      
      if (!currentRefreshToken) {
        throw new Error("No refresh token available");
      }

      const refreshResponse = await httpClient.post<TokenResponse>("/users/refresh-token", {
        refreshToken: currentRefreshToken
      });

      const { accessToken: newAccess, refreshToken: newRefresh } = refreshResponse.data;

      // Зберігаємо оновлені токени через authService
      authService.setToken(newAccess);
      authService.setRefreshToken(newRefresh);

      // Сповіщаємо чергу про успішний рефреш
      processQueue(null, newAccess);

      // Оновлюємо заголовок для поточного (оригінального) запиту
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${newAccess}`;
      }
      return httpClient(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError, null);
      handleLogout();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

function handleLogout() {
  authService.clearTokens();
  const currentPath = window.location.pathname + window.location.search;
  window.location.href = `/login?redirectTo=${encodeURIComponent(currentPath)}`;
}