import React from 'react';
import { Alert, Button, Empty, Space, Spin, Tooltip, Typography } from 'antd';
import { CheckCircleFilled, StopOutlined } from '@ant-design/icons';
import type { ItemPreviewDto } from '@/api/shelfs/itemsApi';
import { useShelfs } from '@/hooks/shelfs/useShelfs';
import { useImage } from '@/hooks/items/useImage';
import noImageSmall from '@/assets/images/noPhotoLoadedSmall.jpg';
import styles from '../OutfitOfferPage.module.css';

const { Title, Text } = Typography;

export type PickMode = 'include' | 'exclude';

interface ItemPickerStepProps {
  mode: PickMode;
  selectedIds: string[];
  // Речі, обрані на протилежному кроці: їх не можна обрати тут
  blockedIds: string[];
  onToggle: (itemId: string) => void;
  onClear: () => void;
}

const ItemPickerStep: React.FC<ItemPickerStepProps> = ({ mode, selectedIds, blockedIds, onToggle, onClear }) => {
  const { data: shelfs, isLoading, isError, refetch } = useShelfs();

  const hint = mode === 'include'
    ? 'Оберіть речі, які обов\'язково мають бути в образі (не більше однієї на слот: верх, низ, взуття…).'
    : 'Оберіть речі, яких не має бути в жодному образі.';

  if (isLoading) return <Spin tip="Завантажуємо гардероб…"><div style={{ height: 80 }} /></Spin>;

  if (isError) {
    return (
      <Alert
        type="error"
        showIcon
        message="Не вдалося завантажити гардероб"
        action={<Button size="small" onClick={() => refetch()}>Повторити</Button>}
      />
    );
  }

  const nonEmptyShelfs = (shelfs ?? []).filter((s) => s.items.length > 0);

  return (
    <Space direction="vertical" style={{ width: '100%' }}>
      <Space wrap style={{ justifyContent: 'space-between', width: '100%' }}>
        <Text type="secondary">{hint}</Text>
        <Space>
          <Text>Обрано: {selectedIds.length}</Text>
          <Button size="small" onClick={onClear} disabled={selectedIds.length === 0}>Очистити</Button>
        </Space>
      </Space>

      {nonEmptyShelfs.length === 0 && <Empty description="У гардеробі ще немає речей" />}

      {nonEmptyShelfs.map((shelf) => (
        <div key={shelf.id}>
          <Title level={5} className={styles.shelfTitle}>{shelf.name}</Title>
          <div className={styles.itemGrid}>
            {shelf.items.map((item) => (
              <PickTile
                key={item.id}
                item={item}
                mode={mode}
                selected={selectedIds.includes(item.id)}
                blocked={blockedIds.includes(item.id)}
                onToggle={onToggle}
              />
            ))}
          </div>
        </div>
      ))}
    </Space>
  );
};

interface PickTileProps {
  item: ItemPreviewDto;
  mode: PickMode;
  selected: boolean;
  blocked: boolean;
  onToggle: (itemId: string) => void;
}

const PickTile: React.FC<PickTileProps> = ({ item, mode, selected, blocked, onToggle }) => {
  const { data: imageUrl } = useImage(item.smallImage || undefined);

  const selectedClass = mode === 'include' ? styles.pickTileIncluded : styles.pickTileExcluded;
  const blockedReason = mode === 'include' ? 'Річ уже у «Виключити»' : 'Річ уже у «Включити»';

  const tile = (
    <button
      type="button"
      className={`${styles.pickTile} ${selected ? selectedClass : ''}`}
      disabled={blocked}
      aria-pressed={selected}
      onClick={() => onToggle(item.id)}
    >
      {selected && (
        <span className={styles.pickMark}>
          {mode === 'include'
            ? <CheckCircleFilled style={{ color: '#52c41a' }} />
            : <StopOutlined style={{ color: '#ff4d4f' }} />}
        </span>
      )}
      <img src={imageUrl ?? noImageSmall} alt={item.name} className={styles.thumb} />
      <div className={styles.itemName} title={item.name}>{item.name}</div>
    </button>
  );

  // Вимкнена кнопка не генерує mouse-подій, тому тултіп вішаємо на обгортку
  return blocked
    ? <Tooltip title={blockedReason}><span style={{ display: 'block' }}>{tile}</span></Tooltip>
    : tile;
};

export default ItemPickerStep;
