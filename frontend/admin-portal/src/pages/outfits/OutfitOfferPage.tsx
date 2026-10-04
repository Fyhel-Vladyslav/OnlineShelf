import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Steps, Typography } from 'antd';
import { ThunderboltOutlined } from '@ant-design/icons';
import { isAxiosError } from 'axios';
import type { GenerateOutfitsRequest } from '@/api/offers/offersApi';
import { authService } from '@/hooks/jwtauth/AuthService';
import { getGenerateErrorMessages, useGenerateOutfits } from '@/hooks/offers/useGenerateOutfits';
import { useNotification } from '@/notification/useNotification';
import ContextStep, { type GeoStatus } from './components/ContextStep';
import ItemPickerStep from './components/ItemPickerStep';
import OutfitResults from './components/OutfitResults';
import type { Coords } from './outfitPresentation';
import styles from './OutfitOfferPage.module.css';

const { Title, Text } = Typography;

const STEP_CONTEXT = 0;
const STEP_INCLUDE = 1;
const STEP_EXCLUDE = 2;
const STEP_REFERENCE = 3; // бекенд поки не підтримує
const STEP_RESULTS = 4;

const toggleId = (ids: string[], id: string) =>
  ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id];

const OutfitOfferPage: React.FC = () => {
  const [step, setStep] = useState(STEP_CONTEXT);

  // Context
  const [coords, setCoords] = useState<Coords | null>(null);
  const [geoStatus, setGeoStatus] = useState<GeoStatus>('idle');
  const [geoError, setGeoError] = useState<string>();
  const [skipLocation, setSkipLocation] = useState(false);
  const [onlyFavorite, setOnlyFavorite] = useState(false);
  const [topN, setTopN] = useState(5);

  // Include / Exclude
  const [includeIds, setIncludeIds] = useState<string[]>([]);
  const [excludeIds, setExcludeIds] = useState<string[]>([]);

  const generate = useGenerateOutfits();
  const { notify } = useNotification();

  const locate = useCallback(() => {
    if (!('geolocation' in navigator)) {
      setGeoStatus('unsupported');
      setGeoError('Браузер не підтримує геолокацію.');
      return;
    }

    setGeoStatus('locating');
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setCoords({ latitude: position.coords.latitude, longitude: position.coords.longitude });
        setGeoStatus('located');
        setGeoError(undefined);
      },
      (error) => {
        setGeoStatus('error');
        setGeoError(error.code === error.PERMISSION_DENIED
          ? 'Доступ до геолокації заборонено. Введіть координати вручну або продовжіть без локації.'
          : 'Не вдалося отримати координати. Введіть їх вручну або продовжіть без локації.');
      },
      { timeout: 10000, maximumAge: 1000 * 60 * 30 },
    );
  }, []);

  useEffect(() => {
    locate();
  }, [locate]);

  const runGenerate = (request: GenerateOutfitsRequest) => {
    generate.mutate(request, { onSuccess: () => setStep(STEP_RESULTS) });
  };

  const handleGenerate = () => {
    const userId = authService.getUserId();
    if (!userId) {
      notify({ text: 'Не вдалося визначити користувача. Увійдіть у систему знову.', code: 401, time: 6000 });
      return;
    }

    const location = skipLocation ? null : coords;
    runGenerate({
      userId,
      // Координати передаються або обидві, або жодна
      ...(location && { latitude: location.latitude, longitude: location.longitude }),
      includeItemIds: includeIds,
      excludeItemIds: excludeIds,
      onlyFavorite,
      topN,
    });
  };

  const handleRegenerate = () => {
    if (generate.variables) runGenerate(generate.variables);
  };

  const hasResult = !!generate.data;

  const stepItems = [
    { title: 'Context', description: 'Погода, сезон' },
    { title: 'Include', description: includeIds.length ? `Обрано: ${includeIds.length}` : 'Обов\'язкові речі' },
    { title: 'Exclude', description: excludeIds.length ? `Обрано: ${excludeIds.length}` : 'Небажані речі' },
    { title: 'Reference', description: 'скоро', disabled: true },
    { title: 'Результати', disabled: !hasResult },
  ];

  const nextStep = step === STEP_EXCLUDE ? null : step + 1;

  return (
    <div className={styles.page}>
      <Title level={2}>Підбір образу</Title>

      <Steps
        className={styles.steps}
        current={step}
        onChange={setStep}
        items={stepItems}
      />

      {step === STEP_CONTEXT && (
        <ContextStep
          coords={coords}
          geoStatus={geoStatus}
          geoError={geoError}
          skipLocation={skipLocation}
          onlyFavorite={onlyFavorite}
          topN={topN}
          onLocate={locate}
          onManualCoords={(manual) => {
            setCoords(manual);
            setGeoError(undefined);
          }}
          onSkipLocationChange={setSkipLocation}
          onOnlyFavoriteChange={setOnlyFavorite}
          onTopNChange={setTopN}
        />
      )}

      {step === STEP_INCLUDE && (
        <ItemPickerStep
          mode="include"
          selectedIds={includeIds}
          blockedIds={excludeIds}
          onToggle={(id) => setIncludeIds((ids) => toggleId(ids, id))}
          onClear={() => setIncludeIds([])}
        />
      )}

      {step === STEP_EXCLUDE && (
        <ItemPickerStep
          mode="exclude"
          selectedIds={excludeIds}
          blockedIds={includeIds}
          onToggle={(id) => setExcludeIds((ids) => toggleId(ids, id))}
          onClear={() => setExcludeIds([])}
        />
      )}

      {step === STEP_REFERENCE && <Text type="secondary">Цей крок буде доступний згодом.</Text>}

      {step === STEP_RESULTS && generate.data && (
        <OutfitResults result={generate.data} isPending={generate.isPending} onRegenerate={handleRegenerate} />
      )}

      {/* Помилка останньої генерації повністю (нотифікація обрізає довгий текст) */}
      {generate.isError && (
        <Alert
          style={{ marginTop: 16 }}
          type={isAxiosError(generate.error) && generate.error.response?.status === 400 ? 'warning' : 'error'}
          showIcon
          message="Не вдалося згенерувати образи"
          description={getGenerateErrorMessages(generate.error).map((m, i) => <div key={i}>{m}</div>)}
        />
      )}

      {step !== STEP_RESULTS && (
        <div className={styles.footer}>
          <Button disabled={step === STEP_CONTEXT} onClick={() => setStep(step - 1)}>Назад</Button>
          <div style={{ display: 'flex', gap: 8 }}>
            {nextStep !== null && <Button onClick={() => setStep(nextStep)}>Далі</Button>}
            <Button
              type="primary"
              icon={<ThunderboltOutlined />}
              onClick={handleGenerate}
              loading={generate.isPending}
              disabled={generate.isPending}
            >
              Згенерувати
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};

export default OutfitOfferPage;
