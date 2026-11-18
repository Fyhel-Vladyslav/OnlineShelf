import { useState, type FC } from "react";
import { Alert, Button, Card, Col, Empty, Pagination, Popconfirm, Row, Segmented, Select, Space, Tag, Typography } from "antd";
import { DeleteOutlined, ReloadOutlined } from "@ant-design/icons";
import {
  OutfitStatus,
  outfitStatusLabels,
  PhotoStatus,
  photoStatusLabels,
  referenceImageUrl,
  referenceSlotLabels,
  type ListPhotosParams,
  type ReferenceItem,
  type ReferencePhoto,
} from "@/api/offers/referenceApi";
import { useAttributeValues } from "@/hooks/items/useAttributeValues";
import {
  useApproveReady,
  useReferencePhotos,
  useReviewActions,
  useRetryPhoto,
} from "@/hooks/offers/useReference";

type Props = {
  batchId: string;
  batchName: string;
  readyCount: number;
};

type Filter = "all" | "Ready" | "NeedsReview" | "Approved" | "Rejected" | "Failed";

const PAGE_SIZE = 24;

const filterParams = (filter: Filter): Omit<ListPhotosParams, "page" | "pageSize"> => {
  if (filter === "Failed") return { status: "Failed" };
  if (filter === "all") return {};
  return { outfitStatus: filter };
};

const slotOptions = Object.entries(referenceSlotLabels).map(([value, label]) => ({ value: Number(value), label }));

const outfitTag: Record<number, { color: string }> = {
  [OutfitStatus.Ready]: { color: "cyan" },
  [OutfitStatus.NeedsReview]: { color: "orange" },
  [OutfitStatus.Approved]: { color: "success" },
  [OutfitStatus.Rejected]: { color: "red" },
};

/** Вкладка «Огляд»: сітка фото з рамками, фільтри, правка речей і рішення про образ. */
const ReviewTab: FC<Props> = ({ batchId, batchName, readyCount }) => {
  const [filter, setFilter] = useState<Filter>("NeedsReview");
  const [page, setPage] = useState(1);
  const { data, isLoading } = useReferencePhotos(batchId, { ...filterParams(filter), page, pageSize: PAGE_SIZE });
  const approveReady = useApproveReady();
  const { data: attributes } = useAttributeValues();
  const typeOptions = (attributes?.find((a) => a.typeName === "Type")?.options ?? []).map((o) => ({ value: o.key, label: o.value }));
  const typeLabel = (key: number | null) => typeOptions.find((o) => o.value === key)?.label;

  const changeFilter = (value: Filter) => {
    setFilter(value);
    setPage(1);
  };

  return (
    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
      <Space wrap style={{ justifyContent: "space-between", width: "100%" }}>
        <Segmented<Filter>
          value={filter}
          onChange={changeFilter}
          options={[
            { value: "NeedsReview", label: "Перегляд" },
            { value: "Ready", label: "Готові" },
            { value: "Approved", label: "Прийнято" },
            { value: "Rejected", label: "Відхилено" },
            { value: "Failed", label: "Помилки" },
            { value: "all", label: "Усі" },
          ]}
        />
        <Popconfirm
          title={`Прийняти всі готові образи партії «${batchName}» (${readyCount})?`}
          okText="Прийняти"
          cancelText="Ні"
          disabled={readyCount === 0}
          onConfirm={() => approveReady.mutate(batchId)}
        >
          <Button type="primary" disabled={readyCount === 0} loading={approveReady.isPending}>
            Прийняти всі готові ({readyCount})
          </Button>
        </Popconfirm>
      </Space>

      {isLoading && !data && <Typography.Text type="secondary">Завантаження…</Typography.Text>}
      {data && data.items.length === 0 && (
        <Empty description={filter === "NeedsReview" ? "Немає образів на перегляді" : "Порожньо"} />
      )}

      <Row gutter={[16, 16]}>
        {data?.items.map((photo) => (
          <Col key={photo.id} xs={24} sm={12} lg={8}>
            <PhotoCard photo={photo} batchId={batchId} typeLabel={typeLabel} typeOptions={typeOptions} />
          </Col>
        ))}
      </Row>

      {data && data.total > PAGE_SIZE && (
        <Pagination current={page} pageSize={PAGE_SIZE} total={data.total} onChange={setPage} showSizeChanger={false} />
      )}
    </Space>
  );
};

type CardProps = {
  photo: ReferencePhoto;
  batchId: string;
  typeLabel: (key: number | null) => string | undefined;
  typeOptions: { value: number; label: string }[];
};

/** Картка фото: рамки речей поверх зображення і дії з образом. */
const PhotoCard: FC<CardProps> = ({ photo, batchId, typeLabel, typeOptions }) => {
  const actions = useReviewActions(batchId);
  const retry = useRetryPhoto(batchId);
  const busy = actions.setOutfitStatus.isPending || actions.updateItem.isPending || actions.deleteItem.isPending;
  const hasBoxes = photo.width != null && photo.height != null;

  const decide = (status: OutfitStatus) => {
    if (photo.outfitId) actions.setOutfitStatus.mutate({ outfitId: photo.outfitId, status });
  };

  return (
    <Card size="small" title={<Typography.Text ellipsis style={{ maxWidth: 220 }}>{photo.fileName}</Typography.Text>}
      extra={photo.outfitStatus != null
        ? <Tag color={outfitTag[photo.outfitStatus]?.color}>{outfitStatusLabels[photo.outfitStatus]}</Tag>
        : <Tag>{photoStatusLabels[photo.status]}</Tag>}>
      {photo.status === PhotoStatus.Failed ? (
        <Alert
          type="error"
          showIcon
          message="Фото не оброблено"
          description={photo.lastError ?? "Невідома помилка"}
          action={
            <Button size="small" icon={<ReloadOutlined />} loading={retry.isPending} onClick={() => retry.mutate(photo.id)}>
              Повторити
            </Button>
          }
        />
      ) : (
        <>
          <div style={{ position: "relative", width: "100%", background: "#111", borderRadius: 4, overflow: "hidden" }}>
            <img
              src={referenceImageUrl(photo.bigImage)}
              alt={photo.fileName}
              style={{ display: "block", width: "100%", height: "auto" }}
              loading="lazy"
            />
            {hasBoxes && photo.items.map((item) => (
              <BoxOverlay key={item.id} item={item} width={photo.width!} height={photo.height!} label={referenceSlotLabels[item.slot]} />
            ))}
          </div>

          <Space direction="vertical" size={6} style={{ width: "100%", marginTop: 8 }}>
            {photo.items.length === 0 && <Typography.Text type="secondary">Речей не знайдено</Typography.Text>}
            {photo.items.map((item) => (
              <Space key={item.id} wrap size={4}>
                <Select
                  size="small"
                  style={{ width: 150 }}
                  value={item.slot}
                  options={slotOptions}
                  disabled={busy}
                  onChange={(slot) => actions.updateItem.mutate({ itemId: item.id, slot })}
                />
                <Select
                  size="small"
                  style={{ width: 170 }}
                  placeholder={typeLabel(item.attributeType) ?? "Тип"}
                  value={item.attributeType ?? undefined}
                  options={typeOptions}
                  disabled={busy}
                  showSearch
                  optionFilterProp="label"
                  onChange={(attributeType) => actions.updateItem.mutate({ itemId: item.id, attributeType })}
                />
                <Popconfirm title="Прибрати річ з образу?" okText="Так" cancelText="Ні" onConfirm={() => actions.deleteItem.mutate(item.id)}>
                  <Button size="small" danger icon={<DeleteOutlined />} disabled={busy} />
                </Popconfirm>
              </Space>
            ))}
          </Space>

          {photo.outfitId && (
            <Space wrap style={{ marginTop: 10 }}>
              <Button size="small" type="primary" disabled={busy || photo.outfitStatus === OutfitStatus.Approved}
                onClick={() => decide(OutfitStatus.Approved)}>Прийняти</Button>
              <Button size="small" danger disabled={busy || photo.outfitStatus === OutfitStatus.Rejected}
                onClick={() => decide(OutfitStatus.Rejected)}>Відхилити</Button>
              <Button size="small" disabled={busy || photo.outfitStatus === OutfitStatus.NeedsReview}
                onClick={() => decide(OutfitStatus.NeedsReview)}>На перегляд</Button>
            </Space>
          )}
        </>
      )}
    </Card>
  );
};

/** Рамка речі в відсотках від розмірів фото, тож масштабується разом із зображенням. */
const BoxOverlay: FC<{ item: ReferenceItem; width: number; height: number; label?: string }> = ({ item, width, height, label }) => (
  <div
    title={label}
    style={{
      position: "absolute",
      left: `${(item.boxX1 / width) * 100}%`,
      top: `${(item.boxY1 / height) * 100}%`,
      width: `${((item.boxX2 - item.boxX1) / width) * 100}%`,
      height: `${((item.boxY2 - item.boxY1) / height) * 100}%`,
      border: "2px solid #1677ff",
      boxSizing: "border-box",
      pointerEvents: "none",
    }}
  >
    <span style={{ position: "absolute", top: 0, left: 0, background: "#1677ff", color: "white", fontSize: 11, padding: "0 4px" }}>
      {label}
    </span>
  </div>
);

export default ReviewTab;
