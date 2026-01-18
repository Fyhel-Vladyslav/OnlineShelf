import { useMutation, useQueryClient } from "@tanstack/react-query";
import { itemsApi } from "@/api/shelfs/itemsApi";

export const useDeleteItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (itemId: string) => itemsApi.deleteItem(itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({queryKey: ["shelfs"]});
    },
  });
};
