import { useMutation, useQueryClient } from "@tanstack/react-query";
import { itemsApi } from "@/api/shelfs/itemsApi";
import type { CreateItemDto } from "@/api/shelfs/itemsApi";

export const useCreateItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (item: CreateItemDto) => itemsApi.createItem(item),
    
    onSuccess: () => {
      // Invalidate and refetch items queries
      queryClient.invalidateQueries({ queryKey: ["items"] });
      queryClient.invalidateQueries({ queryKey: ["shelfs"] });
    },
    onError: (error, variables) => {
        console.error("Creation failed for:", variables);
        console.log(error.message);
      },
  });
};
