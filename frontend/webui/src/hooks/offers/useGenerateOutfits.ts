import { useMutation } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { offersApi } from "@/api/offers/offersApi";
import type {
  GenerateOutfitsRequest,
  GenerateOutfitsResponse,
  ProblemDetailsResponse,
  ValidationErrorResponse,
} from "@/api/offers/offersApi";
import { useNotification } from "@/notification/useNotification";
import { notificationColours } from "@/notification/notificationColours";

/** Витягує зрозумілі тексти з 400 (FastEndpoints) або 503 (ProblemDetails). */
export const getGenerateErrorMessages = (error: unknown): string[] => {
  if (!isAxiosError(error)) return ["Не вдалося згенерувати образи."];

  const status = error.response?.status;
  const data = error.response?.data as Partial<ValidationErrorResponse & ProblemDetailsResponse> | undefined;

  if (status === 400 && data?.errors) {
    // generalErrors першими, потім помилки окремих полів
    const { generalErrors = [], ...fieldErrors } = data.errors;
    const messages = [...generalErrors, ...Object.values(fieldErrors).flat()];
    if (messages.length > 0) return messages;
  }

  if (status === 503) {
    return [data?.detail ?? "Сервіс підбору образів тимчасово недоступний."];
  }

  if (data?.detail) return [data.detail];
  if (data?.message) return [data.message];
  if (!error.response) return ["Немає з'єднання з сервером."];

  return [`Не вдалося згенерувати образи (код ${status}).`];
};

export const useGenerateOutfits = () => {
  const { notify } = useNotification();

  return useMutation<GenerateOutfitsResponse, Error, GenerateOutfitsRequest>({
    mutationFn: async (request) => {
      const res = await offersApi.generateOutfits(request);
      return res.data;
    },
    onError: (error) => {
      const status = isAxiosError(error) ? error.response?.status : undefined;
      // 400 — помилка вибору користувача, не збій сервера
      const colour = status === 400 ? notificationColours.Orange : notificationColours.Red;

      getGenerateErrorMessages(error).forEach((text) =>
        notify({ text, code: status, colour, time: 8000 })
      );
    },
  });
};
