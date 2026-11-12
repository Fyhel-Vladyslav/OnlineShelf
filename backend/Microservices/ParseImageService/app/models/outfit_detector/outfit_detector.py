import os
from pathlib import Path

import cv2
import numpy as np
import torch
from PIL import Image
from transformers import AutoImageProcessor, AutoModelForObjectDetection

WEIGHTS_DIR = Path(__file__).resolve().parent.parent / "weights" / "outfit_detector"

# Клас детектора → слот, як OutfitGeneration:Slots в OutfitOfferService. bag пропускаємо: такого типу немає в БД.
CLASS_SLOTS = {
    "top": "Top",
    "outer": "Outerwear",
    "bottom": "Bottom",
    "dress": "FullBody",
    "shoes": "Shoes",
    "hat": "Accessory",
}
DEFAULT_THRESHOLD = 0.25  # баланс знайдених образів і хибних рамок на фото людей (ml/outfit_scorer/src/eval_detection.py)


def select_items(detections: list[dict]) -> list[dict]:
    """По одній речі на клас; два черевики однієї пари стають однією річчю зі спільною рамкою."""
    items = []
    for raw_class_name in CLASS_SLOTS:
        ranked = sorted((d for d in detections if d["raw_class_name"] == raw_class_name),
                        key=lambda d: d["confidence"], reverse=True)
        if raw_class_name != "shoes" or len(ranked) < 2:
            items += ranked[:1]
            continue
        first, second = ranked[:2]
        items.append({
            "raw_class_name": raw_class_name,
            "confidence": (first["confidence"] + second["confidence"]) / 2,
            "box": (min(first["box"][0], second["box"][0]), min(first["box"][1], second["box"][1]),
                    max(first["box"][2], second["box"][2]), max(first["box"][3], second["box"][3])),
        })
    return items


class OutfitDetector:
    """Усі речі на фото людини для еталонних образів (AnalyzeOutfit): Conditional DETR, 7 широких класів."""

    def __init__(self, weights_dir: Path = WEIGHTS_DIR):
        self.threshold = float(os.getenv("OUTFIT_DETECTION_THRESHOLD", DEFAULT_THRESHOLD))
        self.model = None
        if not (weights_dir / "model.safetensors").exists():
            print(f"⚠️ Ваг детектора образів немає в {weights_dir}: AnalyzeOutfit недоступний, решта сервісу працює.")
            return

        print(f"Loading Outfit Detector model from: {weights_dir}")
        self.processor = AutoImageProcessor.from_pretrained(weights_dir, local_files_only=True)
        self.model = AutoModelForObjectDetection.from_pretrained(weights_dir, local_files_only=True).eval()

    @property
    def is_ready(self) -> bool:
        return self.model is not None

    def detect(self, image_np: np.ndarray) -> list[dict]:
        height, width = image_np.shape[:2]
        inputs = self.processor(images=[Image.fromarray(image_np)], return_tensors="pt").to(self.model.device)
        with torch.no_grad():
            outputs = self.model(**inputs)
        result = self.processor.post_process_object_detection(
            outputs, threshold=self.threshold, target_sizes=[(height, width)])[0]

        detections = []
        for score, label, box in zip(result["scores"], result["labels"], result["boxes"]):
            x1, y1, x2, y2 = (int(v) for v in box.tolist())
            box = (max(x1, 0), max(y1, 0), min(x2, width), min(y2, height))
            if box[2] > box[0] and box[3] > box[1]:
                detections.append({"raw_class_name": self.model.config.id2label[int(label)],
                                   "confidence": float(score), "box": box})

        image_bgr = cv2.cvtColor(image_np, cv2.COLOR_RGB2BGR)
        items = select_items(detections)
        for item in items:
            x1, y1, x2, y2 = item["box"]
            item["slot"] = CLASS_SLOTS[item["raw_class_name"]]
            item["crop_bgr"] = image_bgr[y1:y2, x1:x2]
        return items
