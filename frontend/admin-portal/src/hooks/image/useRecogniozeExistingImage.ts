import { useQuery } from "@tanstack/react-query";
import { itemsApi } from "@/api/shelfs/itemsApi.ts";
import type { ClothingAttributes, RecognizeImageResponse } from "@/api/shelfs/itemsApi.ts";

type RecognizePayload = RecognizeImageResponse & {
  responseData?: unknown;
  res1?: unknown;
  result?: unknown;
};

type RecognizeAttributePayload = Partial<ClothingAttributes> & {
  attribute_color_main?: string;
  attribute_color_second?: string;
  attribute_type?: number;
  attribute_season?: number;
  attribute_pattern?: number;
  attribute_material?: number;
};

const parseRecognizePayload = (responseData: unknown): unknown => {
  if (typeof responseData !== "string") return responseData;

  try {
    return JSON.parse(responseData);
  } catch {
    return undefined;
  }
};

const isRecognizePayload = (value: unknown): value is RecognizePayload => {
  return typeof value === "object" && value !== null;
};

const isRecognizeAttributePayload = (value: unknown): value is RecognizeAttributePayload => {
  return typeof value === "object" && value !== null;
};

export class RecognizedImageAttributes implements ClothingAttributes {
  attributeColorMain?: string;
  attributeColorSecond?: string;
  attributeType?: number;
  attributeSeason?: number;
  attributePattern?: number;
  attributeMaterial?: number;
  confidence?: number;

  private constructor(attributes: RecognizeAttributePayload) {
    this.attributeColorMain = attributes.attributeColorMain ?? attributes.attribute_color_main;
    this.attributeColorSecond = attributes.attributeColorSecond ?? attributes.attribute_color_second;
    this.attributeType = attributes.attributeType ?? attributes.attribute_type;
    this.attributeSeason = attributes.attributeSeason ?? attributes.attribute_season;
    this.attributePattern = attributes.attributePattern ?? attributes.attribute_pattern;
    this.attributeMaterial = attributes.attributeMaterial ?? attributes.attribute_material;
    this.confidence = attributes.confidence;
  }

  static fromResponseData(responseData: unknown): RecognizedImageAttributes | null {
    const parsedResponse = parseRecognizePayload(responseData);
    if (!isRecognizePayload(parsedResponse)) return null;

    const payload =
      parsedResponse.responseData ??
      parsedResponse.res1 ??
      parsedResponse.result ??
      parsedResponse;

    const parsedPayload = parseRecognizePayload(payload);
    if (!isRecognizePayload(parsedPayload) || !parsedPayload.success) return null;
    if (!isRecognizeAttributePayload(parsedPayload.attributes)) return null;

    return new RecognizedImageAttributes(parsedPayload.attributes);
  }
}

export const useRecogniozeExistingImage = (imageName?: string) => {
  return useQuery<RecognizedImageAttributes | null>({
    queryKey: ["recognizeImageByName", imageName],
    queryFn: async () => {
      const response = await itemsApi.recognizeImageByName(imageName!);

      return RecognizedImageAttributes.fromResponseData(response.data);
    },
    enabled: false,
  });
};
