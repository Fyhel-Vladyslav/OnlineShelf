import { useQuery } from "@tanstack/react-query";
import { shelfsApi} from "@/api/shelfs/shelfsApi.ts";
import type { ShelfsDto } from "@/api/shelfs/shelfsApi.ts";

export const useShelfs = () => {
  return useQuery<ShelfsDto[]>({
    queryKey: ["shelfs"],
    queryFn: async () => {
      const res = await shelfsApi.getShelfs();
      return res.data.shelfs; // match backend shape
    },
    staleTime: 1000 * 60, // cache 1 minute
    retry: 1, // retry once on failure
  });
};
