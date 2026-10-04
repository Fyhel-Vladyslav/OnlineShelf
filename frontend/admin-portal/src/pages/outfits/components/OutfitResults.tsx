import React from 'react';
import { Alert, Button, Card, Collapse, Empty, Space, Table, Tag, Typography } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import {
  seasonPhaseLabels,
  slotLabels,
  weatherLabels,
  type GenerateOutfitsResponse,
  type OutfitDto,
  type OutfitItemDto,
} from '@/api/offers/offersApi';
import { useImage } from '@/hooks/items/useImage';
import noImage from '@/assets/images/noPhotoLoaded.jpg';
import { formatMultiplier, formatPercent, formatRuleName, formatTemperature, weatherIcons } from '../outfitPresentation';
import styles from '../OutfitOfferPage.module.css';

const { Text } = Typography;

interface OutfitResultsProps {
  result: GenerateOutfitsResponse;
  isPending: boolean;
  onRegenerate: () => void;
}

const OutfitResults: React.FC<OutfitResultsProps> = ({ result, isPending, onRegenerate }) => {
  const { outfits, weather, seasonPhase, warnings, elapsedMs } = result;

  // Поки скорер — заглушка, усі скори однакові: не робимо вигляд, що є «кращий» варіант
  const allScoresEqual = outfits.length > 1 && outfits.every((o) => o.finalScore === outfits[0].finalScore);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Space wrap style={{ justifyContent: 'space-between', width: '100%' }}>
        <Space wrap>
          {weather
            ? <Tag>{weatherIcons[weather.condition] ?? weatherIcons[0]} {weatherLabels[weather.condition] ?? weatherLabels[0]}, {formatTemperature(weather.temperatureCelsius)}</Tag>
            : <Tag>Погода не врахована</Tag>}
          {seasonPhaseLabels[seasonPhase] && <Tag>{seasonPhaseLabels[seasonPhase]}</Tag>}
        </Space>
        <Button icon={<ReloadOutlined />} onClick={onRegenerate} loading={isPending} disabled={isPending}>
          Згенерувати ще раз
        </Button>
      </Space>

      {warnings.map((warning, index) => (
        <Alert key={index} type="warning" showIcon message={warning} />
      ))}

      {allScoresEqual && (
        <Text type="secondary">
          Усі варіанти мають однакову оцінку сумісності, тому порядок не означає, що перший кращий.
        </Text>
      )}

      {outfits.length === 0 ? (
        <Empty description="Не вдалося скласти жодного образу. Причину дивіться в попередженнях вище." />
      ) : (
        <div className={styles.outfitGrid}>
          {outfits.map((outfit, index) => (
            <OutfitCard key={index} outfit={outfit} index={index} />
          ))}
        </div>
      )}

      <Text type="secondary" style={{ fontSize: 12 }}>Згенеровано за {elapsedMs} мс</Text>
    </Space>
  );
};

const OutfitCard: React.FC<{ outfit: OutfitDto; index: number }> = ({ outfit, index }) => {
  const nameById = new Map(outfit.items.map((i) => [i.itemId, i.name]));
  const itemName = (id: string) => nameById.get(id) ?? 'Невідома річ';

  const penalties = Object.entries(outfit.appliedPenalties).map(([rule, multiplier]) => ({ rule, multiplier }));

  return (
    <Card
      title={`Варіант ${index + 1}`}
      extra={
        <Space direction="vertical" size={0} style={{ textAlign: 'right' }}>
          <span className={styles.score}>{formatPercent(outfit.finalScore)}</span>
          <Text type="secondary" style={{ fontSize: 12 }}>штрафи {formatMultiplier(outfit.penaltyMultiplier)}</Text>
        </Space>
      }
    >
      <div className={styles.outfitItems}>
        {outfit.items.map((item) => <OutfitItemView key={item.itemId} item={item} />)}
      </div>

      <Collapse
        ghost
        size="small"
        style={{ marginTop: 12 }}
        items={[{
          key: 'why',
          label: 'Чому так',
          children: (
            <Space direction="vertical" style={{ width: '100%' }}>
              <Text>
                Оцінка моделі: <Text strong>{formatPercent(outfit.graphLevelScore)}</Text>
                {' '}{formatMultiplier(outfit.penaltyMultiplier)} = <Text strong>{formatPercent(outfit.finalScore)}</Text>
              </Text>

              <Table
                size="small"
                pagination={false}
                rowKey="rule"
                dataSource={penalties}
                locale={{ emptyText: 'Правил штрафів не застосовано' }}
                columns={[
                  { title: 'Правило', dataIndex: 'rule', render: (rule: string) => <span title={rule}>{formatRuleName(rule)}</span> },
                  {
                    title: 'Множник',
                    dataIndex: 'multiplier',
                    align: 'right',
                    render: (m: number) => m < 1 ? <Text type="danger">{formatMultiplier(m)}</Text> : <Text type="secondary">{formatMultiplier(m)}</Text>,
                  },
                ]}
              />

              <Table
                size="small"
                pagination={false}
                rowKey={(p) => `${p.itemIdA}-${p.itemIdB}`}
                dataSource={outfit.pairwiseScores}
                locale={{ emptyText: 'Попарних оцінок немає' }}
                columns={[
                  { title: 'Пара речей', render: (_, p) => `${itemName(p.itemIdA)} + ${itemName(p.itemIdB)}` },
                  { title: 'Сумісність', dataIndex: 'score', align: 'right', render: (s: number) => formatPercent(s) },
                ]}
              />
            </Space>
          ),
        }]}
      />
    </Card>
  );
};

const OutfitItemView: React.FC<{ item: OutfitItemDto }> = ({ item }) => {
  const { data: imageUrl } = useImage(item.bigImage ?? undefined);

  return (
    <div className={styles.outfitItem}>
      <img src={imageUrl ?? noImage} alt={item.name} className={styles.outfitImage} />
      <Tag style={{ marginTop: 4, marginInlineEnd: 0 }}>{slotLabels[item.slot] ?? item.slot}</Tag>
      <div className={styles.itemName} title={item.name}>{item.name}</div>
      {item.isVirtual && <Tag color="gold">Порада: докупити</Tag>}
    </div>
  );
};

export default OutfitResults;
