import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { notificationStore, type NotificationItem } from "./notificationStore";

export const NotificationRoot = () => {
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    return notificationStore.subscribe(setItems);
  }, []);

useEffect(() => {
  setMounted(true);
}, []);

  return createPortal(
    <div style={containerStyle}>
        {items.map(n => (
        <div
            key={n.id}
            style={{
            ...notificationStyle,
            background: n.colour,
            opacity: n.closing ? 0 : 1,
            transform: n.closing
            ? "translateY(-20px)"
            : mounted
              ? "translateY(0)"
              : "translateY(-30px)",
            }}
        >
          {n.code !== undefined && (
            <div style={codeStyle}>{n.code}</div>
          )}

          <div style={textStyle}>{n.text.slice(0, 100)}</div>

          {n.time === 0 && (
            <button
              style={closeBtnStyle}
              onClick={() => notificationStore.remove(n.id)}
            >
              ✕
            </button>
          )}
        </div>
      ))}
    </div>,
    document.body
  );
};

const containerStyle: React.CSSProperties = {
    position: "fixed",
    top: 16,
    left: "50%",
    transform: "translateX(-50%)",
    display: "flex",
    flexDirection: "column",
    gap: 8,
    zIndex: 9999,
    pointerEvents: "none",
  };

const notificationStyle: React.CSSProperties = {
  display: "flex",
  alignItems: "center",
  color: "#fff",
  borderRadius: 6,
  minWidth: 320,
  boxShadow: "0 8px 24px rgba(0,0,0,0.15)",
  pointerEvents: "auto",
  transition: "all 250ms ease",
};
  
  const codeStyle: React.CSSProperties = {
    padding: "8px 12px",
    background: "rgba(0,0,0,0.2)",
    fontWeight: 600,
  };
  
  const textStyle: React.CSSProperties = {
    padding: "8px 12px",
    flex: 1,
  };
  
  const closeBtnStyle: React.CSSProperties = {
    background: "transparent",
    border: "none",
    color: "#fff",
    cursor: "pointer",
    padding: "0 10px",
    fontSize: 16,
  };