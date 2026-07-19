import axios, { AxiosError, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { authService } from "@/hooks/jwtauth/AuthService";
import type { TokenResponse } from "@/api/users/userApi"; // Імпортуємо новий тип відповіді

// Створення базового екземпляра Axios з налаштуваннями
export const httpClient = axios.create({
  baseURL: "http://localhost:5000",
  headers: {
    "Content-Type": undefined
  },
});

// Опис типу елемента для черги повторних запитів.
// Зберігає функції resolve та reject для Promise, щоб виконати або скасувати запит пізніше.
interface RetryQueueItem {
  resolve: (value: string) => void;
  reject: (error: any) => void;
}

// Прапорець, який вказує, чи виконується в даний момент запит на оновлення токена.
let isRefreshing = false;

// Черга для запитів, які "поставили на паузу", поки оновлюється головний токен.
let failedQueue: RetryQueueItem[] = [];

/**
 * Функція processQueue обробляє чергу заблокованих запитів після завершення рефрешу.
 * 
 * @param error - Помилка, якщо оновлення токена провалилося (всі запити в черзі відхиляються).
 * @param token - Новий access токен, якщо оновлення пройшло успішно (всі запити в черзі виконуються з новим токеном).
 */
const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error); // Скасовуємо запит із черги
    } else {
      prom.resolve(token!); // Передаємо новий токен для повторного запиту
    }
  });
  
  failedQueue = []; // Очищаємо чергу
};

// 1. Request Interceptor (Перехоплювач запитів)
// Спрацьовує *перед* тим, як будь-який запит піде на сервер.
httpClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Витягуємо поточний Access Token зі сховища
    const token = authService.getToken();
    
    // Якщо токен існує, автоматично додаємо його в заголовок Authorization для кожного запиту
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error) // Прокидуємо помилку конфігурації запиту далі
);

// 2. Response Interceptor (Перехоплювач відповідей)
// Спрацьовує *після* того, як сервер повернув відповідь або помилку.
httpClient.interceptors.response.use(
  (response) => response, // Якщо запит успішний (status 2xx), просто повертаємо результат
  async (error: AxiosError) => {
    // Кастимо конфіг запиту і додаємо кастомне поле _retry, щоб мітити запити, які вже пробували повторити
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    // Якщо помилка НЕ пов'язана з авторизацією (не 401) АБО цей запит вже є повторною спробою після рефрешу,
    // то ми нічого не робимо і просто повертаємо помилку в блок catch компонента/сервісу.
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Захист від нескінченного циклу: якщо сам запит на ендпоінт "/users/refresh-token" 
    // падає з помилкою 401 (наприклад, Refresh Token застарів), розлогінюємо користувача.
    if (originalRequest.url?.includes("/users/refresh-token")) {
      console.log("Alresady tried to refresh. Logout user");
      
      handleLogout();
      return Promise.reject(error);
    }

    // КЛЮЧОВИЙ МОМЕНТ ДЛЯ ПАРАЛЕЛЬНИХ ЗАПИТІВ:
    // Якщо в цей момент інший запит ВЖЕ запустив процес оновлення токена (`isRefreshing === true`),
    // ми не створюємо новий запит на рефреш. Замість цього ми створюємо новий Promise,
    // "заморожуємо" поточний запит і додаємо його resolve/reject в чергу `failedQueue`.
    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((token) => {
          // Коли Promise розрезолвиться (в processQueue), ми отримаємо новий токен,
          // оновимо заголовок оригінального запиту і виконаємо його знову.
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return httpClient(originalRequest);
        })
        .catch((err) => Promise.reject(err)); // Якщо рефреш першого запиту впав, цей теж відхиляємо
    }

    // Якщо це перший запит, який зловив 401, то саме він бере на себе роль "оновлювача"
    console.log("Trying to refresh token");
    
    originalRequest._retry = true; // Мітимо запит, щоб не зациклитись
    isRefreshing = true;          // Блокуємо інші запити, сигналізуючи, що процес пішов

    try {
      // Отримуємо збережений Refresh Token
      const currentRefreshToken = authService.getRefreshToken();
      
      if (!currentRefreshToken) {
        throw new Error("No refresh token available");
      }
console.log("Sending refresh token");

      // Робимо POST-запит на сервер для отримання нової пари токенів
      const refreshResponse = await httpClient.post<TokenResponse>("/users/refresh-token", {
        refreshToken: currentRefreshToken
      });

      // Деструктуризуємо нові токени з відповіді сервера
      const { accessToken: newAccess, refreshAccessToken: newRefresh } = refreshResponse.data;

      // Оновлюємо токени в локальному сховищі (localStorage/cookies через authService)
      authService.setToken(newAccess);
      authService.setRefreshToken(newRefresh);

      // Проходимо по черзі `failedQueue` і "будимо" всі інші запити, передаючи їм новий Access Token
      processQueue(null, newAccess);

      // Оновлюємо заголовок для поточного (першого) запиту, який ініціював рефреш
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${newAccess}`;
      }
      
      // Повторно відправляємо оригінальний запит на сервер і повертаємо його результат
      return httpClient(originalRequest);
    } catch (refreshError) {
      // Якщо під час оновлення токена сталася помилка (наприклад, Refresh Token протух на сервері):
      processQueue(refreshError, null); // Ламаємо всі запити, що чекали в черзі
      console.log("no tokens");
      
      handleLogout();                   // Викидаємо користувача на сторінку логіну
      return Promise.reject(refreshError);
    } finally {
      // Гарантовано знімаємо прапорець блокування, незалежно від результату рефрешу
      isRefreshing = false;
    }
  }
);

/**
 * Функція handleLogout очищає сесію користувача в разі повної невалідності токенів
 * та перенаправляє його на сторінку авторизації із збереженням попереднього шляху (Query Parameter),
 * щоб після логіну повернути користувача туди, де він був.
 */
function handleLogout() {
  authService.clearTokens(); // Видаляємо токени зі сховища
  
  // Запам'ятовуємо поточний URL (шлях + query-параметри), на якому користувач знаходився
  const currentPath = window.location.pathname + window.location.search;
  
  // Перенаправляємо на сторінку логіну, передаючи попередній шлях як параметр редиректу
  window.location.href = `/login?redirectTo=${encodeURIComponent(currentPath)}`;
}