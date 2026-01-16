import { httpClient } from "../!localHttpClient";
import type { ItemPreviewDto } from "./itemsApi";

export type ShelfsDto = {
  id: string;
  name: string;
  userId: string;
  items: ItemPreviewDto[];
};

export type CreateShelfDto = {
  userId: string;
  name: string;
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
    createShelf: (newShelf: CreateShelfDto) =>
      httpClient.post<CreateShelfDto>("/add-shelf",{newShelf}),
    updateShelf: (shelfDto: UpdateShelfDto) =>
      httpClient.put<UpdateShelfDto>("/shelfs",{shelfDto})
  };