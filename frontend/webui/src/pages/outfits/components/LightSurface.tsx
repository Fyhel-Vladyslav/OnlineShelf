import React from 'react';
import { ConfigProvider } from 'antd';
import { lightSurfaceTheme } from '../outfitTheme';

/** Обгортка для білих карток на темній сторінці: повертає темний колір тексту. */
const LightSurface: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <ConfigProvider theme={lightSurfaceTheme}>{children}</ConfigProvider>
);

export default LightSurface;
