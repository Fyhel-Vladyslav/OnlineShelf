from pathlib import Path
import cv2
import numpy as np
from ultralytics import YOLO
CURRENT_DIR = Path(__file__).resolve().parent
WEIGHTS_DIR = CURRENT_DIR.parent / "weights"

class ClothingDetector:
    def __init__(self, model_filename="fashion_best.pt"):
        WEIGHTS_DIR.mkdir(parents=True, exist_ok=True)
        self.model_path = WEIGHTS_DIR / model_filename
        print(f"Loading Clothing Detector model from: {self.model_path}")

        # Завантажуємо предобучену модель YOLOv8
        self.model = YOLO(str(self.model_path))

    def detect_and_crop(self, image_np, allowed_classes=None):
        # Переконуємося, що кольори правильні для YOLO
        
        image_bgr = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)
        results = self.model(image_bgr, verbose=False, conf=0.05)[0]
        
        best_box = None
        best_class_name = None
        highest_conf = 0.0
        
        # Тимчасовий дебаг: подивимось у консоль, що взагалі бачить сира модель
        if len(results.boxes) > 0:
            print("📊 [YOLO Raw] Знайдено на фото:")
            for box in results.boxes:
                c_id = int(box.cls[0].item())
                c_name = results.names[c_id]
                c_conf = float(box.conf[0].item())
                print(f"  - {c_name} (conf: {c_conf:.2f})")
        
        for box in results.boxes:
            class_id = int(box.cls[0].item())
            class_name = results.names[class_id] # Отримуємо ім'я класу ('trousers', 'coat' тощо)
            conf = float(box.conf[0].item())
            
            # Фільтруємо за текстом і шукаємо найкращий confidence
            # allowed_classes=None — довідник ще не завантажено, приймаємо будь-який клас
            is_allowed = allowed_classes is None or class_name in allowed_classes
            if is_allowed and conf > highest_conf:
                highest_conf = conf
                best_class_name = class_name
                best_box = box.xyxy[0].cpu().numpy().astype(int)
        
        # ЗАХИСТ: Якщо нічого не знайшли на фото
        if best_box is None:
            print("⚠️ YOLO не виявив жодного знайомого елементу одягу з дозволеного списку.")
            return {
                "raw_class_name": "unknown",
                "confidence": 0.0,
                "crop_bgr": image_bgr # Передаємо всю картинку для аналізу кольору
            }
            
        # Якщо знайшли — вирізаємо кроп
        x1, y1, x2, y2 = best_box
        crop_bgr = image_bgr[y1:y2, x1:x2]
        
        print(f"🎯 [Фільтр] Фінальний вибір пайплайну: {best_class_name} (Впевненість: {highest_conf:.2f})")
        
        return {
            "raw_class_name": best_class_name,
            "confidence": highest_conf,
            "crop_bgr": crop_bgr
        }