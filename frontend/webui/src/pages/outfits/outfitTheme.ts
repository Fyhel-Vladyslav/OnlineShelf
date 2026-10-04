import type { ThemeConfig } from 'antd';

const lightText = {
  colorText: 'rgba(0, 0, 0, 0.88)',
  colorTextHeading: 'rgba(0, 0, 0, 0.88)',
  colorTextSecondary: 'rgba(0, 0, 0, 0.65)',
  colorTextTertiary: 'rgba(0, 0, 0, 0.45)',
  colorTextDescription: 'rgba(0, 0, 0, 0.45)',
  colorTextLabel: 'rgba(0, 0, 0, 0.65)',
  colorTextDisabled: 'rgba(0, 0, 0, 0.25)',
};

// Сторінка лежить на темному фоні сайту (#242424) — текст білий, як в index.css
export const pageTheme: ThemeConfig = {
  token: {
    colorText: 'rgba(255, 255, 255, 0.87)',
    colorTextHeading: '#ffffff',
    colorTextSecondary: 'rgba(255, 255, 255, 0.65)',
    colorTextTertiary: 'rgba(255, 255, 255, 0.55)',
    colorTextDescription: 'rgba(255, 255, 255, 0.55)',
    colorTextLabel: 'rgba(255, 255, 255, 0.65)',
    colorTextDisabled: 'rgba(255, 255, 255, 0.35)',
    colorSplit: 'rgba(255, 255, 255, 0.25)',
  },
  components: {
    // Кнопки, алерти й теги мають власний світлий фон — лишаємо їм темний текст
    Button: { ...lightText },
    Alert: { ...lightText },
    Tag: { ...lightText },
  },
};

// Для білих карток усередині сторінки повертаємо звичайний темний текст
export const lightSurfaceTheme: ThemeConfig = {
  token: { ...lightText, colorSplit: 'rgba(5, 5, 5, 0.06)' },
};
