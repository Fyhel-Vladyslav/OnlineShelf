import { useMutation, useQueryClient } from "@tanstack/react-query";
import { userApi } from "@/api/users/userApi";
import type { UserDto } from '@/api/users/userApi';
import { message } from "antd";

export const useUpdateUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: UserDto) => userApi.UpdateUser(payload),

    onSuccess: (data, variables) => {
      // variables is your 'payload'
      console.log("Updated user with data:", variables);
      message.success(data.status);
      queryClient.invalidateQueries({ queryKey: ["user"] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },

    onError: (error, variables) => {
      // Log the request data that caused the error
      console.error("Update failed for:", variables);
      message.error(error.message);
      // If you want to show it in the UI (careful with sensitive data)
      message.info(JSON.stringify(variables)); 
    },
  });
};