import { useMutation, useQueryClient } from "@tanstack/react-query";
import { shelfsApi } from "@/api/shelfs/shelfsApi";
import type { CreateShelfDto } from "@/api/shelfs/shelfsApi";

export const useCreateShelf = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (shelf: CreateShelfDto) => shelfsApi.createShelf(shelf),
    
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["shelfs"] });
    },
    onError: (error, variables) => {
        console.error("Update failed for:", variables);
        console.error(error.message);
      },
  });
};
