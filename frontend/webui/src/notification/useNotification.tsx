import { notificationStore } from "./notificationStore";
import { type NotificationColour, notificationColours } from "./notificationColours";

interface NotifyOptions {
  colour?: NotificationColour;
  text: string;
  code?: string | number;
  time: number;
}

export const useNotification = () => {
  const notify = (options: NotifyOptions) => {
    
    // 1. Словник: використовуємо назву ОБ'ЄКТА notificationColours
    const dictionary: Record<number, NotificationColour> = {
      401: notificationColours.Orange,
      500: notificationColours.Red,
      501: notificationColours.Red,
    };

    let finalColour = options.colour;

    if (!finalColour) {
      if(options.code)
        finalColour = dictionary[Number(options.code)] ?? notificationColours.Red;
      else
      finalColour = notificationColours.Red;

    }

    notificationStore.notify({
      ...options,
      colour: finalColour ?? notificationColours.Gray, // фолбек якщо немає ні кольору, ні коду
      id: crypto.randomUUID(),
    });
  };

  return { notify };
};