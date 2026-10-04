import { useMutation, useQueryClient } from "@tanstack/react-query";
import { shelfsApi} from "@/api/shelfs/shelfsApi.ts";

export const useDeleteShelf = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (shelfId: string) => shelfsApi.deleteShelf(shelfId),
    onSuccess: () => {
      queryClient.invalidateQueries({queryKey: ["shelfs"]});
    },
  });
};
