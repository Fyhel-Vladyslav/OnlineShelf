import { useMemo, useState, type FC } from "react";
import { Alert, Select, Space, Tabs, Typography } from "antd";
import { authService } from "@/hooks/jwtauth/AuthService";
import { useReferenceBatches } from "@/hooks/offers/useReference";
import UploadTab from "./UploadTab";
import BatchesTab from "./BatchesTab";
import ReviewTab from "./ReviewTab";

/** Сторінка дизайнера: завантаження еталонних фото, стан партій і огляд образів. */
const DesignerPage: FC = () => {
  const ownerUserId = authService.getUserId();
  const { data: batches = [], isLoading } = useReferenceBatches();
  const [selectedBatchId, setSelectedBatchId] = useState<string | undefined>();
  const [activeTab, setActiveTab] = useState("upload");

  // Без вибору користувача беремо найновішу партію
  const batchId = selectedBatchId ?? batches[0]?.id;
  const selectedBatch = useMemo(() => batches.find((b) => b.id === batchId), [batches, batchId]);

  if (!ownerUserId) {
    return <Alert type="warning" showIcon message="Немає ідентифікатора користувача. Увійдіть у систему." />;
  }

  const openReview = (id: string) => {
    setSelectedBatchId(id);
    setActiveTab("review");
  };

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto", padding: "16px" }}>
      <Typography.Title level={3} style={{ color: "white" }}>Еталонні образи</Typography.Title>
      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        tabBarExtraContent={
          <Space>
            <Typography.Text style={{ color: "#bbb" }}>Партія:</Typography.Text>
            <Select
              style={{ minWidth: 240 }}
              loading={isLoading}
              placeholder="Оберіть партію"
              value={batchId}
              onChange={setSelectedBatchId}
              options={batches.map((b) => ({ value: b.id, label: b.name }))}
            />
          </Space>
        }
        items={[
          {
            key: "upload",
            label: "Завантаження",
            children: <UploadTab ownerUserId={ownerUserId} batchId={batchId} batches={batches} onBatchChange={setSelectedBatchId} />,
          },
          {
            key: "batches",
            label: "Партії",
            children: <BatchesTab batches={batches} onOpenReview={openReview} />,
          },
          {
            key: "review",
            label: "Огляд",
            children: batchId
              ? <ReviewTab batchId={batchId} batchName={selectedBatch?.name ?? ""} readyCount={selectedBatch?.outfits.ready ?? 0} />
              : <Alert type="info" showIcon message="Спершу створіть партію у вкладці «Завантаження»." />,
          },
        ]}
      />
    </div>
  );
};

export default DesignerPage;
