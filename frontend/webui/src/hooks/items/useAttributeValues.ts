import { useQuery } from "@tanstack/react-query";
import { itemsApi } from "@/api/shelfs/itemsApi";
import type { AttributeValue } from "@/api/shelfs/itemsApi";

export const useAttributeValues = () => {
  return useQuery({
    queryKey: ["attributeValues"],
    queryFn: async () => {
      const response = await itemsApi.getAttributeValues();
      return response.data.attributesValues as AttributeValue[];
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

