import { useQuery } from "@tanstack/react-query";
import { offersApi } from "@/api/offers/offersApi";
import type { WeatherResponse } from "@/api/offers/offersApi";

export const useWeather = (latitude?: number | null, longitude?: number | null) => {
  const hasCoords = latitude != null && longitude != null;

  return useQuery<WeatherResponse>({
    // Бекенд округлює координати до 2 знаків, тож і ключ кешу округлюємо
    queryKey: ["weather", latitude?.toFixed(2), longitude?.toFixed(2)],
    queryFn: async () => {
      const res = await offersApi.getWeather(latitude!, longitude!);
      return res.data;
    },
    enabled: hasCoords,
    staleTime: 1000 * 60 * 30, // як TTL кешу на бекенді
    retry: 1,
  });
};
