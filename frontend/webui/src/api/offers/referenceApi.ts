import { httpClient } from "../httpClient";

// Числові значення enum з OutfitOfferService (JSON серіалізує їх числами, порядок як у C#).
export const PhotoStatus = { Queued: 0, Processing: 1, Done: 2, NoItems: 3, Failed: 4 } as const;
export type PhotoStatus = typeof PhotoStatus[keyof typeof PhotoStatus];

export const OutfitStatus = { Ready: 0, NeedsReview: 1, Approved: 2, Rejected: 3 } as const;
export type OutfitStatus = typeof OutfitStatus[keyof typeof OutfitStatus];

export const ReferenceSlot = {
  Unknown: 0, Top: 1, Bottom: 2, FullBody: 3, Shoes: 4, Outerwear: 5, Accessory: 6,
} as const;
export type ReferenceSlot = typeof ReferenceSlot[keyof typeof ReferenceSlot];

export const referenceSlotLabels: Record<number, string> = {
  1: "Верх",
  2: "Низ",
  3: "Сукня/комбінезон",
  4: "Взуття",
  5: "Верхній одяг",
  6: "Аксесуар",
};

export const outfitStatusLabels: Record<number, string> = {
  0: "Готовий",
  1: "Потребує перегляду",
  2: "Прийнято",
  3: "Відхилено",
};

export const photoStatusLabels: Record<number, string> = {
  0: "У черзі",
  1: "Обробляється",
  2: "Готово",
  3: "Без речей",
  4: "Помилка",
};

export type PhotoStatusCounts = { queued: number; processing: number; done: number; noItems: number; failed: number };
export type OutfitStatusCounts = { ready: number; needsReview: number; approved: number; rejected: number };

export type ReferenceBatchSummary = {
  id: string;
  name: string;
  createdAt: string;
  photos: PhotoStatusCounts;
  outfits: OutfitStatusCounts;
};

export type ReferenceItem = {
  id: string;
  slot: ReferenceSlot;
  attributeType: number | null;
  attributeSeason: number | null;
  attributePattern: number | null;
  attributeMaterial: number | null;
  colorMain: string;
  colorSecond: string;
  confidence: number;
  boxX1: number;
  boxY1: number;
  boxX2: number;
  boxY2: number;
};

export type ReferencePhoto = {
  id: string;
  fileName: string;
  bigImage: string;
  width: number | null;
  height: number | null;
  status: PhotoStatus;
  lastError: string | null;
  outfitId: string | null;
  outfitStatus: OutfitStatus | null;
  items: ReferenceItem[];
};

export type ReferencePhotoPage = { total: number; page: number; pageSize: number; items: ReferencePhoto[] };

export type UploadPhotoResponse = { photoId: string; status: string; duplicate: boolean };

export type ListPhotosParams = {
  outfitStatus?: keyof typeof OutfitStatus;
  status?: keyof typeof PhotoStatus;
  page?: number;
  pageSize?: number;
};

export const referenceApi = {
  createBatch: (ownerUserId: string, name: string) =>
    httpClient.post<{ id: string; name: string; createdAt: string }>(
      "/offers/reference/batches",
      { ownerUserId, name },
      { headers: { "Content-Type": "application/json" } },
    ),

  listBatches: () => httpClient.get<ReferenceBatchSummary[]>("/offers/reference/batches"),

  uploadPhoto: (batchId: string, ownerUserId: string, file: File) => {
    const form = new FormData();
    form.append("ownerUserId", ownerUserId);
    form.append("file", file);
    return httpClient.post<UploadPhotoResponse>(`/offers/reference/batches/${batchId}/photos`, form);
  },

  listPhotos: (batchId: string, params: ListPhotosParams) =>
    httpClient.get<ReferencePhotoPage>(`/offers/reference/batches/${batchId}/photos`, { params }),

  approveReady: (batchId: string) =>
    httpClient.post<{ approved: number }>(
      `/offers/reference/batches/${batchId}/approve-ready`,
      {},
      { headers: { "Content-Type": "application/json" } },
    ),

  retryPhoto: (photoId: string) =>
    httpClient.post(`/offers/reference/photos/${photoId}/retry`, {}, { headers: { "Content-Type": "application/json" } }),

  updateOutfitStatus: (outfitId: string, status: OutfitStatus) =>
    httpClient.patch(`/offers/reference/outfits/${outfitId}`, { status }, { headers: { "Content-Type": "application/json" } }),

  updateItem: (itemId: string, body: { slot?: ReferenceSlot; attributeType?: number }) =>
    httpClient.patch<ReferenceItem>(`/offers/reference/items/${itemId}`, body, { headers: { "Content-Type": "application/json" } }),

  deleteItem: (itemId: string) => httpClient.delete(`/offers/reference/items/${itemId}`),
};

/** Фото зберігає ImageService; шлях через Gateway, як у решти зображень. */
export const referenceImageUrl = (bigImage: string) =>
  `${httpClient.defaults.baseURL}/shelfs/image/get-image/${bigImage}`;
