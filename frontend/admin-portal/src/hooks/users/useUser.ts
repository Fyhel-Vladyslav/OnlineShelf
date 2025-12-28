import { useQuery } from "@tanstack/react-query";
import { userApi } from "@/api/userApi";
import type { UserDto } from "@/api/userApi";

export const useUser = (userId?: string) => {
  return useQuery<UserDto>({
    queryKey: ["user", userId],
    queryFn: async () => {
      const res = await userApi.getUserById(userId!);
      return res.data;
    },
    enabled: !!userId,
    staleTime: 1000 * 60,
  });
};