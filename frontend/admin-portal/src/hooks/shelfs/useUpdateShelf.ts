import { useMutation, useQueryClient } from "@tanstack/react-query";
import { shelfsApi } from "@/api/shelfs/shelfsApi";
import type { UpdateShelfDto } from "@/api/shelfs/shelfsApi";

export const useUpdateShelf = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (item: UpdateShelfDto) => shelfsApi.updateShelf(item),
    
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["shelfs"] });
    },
    onError: (error, variables) => {
        console.error("Update failed for:", variables);
      },
  });
};
