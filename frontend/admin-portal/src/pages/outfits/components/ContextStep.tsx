import React, { useState } from 'react';
import { Alert, Button, Card, Checkbox, Col, InputNumber, Row, Space, Spin, Switch, Typography } from 'antd';
import { AimOutlined, EnvironmentOutlined } from '@ant-design/icons';
import { getCurrentSeasonPhase, seasonPhaseLabels, weatherLabels } from '@/api/offers/offersApi';
import { useWeather } from '@/hooks/offers/useWeather';
import { formatTemperature, weatherIcons, type Coords } from '../outfitPresentation';
import styles from '../OutfitOfferPage.module.css';
import LightSurface from './LightSurface';

const { Text } = Typography;

export type GeoStatus = 'idle' | 'locating' | 'located' | 'error' | 'unsupported';

interface ContextStepProps {
  coords: Coords | null;
  geoStatus: GeoStatus;
  geoError?: string;
  skipLocation: boolean;
  onlyFavorite: boolean;
  topN: number;
  onLocate: () => void;
  onManualCoords: (coords: Coords) => void;
  onSkipLocationChange: (skip: boolean) => void;
  onOnlyFavoriteChange: (value: boolean) => void;
  onTopNChange: (value: number) => void;
}

const ContextStep: React.FC<ContextStepProps> = ({
  coords, geoStatus, geoError, skipLocation, onlyFavorite, topN,
  onLocate, onManualCoords, onSkipLocationChange, onOnlyFavoriteChange, onTopNChange,
}) => {
  const [manualLat, setManualLat] = useState<number | null>(null);
  const [manualLon, setManualLon] = useState<number | null>(null);

  const effectiveCoords = skipLocation ? null : coords;
  const weather = useWeather(effectiveCoords?.latitude, effectiveCoords?.longitude);
  const seasonPhase = getCurrentSeasonPhase(new Date(), effectiveCoords?.latitude);

  const canApplyManual = manualLat != null && manualLon != null;

  return (
    <LightSurface>
    <Row gutter={[16, 16]}>
      <Col xs={24} lg={12}>
        <Card title={<><EnvironmentOutlined /> Локація</>}>
          <Space direction="vertical" style={{ width: '100%' }}>
            {geoStatus === 'locating' && <Spin size="small" tip="Визначаємо локацію…"><div style={{ height: 32 }} /></Spin>}

            {coords && (
              <Text>
                Координати: <Text strong>{coords.latitude.toFixed(2)}, {coords.longitude.toFixed(2)}</Text>
              </Text>
            )}

            {(geoStatus === 'error' || geoStatus === 'unsupported') && !coords && (
              <Alert
                type="warning"
                showIcon
                message="Не вдалося визначити локацію автоматично"
                description={geoError ?? 'Введіть координати вручну або продовжіть без локації.'}
              />
            )}

            <Button icon={<AimOutlined />} onClick={onLocate} disabled={skipLocation || geoStatus === 'locating' || geoStatus === 'unsupported'}>
              Визначити автоматично
            </Button>

            <Text type="secondary">Або введіть вручну:</Text>
            <Space wrap>
              <InputNumber
                placeholder="Широта"
                min={-90}
                max={90}
                step={0.01}
                value={manualLat}
                onChange={setManualLat}
                disabled={skipLocation}
              />
              <InputNumber
                placeholder="Довгота"
                min={-180}
                max={180}
                step={0.01}
                value={manualLon}
                onChange={setManualLon}
                disabled={skipLocation}
              />
              <Button
                disabled={skipLocation || !canApplyManual}
                onClick={() => onManualCoords({ latitude: manualLat!, longitude: manualLon! })}
              >
                Застосувати
              </Button>
            </Space>

            <Checkbox checked={skipLocation} onChange={(e) => onSkipLocationChange(e.target.checked)}>
              Продовжити без локації
            </Checkbox>
            {skipLocation && (
              <Alert type="info" showIcon message="Без локації погодні обмеження не застосовуються." />
            )}
          </Space>
        </Card>
      </Col>

      <Col xs={24} lg={12}>
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Card title="Погода і сезон">
            <Space direction="vertical" style={{ width: '100%' }}>
              {!effectiveCoords && <Text type="secondary">Погода недоступна без локації.</Text>}
              {effectiveCoords && weather.isLoading && <Spin size="small" />}
              {effectiveCoords && weather.isError && (
                <Alert type="warning" showIcon message="Не вдалося отримати погоду. Генерація працюватиме, але без погодних обмежень." />
              )}
              {effectiveCoords && weather.data && (
                <div className={styles.weatherCard}>
                  <span className={styles.weatherIcon}>{weatherIcons[weather.data.currentWeather] ?? weatherIcons[0]}</span>
                  <div>
                    <div className={styles.temperature}>{formatTemperature(weather.data.temperatureCelsius)}</div>
                    <Text>{weatherLabels[weather.data.currentWeather] ?? weatherLabels[0]}</Text>
                  </div>
                </div>
              )}
              <Text>
                Сезон: <Text strong>{seasonPhaseLabels[seasonPhase]}</Text>
              </Text>
            </Space>
          </Card>

          <Card title="Параметри">
            <Space direction="vertical">
              <Space>
                <Switch checked={onlyFavorite} onChange={onOnlyFavoriteChange} />
                <Text>Лише улюблені речі</Text>
              </Space>
              <Space>
                <Text>Кількість варіантів:</Text>
                <InputNumber
                  min={1}
                  max={20}
                  precision={0}
                  value={topN}
                  onChange={(value) => onTopNChange(value ?? 5)}
                />
              </Space>
            </Space>
          </Card>
        </Space>
      </Col>
    </Row>
    </LightSurface>
  );
};

export default ContextStep;
