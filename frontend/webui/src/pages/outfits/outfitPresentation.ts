import type { Weather } from "@/api/offers/offersApi";

export type Coords = { latitude: number; longitude: number };

export const weatherIcons: Record<Weather, string> = {
  0: "❔",
  1: "🔥",
  2: "☀️",
  3: "🌤️",
  4: "☁️",
  5: "🌧️",
  6: "❄️",
  7: "⛈️",
};

export const formatPercent = (value: number) => `${(value * 100).toFixed(1)}%`;

export const formatMultiplier = (value: number) => `×${value.toFixed(2)}`;

export const formatTemperature = (value: number) => `${value > 0 ? "+" : ""}${value.toFixed(1)} °C`;

// "ColorClashPenaltyRule" → "Color clash"
export const formatRuleName = (rule: string) =>
  rule
    .replace(/PenaltyRule$|Rule$/, "")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^(.)(.*)$/, (_, first: string, rest: string) => first + rest.toLowerCase());
