import { useQuery } from "@tanstack/react-query";
import { userApi}from "@/api/users/userApi";
import type { UserDto } from "@/api/users/userApi";

export const useUsers = () => {
  return useQuery<UserDto[]>({
    queryKey: ["users"],
    queryFn: async () => {
      const res = await userApi.getUsers();
      return res.data.users; // match backend shape
    },
    staleTime: 1000 * 60, // cache 1 minute
    retry: 1, // retry once on failure
  });
};