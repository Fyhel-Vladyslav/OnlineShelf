import { type NotificationColour } from "./notificationColours";

export interface NotificationItem {
    id: string;
    colour?: NotificationColour;
    text: string;
    code?: string | number;
    time: number;
    closing?: boolean;
  }

type Listener = (items: NotificationItem[]) => void;

class NotificationStore {
  private items: NotificationItem[] = [];
  private listeners = new Set<Listener>();

  subscribe(listener: Listener) {
    this.listeners.add(listener);
    listener(this.items);
  
    return () => {
      this.listeners.delete(listener);
    };
  }
  notify(item: NotificationItem) {
    this.items = [...this.items, item];
    this.emit();

    if (item.time > 0) {
      setTimeout(() => this.remove(item.id), item.time);
    }
  }

  remove(id: string) {
    this.items = this.items.map(n =>
      n.id === id ? { ...n, closing: true } : n
    );
    this.emit();
  
    setTimeout(() => {
      this.items = this.items.filter(n => n.id !== id);
      this.emit();
    }, 250); // must match CSS animation
  }

  private emit() {
    this.listeners.forEach(l => l(this.items));
  }
}

export const notificationStore = new NotificationStore();