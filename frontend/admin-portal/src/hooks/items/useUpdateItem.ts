import { useMutation, useQueryClient } from "@tanstack/react-query";
import { itemsApi } from "@/api/shelfs/itemsApi";
import type { UpdateItemDto } from "@/api/shelfs/itemsApi";

export const useUpdateItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (item: UpdateItemDto) => itemsApi.updateItem(item),
    
    onSuccess: () => {
      // Invalidate and refetch items queries
      queryClient.invalidateQueries({ queryKey: ["items"] });
      queryClient.invalidateQueries({ queryKey: ["shelfs"] });
    },
    onError: (error, variables) => {
        console.error("Update failed for:", variables);
      },
  });
};
