import { useQuery } from "@tanstack/react-query";
import { rolesApi}from "@/api/users/rolesApi";
import type { RoleDto } from "@/api/users/rolesApi";

export const useRolesList = () => {
  return useQuery<RoleDto[]>({
    queryKey: ["roles"],
    queryFn: async () => {
      const res = await rolesApi.getRolesList();
      return res.data.roles; // match backend shape
    },
    staleTime: 1000 * 60, // cache 1 minute
    retry: 1, // retry once on failure
  });
};