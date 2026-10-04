import { useMutation, useQueryClient } from "@tanstack/react-query";
import { itemsApi, type MoveItemParams } from "@/api/shelfs/itemsApi";

// You can define a local type or import it if you created a Dto for it

export const useMoveItem = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ itemId, newShelfId }: MoveItemParams) => 
      itemsApi.moveItem(itemId, newShelfId),
    
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["items"] });
      queryClient.invalidateQueries({ queryKey: ["shelfs"] });
    },
    
    onError: (error, variables) => {
      console.error("Move failed for Item:", variables.itemId);
      console.log(error.message);
    },
  });
};
