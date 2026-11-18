import { useRef, useState, type FC } from "react";
import { Button, Input, List, Space, Tag, Typography, Upload } from "antd";
import { InboxOutlined } from "@ant-design/icons";
import { isAxiosError } from "axios";
import { referenceApi, type ReferenceBatchSummary } from "@/api/offers/referenceApi";
import { useCreateReferenceBatch } from "@/hooks/offers/useReference";

const ACCEPTED = ["image/jpeg", "image/png", "image/webp"];
// Сервер приймає до 32 МБ на gRPC, але запит з фото з телефона в межах 30 МБ з запасом
const MAX_BYTES = 30 * 1024 * 1024;
const CONCURRENCY = 4;

type EntryStatus = "ready" | "uploading" | "done" | "duplicate" | "error";

type Entry = {
  id: number;
  file: File;
  status: EntryStatus;
  message?: string;
};

type Props = {
  ownerUserId: string;
  batchId: string | undefined;
  batches: ReferenceBatchSummary[];
  onBatchChange: (id: string) => void;
};

const statusTag: Record<EntryStatus, { color: string; text: string }> = {
  ready: { color: "default", text: "Очікує" },
  uploading: { color: "processing", text: "Завантажується" },
  done: { color: "success", text: "Завантажено" },
  duplicate: { color: "blue", text: "Дубль, пропущено" },
  error: { color: "error", text: "Помилка" },
};

const uploadError = (error: unknown) => {
  if (isAxiosError(error)) {
    const data = error.response?.data as { message?: string; errors?: Record<string, string[]> } | string | undefined;
    if (typeof data === "string") return data;
    const field = data?.errors ? Object.values(data.errors).flat()[0] : undefined;
    if (field) return field;
    if (error.response?.status === 413) return "Файл завеликий.";
    if (error.response?.status === 404) return "Партію не знайдено.";
    if (!error.response) return "Немає з'єднання з сервером.";
    return data?.message ?? `Помилка сервера (код ${error.response.status}).`;
  }
  return "Невідома помилка.";
};

/** Вкладка «Завантаження»: створення партії, перетягування фото, один запит на файл, до 4 паралельно. */
const UploadTab: FC<Props> = ({ ownerUserId, batchId, batches, onBatchChange }) => {
  const [entries, setEntries] = useState<Entry[]>([]);
  const [running, setRunning] = useState(false);
  const [newName, setNewName] = useState("");
  const createBatch = useCreateReferenceBatch(ownerUserId);
  const nextId = useRef(1);

  const addFiles = (files: File[]) => {
    const added: Entry[] = files.map((file) => {
      const id = nextId.current++;
      if (!ACCEPTED.includes(file.type)) {
        return { id, file, status: "error", message: "Лише JPEG, PNG або WebP (HEIC конвертуйте)." };
      }
      if (file.size > MAX_BYTES) {
        return { id, file, status: "error", message: "Файл більший за 30 МБ." };
      }
      return { id, file, status: "ready" };
    });
    setEntries((prev) => [...prev, ...added]);
  };

  const patch = (id: number, change: Partial<Entry>) =>
    setEntries((prev) => prev.map((e) => (e.id === id ? { ...e, ...change } : e)));

  const uploadOne = async (entry: Entry) => {
    if (!batchId) return;
    patch(entry.id, { status: "uploading", message: undefined });
    try {
      const res = await referenceApi.uploadPhoto(batchId, ownerUserId, entry.file);
      patch(entry.id, res.data.duplicate
        ? { status: "duplicate", message: "Таке фото вже є в партії або завантажене раніше." }
        : { status: "done" });
    } catch (error) {
      patch(entry.id, { status: "error", message: uploadError(error) });
    }
  };

  // Пул на CONCURRENCY запитів: збій одного файла не зупиняє решту
  const runQueue = async (targets: Entry[]) => {
    setRunning(true);
    let cursor = 0;
    const worker = async () => {
      while (cursor < targets.length) {
        const entry = targets[cursor++];
        await uploadOne(entry);
      }
    };
    await Promise.all(Array.from({ length: Math.min(CONCURRENCY, targets.length) }, worker));
    setRunning(false);
  };

  const pending = entries.filter((e) => e.status === "ready");
  const failed = entries.filter((e) => e.status === "error" && e.file.size <= MAX_BYTES && ACCEPTED.includes(e.file.type));
  const counts = {
    done: entries.filter((e) => e.status === "done").length,
    duplicate: entries.filter((e) => e.status === "duplicate").length,
    error: entries.filter((e) => e.status === "error").length,
  };

  const create = () => {
    createBatch.mutate(newName.trim(), {
      onSuccess: (created) => {
        setNewName("");
        onBatchChange(created.id);
      },
    });
  };

  return (
    <Space direction="vertical" size="large" style={{ width: "100%" }}>
      <Space wrap>
        <Input
          style={{ width: 260 }}
          placeholder="Назва нової партії (необов'язково)"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          onPressEnter={create}
        />
        <Button onClick={create} loading={createBatch.isPending}>Створити партію</Button>
        {batchId && (
          <Typography.Text style={{ color: "#bbb" }}>
            Фото підуть у: {batches.find((b) => b.id === batchId)?.name ?? "—"}
          </Typography.Text>
        )}
      </Space>

      <Upload.Dragger
        multiple
        accept={ACCEPTED.join(",")}
        showUploadList={false}
        beforeUpload={(_file, fileList) => {
          // Обробляємо весь набір один раз, на першому файлі
          if (fileList[0] === _file) addFiles(fileList);
          return false;
        }}
      >
        <p className="ant-upload-drag-icon"><InboxOutlined /></p>
        <p className="ant-upload-text">Перетягніть фото сюди або натисніть, щоб обрати</p>
        <p className="ant-upload-hint">JPEG, PNG, WebP. До сотні фото за раз.</p>
      </Upload.Dragger>

      <Space wrap>
        <Button
          type="primary"
          disabled={!batchId || pending.length === 0 || running}
          loading={running}
          onClick={() => runQueue(pending)}
        >
          Завантажити ({pending.length})
        </Button>
        <Button disabled={!batchId || failed.length === 0 || running} onClick={() => runQueue(failed)}>
          Повторити невдалі ({failed.length})
        </Button>
        <Button disabled={running || entries.length === 0} onClick={() => setEntries([])}>Очистити список</Button>
        <Typography.Text>
          Завантажено: {counts.done} · дублікати: {counts.duplicate} · помилки: {counts.error}
        </Typography.Text>
      </Space>

      <List
        size="small"
        bordered
        dataSource={entries}
        locale={{ emptyText: "Файлів ще немає" }}
        renderItem={(entry) => {
          const tag = statusTag[entry.status];
          return (
            <List.Item>
              <Space wrap style={{ width: "100%", justifyContent: "space-between" }}>
                <Typography.Text>{entry.file.name}</Typography.Text>
                <Space>
                  <Tag color={tag.color}>{tag.text}</Tag>
                  {entry.message && <Typography.Text type="secondary">{entry.message}</Typography.Text>}
                </Space>
              </Space>
            </List.Item>
          );
        }}
      />
    </Space>
  );
};

export default UploadTab;
