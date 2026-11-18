import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import {
  referenceApi,
  type ListPhotosParams,
  type OutfitStatus,
  type ReferenceSlot,
} from "@/api/offers/referenceApi";
import { useNotification } from "@/notification/useNotification";
import { notificationColours } from "@/notification/notificationColours";

const batchesKey = ["reference", "batches"] as const;
const photosKey = (batchId: string) => ["reference", "photos", batchId] as const;

/** Опитування кожні 4 с, поки є фото в черзі; без SignalR, як домовлено в плані. */
export const useReferenceBatches = () =>
  useQuery({
    queryKey: batchesKey,
    queryFn: async () => (await referenceApi.listBatches()).data,
    refetchInterval: (query) => {
      const batches = query.state.data ?? [];
      const busy = batches.some((b) => b.photos.queued + b.photos.processing > 0);
      return busy ? 4000 : false;
    },
  });

export const useReferencePhotos = (batchId: string | undefined, params: ListPhotosParams) =>
  useQuery({
    queryKey: [...photosKey(batchId ?? ""), params],
    queryFn: async () => (await referenceApi.listPhotos(batchId!, params)).data,
    enabled: !!batchId,
    placeholderData: (prev) => prev,
    refetchInterval: 4000,
  });

const errorText = (error: unknown) => {
  if (isAxiosError(error)) {
    const data = error.response?.data as { detail?: string; message?: string } | undefined;
    if (data?.detail) return data.detail;
    if (data?.message) return data.message;
    if (!error.response) return "Немає з'єднання з сервером.";
    return `Помилка сервера (код ${error.response.status}).`;
  }
  return "Невідома помилка.";
};

/** Хуки для мутацій: тост успіху або помилки. */
const useToast = () => {
  const { notify } = useNotification();
  return {
    ok: (text: string) => notify({ text, colour: notificationColours.Green, time: 4000 }),
    fail: (error: unknown) => notify({ text: errorText(error), colour: notificationColours.Red, time: 6000 }),
  };
};

export const useCreateReferenceBatch = (ownerUserId: string) => {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: async (name: string) => (await referenceApi.createBatch(ownerUserId, name)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: batchesKey }),
    onError: toast.fail,
  });
};

export const useApproveReady = () => {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: async (batchId: string) => (await referenceApi.approveReady(batchId)).data,
    onSuccess: (data, batchId) => {
      toast.ok(`Прийнято образів: ${data.approved}`);
      qc.invalidateQueries({ queryKey: batchesKey });
      qc.invalidateQueries({ queryKey: photosKey(batchId) });
    },
    onError: toast.fail,
  });
};

export const useRetryPhoto = (batchId: string) => {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: (photoId: string) => referenceApi.retryPhoto(photoId),
    onSuccess: () => {
      toast.ok("Фото повернуто в чергу");
      qc.invalidateQueries({ queryKey: batchesKey });
      qc.invalidateQueries({ queryKey: photosKey(batchId) });
    },
    onError: toast.fail,
  });
};

/** Правки огляду: образ, річ, видалення речі. Після кожної правки оновлюємо сітку партії. */
export const useReviewActions = (batchId: string) => {
  const qc = useQueryClient();
  const toast = useToast();
  const refresh = () => {
    qc.invalidateQueries({ queryKey: batchesKey });
    qc.invalidateQueries({ queryKey: photosKey(batchId) });
  };

  return {
    setOutfitStatus: useMutation({
      mutationFn: ({ outfitId, status }: { outfitId: string; status: OutfitStatus }) =>
        referenceApi.updateOutfitStatus(outfitId, status),
      onSuccess: refresh,
      onError: toast.fail,
    }),
    updateItem: useMutation({
      mutationFn: ({ itemId, slot, attributeType }: { itemId: string; slot?: ReferenceSlot; attributeType?: number }) =>
        referenceApi.updateItem(itemId, { slot, attributeType }),
      onSuccess: refresh,
      onError: toast.fail,
    }),
    deleteItem: useMutation({
      mutationFn: (itemId: string) => referenceApi.deleteItem(itemId),
      onSuccess: refresh,
      onError: toast.fail,
    }),
  };
};
