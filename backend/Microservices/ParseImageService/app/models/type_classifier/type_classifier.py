import numpy as np
import torch

# Детектор образів знає лише широкі класи, тому точний тип обираємо серед значень Type з БД цього класу.
# Групи збігаються з OutfitGeneration:Slots в OutfitOfferService: тип не може вивести річ за межі слота.
# Кілька описів на тип: один опис на «top, t-shirt, sweatshirt» розмивається і програє вузьким типам.
TYPE_PROMPTS = {
    "top": {
        "shirt": ["a photo of a button-up shirt", "a photo of a collared dress shirt"],
        "top, t-shirt, sweatshirt": [
            "a photo of a t-shirt", "a photo of a tank top", "a photo of a blouse", "a photo of a crop top",
            "a photo of a sweatshirt", "a photo of a camisole",
        ],
        "sweater": ["a photo of a knitted sweater", "a photo of a wool jumper"],
        "cardigan": ["a photo of an open knitted cardigan"],
        "vest": ["a photo of a tailored waistcoat", "a photo of a puffer vest"],
    },
    "outer": {
        "jacket": ["a photo of a jacket", "a photo of a blazer"],
        "coat": ["a photo of a long coat", "a photo of a trench coat"],
        "cape": ["a photo of a cape", "a photo of a poncho"],
    },
    "bottom": {
        "pants": ["a photo of pants", "a photo of jeans", "a photo of leggings"],
        "shorts": ["a photo of shorts"],
        "skirt": ["a photo of a skirt"],
    },
    "dress": {
        "dress": ["a photo of a dress", "a photo of a gown"],
        "jumpsuit": ["a photo of a jumpsuit"],
    },
    "shoes": {"shoe": ["a photo of shoes"]},
    "hat": {"hat": ["a photo of a hat"]},
}


class TypeClassifier:
    """Обирає тип за ембедингом речі: окремий прохід FashionCLIP по кропу не потрібен."""

    def __init__(self, model, processor):
        self.model = model
        self.processor = processor
        self.text_features = {name: self._embed_group(prompts) for name, prompts in TYPE_PROMPTS.items() if len(prompts) > 1}

    def _embed_group(self, prompts: dict[str, list[str]]) -> np.ndarray:
        """Рядок на тип: середній нормований вектор його описів."""
        features = self._embed_texts([text for texts in prompts.values() for text in texts])
        ends = np.cumsum([len(texts) for texts in prompts.values()])
        means = np.stack([part.mean(axis=0) for part in np.split(features, ends[:-1])])
        return means / np.linalg.norm(means, axis=1, keepdims=True)

    def _embed_texts(self, texts: list[str]) -> np.ndarray:
        inputs = self.processor(text=texts, return_tensors="pt", padding=True).to(self.model.device)
        with torch.no_grad():
            features = self.model.get_text_features(**inputs)

        # transformers 5.x повертає BaseModelOutputWithPooling (проєктований вектор у pooler_output), 4.x — тензор
        if not isinstance(features, torch.Tensor):
            features = features.pooler_output
        features = features / features.norm(dim=-1, keepdim=True)
        return features.cpu().numpy()

    def predict_type(self, embedding: list[float], raw_class_name: str) -> str:
        names = list(TYPE_PROMPTS[raw_class_name])
        if len(names) == 1:
            return names[0]
        return names[int(np.argmax(self.text_features[raw_class_name] @ np.asarray(embedding)))]
