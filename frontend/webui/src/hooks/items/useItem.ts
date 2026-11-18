import { useQuery } from "@tanstack/react-query";
import { itemsApi} from "@/api/shelfs/itemsApi.ts";
import type { ItemDto } from "@/api/shelfs/itemsApi";

export const useItem = (itemId?: string) => {
  return useQuery<ItemDto>({
    queryKey: ["items", itemId],
    queryFn: async () => {
      const res = await itemsApi.getItemById(itemId!);
      return res.data;
    },
    staleTime: 1000 * 60,
    enabled: !!itemId, // Only run query when itemId is defined
  });
};
