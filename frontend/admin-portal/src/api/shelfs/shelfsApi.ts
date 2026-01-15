import { httpClient } from "../!localHttpClient";
import type { ItemPreviewDto } from "./itemsApi";

export type ShelfsDto = {
  id: string;
  name: string;
  userId: string;
  items: ItemPreviewDto[];
};

export type UpdateShelfDto = {
  id: string;
  name: string;
};

export type GetShelfsResponse = {
  shelfs: ShelfsDto[];
};
export const shelfsApi = {
    getShelfs: () =>
      httpClient.get<GetShelfsResponse>("/shelfs"),
    updateShelf: (shelfDto: UpdateShelfDto) =>
      httpClient.put<UpdateShelfDto>("/shelfs",{shelfDto})
  };