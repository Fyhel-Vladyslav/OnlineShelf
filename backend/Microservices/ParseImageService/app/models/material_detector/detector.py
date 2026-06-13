import numpy as np

class MaterialDetector:
    def __init__(self):
        # Тут згодом буде завантаження CLIP (from transformers import CLIPProcessor, CLIPModel)
        pass

    def predict_material(self, crop_bgr: np.ndarray) -> str:
        # Тимчасова заглушка. Твої значення: наприклад, "denim", "cotton", "leather"
        if crop_bgr is None or crop_bgr.size == 0:
            return "cotton"
            
        # Логіка CLIP буде тут
        detected_material = "cotton"
        return detected_material