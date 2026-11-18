import type { FC } from "react";
import { Button, Popconfirm, Space, Table, Tag, Typography } from "antd";
import type { ColumnsType } from "antd/es/table";
import type { ReferenceBatchSummary } from "@/api/offers/referenceApi";
import { useApproveReady } from "@/hooks/offers/useReference";

type Props = {
  batches: ReferenceBatchSummary[];
  onOpenReview: (batchId: string) => void;
};

const formatDate = (iso: string) => new Date(iso).toLocaleString("uk-UA", { dateStyle: "short", timeStyle: "short" });

/** Вкладка «Партії»: лічильники обробки і рішень, перехід до огляду, прийняття всіх готових. */
const BatchesTab: FC<Props> = ({ batches, onOpenReview }) => {
  const approveReady = useApproveReady();

  const columns: ColumnsType<ReferenceBatchSummary> = [
    { title: "Партія", dataIndex: "name", key: "name" },
    { title: "Створено", dataIndex: "createdAt", key: "createdAt", render: formatDate, width: 160 },
    {
      title: "Фото",
      key: "photos",
      render: (_, b) => (
        <Space size={4} wrap>
          <Tag>У черзі: {b.photos.queued + b.photos.processing}</Tag>
          <Tag color="success">Готово: {b.photos.done}</Tag>
          <Tag>Без речей: {b.photos.noItems}</Tag>
          <Tag color={b.photos.failed > 0 ? "error" : "default"}>Помилки: {b.photos.failed}</Tag>
        </Space>
      ),
    },
    {
      title: "Образи",
      key: "outfits",
      render: (_, b) => (
        <Space size={4} wrap>
          <Tag color="cyan">Готові: {b.outfits.ready}</Tag>
          <Tag color="orange">Перегляд: {b.outfits.needsReview}</Tag>
          <Tag color="success">Прийнято: {b.outfits.approved}</Tag>
          <Tag color="red">Відхилено: {b.outfits.rejected}</Tag>
        </Space>
      ),
    },
    {
      title: "",
      key: "actions",
      width: 280,
      render: (_, b) => (
        <Space>
          <Button size="small" onClick={() => onOpenReview(b.id)}>Огляд</Button>
          <Popconfirm
            title={`Прийняти всі готові образи (${b.outfits.ready})?`}
            okText="Прийняти"
            cancelText="Ні"
            disabled={b.outfits.ready === 0}
            onConfirm={() => approveReady.mutate(b.id)}
          >
            <Button size="small" type="primary" disabled={b.outfits.ready === 0} loading={approveReady.isPending && approveReady.variables === b.id}>
              Прийняти всі
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <Space direction="vertical" style={{ width: "100%" }}>
      <Typography.Text type="secondary">
        Таблиця оновлюється сама, поки в черзі є фото.
      </Typography.Text>
      <Table
        rowKey="id"
        size="middle"
        columns={columns}
        dataSource={batches}
        pagination={{ pageSize: 10, hideOnSinglePage: true }}
        locale={{ emptyText: "Партій ще немає" }}
      />
    </Space>
  );
};

export default BatchesTab;
