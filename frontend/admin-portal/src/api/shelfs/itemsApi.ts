import { httpClient } from "../!localHttpClient";

export type UpdateItemDto = {
    id: string;
    name: string;
    shelfId: string;
    bigImage?: string;
    smallImage?: string;
  }; 

  export type ItemPreviewDto = {
    id: string;
    name: string;
    smallImage: string;
}

export type ItemDto = {
  id: string;
  name: string;
  shelfId: string;
  bigImage: string;
  tags: string[]
};


export const itemsApi = {
    getItemById: (itemId: string) =>
      httpClient.get<ItemDto>(`/shelfs/items/${itemId}`),

    updateItem: (newItem: UpdateItemDto) =>
      httpClient.put<UpdateItemDto>("/shelfs/items", {
        newItem,
      }),

    deleteItem: (itemId: string) =>
      httpClient.delete<string>(`/shelfs/items/${itemId}`),
  };
