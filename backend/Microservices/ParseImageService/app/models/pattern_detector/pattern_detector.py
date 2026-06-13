import numpy as np

class PatternDetector:
    def __init__(self):
        # Тут згодом буде ініціалізація моделі PyTorch (EfficientNet)
        pass

    def predict_pattern(self, crop_bgr: np.ndarray) -> str:
        # Тимчасовий фолбек-заглушка для тестів, що повертає значення з твого довідника
        # Твої значення: наприклад, "plain", "striped", "checked"
        if crop_bgr is None or crop_bgr.size == 0:
            return "plain"
            
        # Логіка ШІ буде тут
        detected_pattern = "plain" 
        return detected_pattern