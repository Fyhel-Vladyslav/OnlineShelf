export const notificationColours = {
    Red: "#ff4d4f",
    Green: "#52c41a",
    Blue: "#1677ff",
    Orange: "#fa8c16",
    Gray: "#595959",
  } as const;
  
  export type NotificationColour =
    typeof notificationColours[keyof typeof notificationColours];