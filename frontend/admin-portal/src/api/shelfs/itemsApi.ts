import { httpClient } from "../!localHttpClient";

export type AttributeOption = {
  key: number;
  value: string;
};

export type AttributeValue = {
  attributeId: number;
  typeName: string;
  options: AttributeOption[];
};

export type AttributesValuesResponse = {
  attributesValues: AttributeValue[];
};

export type UpdateItemDto = {
    id: string;
    name: string;
    shelfId: string;
    bigImage?: File;
    attributeColorMain?: string;
    attributeColorSecond?: string;
    attributeType?: number;
    attributeSeason?: number;
    attributePattern?: number;
    attributeMatterial?: number;
    isFavorite?: boolean;
  }; 

  export type CreateItemDto = {
    name: string;
    shelfId: string;
    bigImage?: File;
    attributeColorMain?: string;
    attributeColorSecond?: string;
    attributeType?: number;
    attributeSeason?: number;
    attributePattern?: number;
    attributeMatterial?: number;
    isFavorite?: boolean;
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
  attributeColorMain?: string;
  attributeColorSecond?: string;
  attributeType?: number;
  attributeSeason?: number;
  attributePattern?: number;
  attributeMatterial?: number;
  isFavorite?: boolean;
};
export type MoveItemParams = {
  itemId: string;
  newShelfId: string;
};

export const itemsApi = {
    getItemById: (itemId: string) =>
      httpClient.get<ItemDto>(`/shelfs/items/${itemId}`),

    getAttributeValues: () =>
      httpClient.get<AttributesValuesResponse>("/shelfs/items/attributes"),

    updateItem: async (newItem: UpdateItemDto) =>{
        // 1. СТВОРЮЄМО FORM DATA
        const formData = new FormData();
            
        // 2. ПАКУЄМО ВСІ ПОЛЯ
        Object.entries(newItem).forEach(([key, value]) => {
          if (value !== undefined && value !== null) {
            
            // Якщо це файл - додаємо як файл
            if (typeof value === 'object' && (value as any).size !== undefined) {
              formData.append(key, value as File);
            } 
            // Якщо це чекбокс isFavorite - передаємо рядок 'true', якщо він увімкнений
            else if (typeof value === 'boolean') {
              if (value === true) formData.append(key, 'true');
            } 
            // Всі інші поля (тексти, числа) перетворюємо на рядки
            else {
              formData.append(key, String(value));
            }
          }
        });

        // 3. ВІДПРАВЛЯЄМО САМЕ formData, А НЕ { newItem }
        // ВАЖЛИВО: httpClient (axios) сам зрозуміє, що це FormData і поставить правильні заголовки!
        return await httpClient.put<UpdateItemDto>("/shelfs/items",formData      )
    },

      createItem: async (newItem: CreateItemDto) => {
        // 1. СТВОРЮЄМО FORM DATA
        const formData = new FormData();
    
        // 2. ПАКУЄМО ВСІ ПОЛЯ
        Object.entries(newItem).forEach(([key, value]) => {
          if (value !== undefined && value !== null) {
            
            // Якщо це файл - додаємо як файл
            if (typeof value === 'object' && (value as any).size !== undefined) {
              formData.append(key, value as File);
            } 
            // Якщо це чекбокс isFavorite - передаємо рядок 'true', якщо він увімкнений
            else if (typeof value === 'boolean') {
              if (value === true) formData.append(key, 'true');
            } 
            // Всі інші поля (тексти, числа) перетворюємо на рядки
            else {
              formData.append(key, String(value));
            }
          }
        });
    
        // 3. ВІДПРАВЛЯЄМО САМЕ formData, А НЕ { newItem }
        // ВАЖЛИВО: httpClient (axios) сам зрозуміє, що це FormData і поставить правильні заголовки!
        return await httpClient.post("/shelfs/items/add-item", formData);
      },

    deleteItem: (itemId: string) =>
      httpClient.delete<string>(`/shelfs/items/${itemId}`),

    moveItem: (itemId: string, newShelfId: string) => 
      httpClient.post<void>("/shelfs/move-item", {
          itemId,      
          newShelfId
      }),
    getImageByName: (imageName: string) => 
      httpClient.get(`/shelfs/items/get-image/${imageName}`, { responseType: 'blob' }),
  };
