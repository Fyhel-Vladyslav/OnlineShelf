import { useMutation, useQueryClient } from "@tanstack/react-query";
import { userApi } from "@/api/userApi";

export const useUpdateUser = () => {
    const queryClient = useQueryClient();
  
    // return useMutation({
    //   mutationFn: ({ id, payload }: UpdateUserCommand) =>
    //     userApi.updateUser(id, payload),
  
    //   onSuccess: () => {
    //     message.success("User updated");
    //     queryClient.invalidateQueries({ queryKey: ["users"] });
    //     queryClient.invalidateQueries({ queryKey: ["user"] });
    //   },
  
    //   onError: () => {
    //     message.error("Update failed");
    //   },
    // });
  };