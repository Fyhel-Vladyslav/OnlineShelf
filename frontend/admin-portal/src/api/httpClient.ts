// import axios from "axios";
// import { authService } from "@/hooks/jwtauth/AuthService";

// export const httpClient = axios.create({
//   baseURL: "http://localhost:5000",
// });

// httpClient.interceptors.request.use((config) => {
//   const token = authService.getToken();
//   if (token) {
//     config.headers.Authorization = `Bearer ${token}`;
//   }
//   return config;
// });

import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { authService } from "@/hooks/jwtauth/AuthService";

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

// 1. Request Interceptor (Твій базовий, типізований під актуальний Axios)
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

// 2. Response Interceptor (Обробка 401 та черга запитів)
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    if (originalRequest.url?.includes("/auth/refresh")) {
      handleLogout();
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
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
      const refreshToken = authService.getRefreshToken();
      
      if (!refreshToken) {
        throw new Error("No refresh token available");
      }

      // Робимо ізольований запит через чистий axios instance
      const response = await axios.post<{ accessToken: string; refreshAccessToken: string }>(
        `${httpClient.defaults.baseURL}/auth/refresh`, // Зміни шлях, якщо на бекенді він інший
        { refreshToken }
      );

      const { accessToken: newAccess, refreshAccessToken: newRefresh } = response.data;

      // Зберігаємо оновлені токени через розширений authService
      authService.setToken(newAccess);
      authService.setRefreshToken(newRefresh);

      processQueue(null, newAccess);

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
  // Очищаємо localStorage через метод сервісу
  authService.clearTokens();

  // "Запікаємо" поточний URL для Redirect back механізму
  const currentPath = window.location.pathname + window.location.search;
  
  // Викидаємо на логін
  window.location.href = `/login?redirectTo=${encodeURIComponent(currentPath)}`;
}