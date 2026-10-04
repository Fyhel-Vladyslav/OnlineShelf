import { useQuery } from "@tanstack/react-query";
import { itemsApi} from "@/api/shelfs/itemsApi.ts";

export const useImage = (imageName?: string) => {
  return useQuery({
    queryKey: ["imageName", imageName],
    queryFn: async () => {
      const res = await itemsApi.getImageByName(imageName!);
      return URL.createObjectURL(res.data);
    },
    staleTime: 1000 * 60,
    enabled: !!imageName, // Only run query when itemId is defined
  });
};
