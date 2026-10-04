import cv2
import numpy as np
import torch
from PIL import Image


class VisualEmbedder:
    """
    Візуальний ембединг речі для скорера сумісності (x_i, візуальна частина).
    Використовує той самий CLIP, що й MaterialDetector, щоб не тримати в пам'яті дві копії моделі.
    """

    def __init__(self, model, processor, model_name: str):
        self.model = model
        self.processor = processor
        self.model_name = model_name

    def embed(self, crop_bgr: np.ndarray) -> list[float]:
        crop_rgb = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB)
        inputs = self.processor(images=Image.fromarray(crop_rgb), return_tensors="pt")

        with torch.no_grad():
            features = self.model.get_image_features(**inputs)

        # transformers 5.x повертає BaseModelOutputWithPooling (проєктований вектор у pooler_output), 4.x — тензор
        if not isinstance(features, torch.Tensor):
            features = features.pooler_output

        # L2-нормалізація: косинусна схожість стає скалярним добутком, масштаб не залежить від фото
        features = features / features.norm(dim=-1, keepdim=True)
        return features[0].cpu().numpy().astype(np.float32).tolist()
