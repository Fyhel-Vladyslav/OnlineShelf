import { httpClient } from "../httpClient";

// Enum Weather з OutfitOfferService (приходить числом)
export const Weather = {
  Unknown: 0,
  Hot: 1,
  Shiny: 2,
  Clear: 3,
  Cloudy: 4,
  Rainy: 5,
  Snowy: 6,
  Thunderstorm: 7,
} as const;
export type Weather = typeof Weather[keyof typeof Weather];

export const weatherLabels: Record<Weather, string> = {
  0: "Невідомо",
  1: "Спека",
  2: "Сонячно",
  3: "Ясно",
  4: "Хмарно",
  5: "Дощ",
  6: "Сніг",
  7: "Гроза",
};

export type OutfitSlot = "Top" | "Bottom" | "FullBody" | "Shoes" | "Outerwear" | "Accessory";

export const slotLabels: Record<OutfitSlot, string> = {
  Top: "Верх",
  Bottom: "Низ",
  FullBody: "Сукня/комбінезон",
  Shoes: "Взуття",
  Outerwear: "Верхній одяг",
  Accessory: "Аксесуар",
};

// Фази сезону 1..12, як у SeasonCalculator на бекенді
export const seasonPhaseLabels: Record<number, string> = {
  1: "Рання зима",
  2: "Середина зими",
  3: "Пізня зима",
  4: "Рання весна",
  5: "Середина весни",
  6: "Пізня весна",
  7: "Раннє літо",
  8: "Середина літа",
  9: "Пізнє літо",
  10: "Рання осінь",
  11: "Середина осені",
  12: "Пізня осінь",
};

/** Дзеркало SeasonCalculator.CurrentPhase: грудень = 1, ..., листопад = 12; південна півкуля зсунута на пів року. */
export const getCurrentSeasonPhase = (date: Date = new Date(), latitude?: number | null): number => {
  let phase = ((date.getMonth() + 1) % 12) + 1;
  if (latitude != null && latitude < 0) {
    phase = ((phase + 5) % 12) + 1;
  }
  return phase;
};

export type WeatherResponse = {
  currentWeather: Weather;
  temperatureCelsius: number;
};

export type GenerateOutfitsRequest = {
  userId: string;
  latitude?: number;
  longitude?: number;
  includeItemIds?: string[];
  excludeItemIds?: string[];
  onlyFavorite?: boolean;
  topN?: number;
};

export type OutfitItemDto = {
  itemId: string;
  name: string;
  slot: OutfitSlot;
  bigImage: string | null;
  isVirtual: boolean;
};

export type PairwiseScoreDto = {
  itemIdA: string;
  itemIdB: string;
  score: number;
};

export type OutfitDto = {
  items: OutfitItemDto[];
  finalScore: number;
  graphLevelScore: number;
  penaltyMultiplier: number;
  appliedPenalties: Record<string, number>;
  pairwiseScores: PairwiseScoreDto[];
};

export type GenerateOutfitsResponse = {
  outfits: OutfitDto[];
  weather: { condition: Weather; temperatureCelsius: number } | null;
  seasonPhase: number;
  warnings: string[];
  elapsedMs: number;
};

// 400 від FastEndpoints
export type ValidationErrorResponse = {
  statusCode: number;
  message: string;
  errors?: Record<string, string[]>;
};

// 503 у форматі ProblemDetails
export type ProblemDetailsResponse = {
  status: number;
  title?: string;
  detail?: string;
};

export const offersApi = {
  getWeather: (latitude: number, longitude: number) =>
    httpClient.get<WeatherResponse>("/offers/get-weather", { params: { latitude, longitude } }),

  generateOutfits: (request: GenerateOutfitsRequest) =>
    httpClient.post<GenerateOutfitsResponse>("/offers/generate", request, {
      headers: { "Content-Type": "application/json" },
    }),
};
