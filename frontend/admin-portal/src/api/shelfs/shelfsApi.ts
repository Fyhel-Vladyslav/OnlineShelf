import { httpClient } from "../!localHttpClient";
import type { ItemPreviewDto } from "./itemsApi";

export type ShelfsDto = {
  id: string;
  name: string;
  items: ItemPreviewDto[];
};

export type CreateShelfDto = {
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
      httpClient.post<CreateShelfDto>("/shelfs/add-shelf",{newShelf}),
        
    deleteShelf: (shelfId: string) =>
      httpClient.delete<string>(`/shelfs/${shelfId}`),
        
    updateShelf: (shelfDto: UpdateShelfDto) =>
      httpClient.put<UpdateShelfDto>("/shelfs",{shelfDto})
  };