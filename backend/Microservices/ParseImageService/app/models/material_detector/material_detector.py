import cv2
import numpy as np
import torch
from PIL import Image
from transformers import CLIPProcessor, CLIPModel

class MaterialDetector:
    def __init__(self):
        print("📥 Завантаження моделі CLIP для аналізу матеріалів (openai/clip-vit-base-patch32)...")
        # Ініціалізуємо модель та процесор
        self.model = CLIPModel.from_pretrained("openai/clip-vit-base-patch32")
        self.processor = CLIPProcessor.from_pretrained("openai/clip-vit-base-patch32")
        
        # Переводимо в режим оцінки (evaluation mode) для економії ресурсів
        self.model.eval()

        # 1. Словник промтів для ШІ (CLIP найкраще працює з контекстом "photo of...")
        self.material_prompts = {
            "denim": "a photo of denim fabric jeans material",
            "leather": "a photo of leather jacket smooth leather material",
            "cotton": "a photo of cotton t-shirt fabric soft textile",
            "wool": "a photo of knitted wool sweater warm fabric",
            "silk": "a photo of shiny silk or satin fabric smooth material",
            "linen": "a photo of linen fabric texture",
            "polyester": "a photo of synthetic polyester sportswear fabric",
            "corduroy": "a photo of ribbed corduroy fabric material"
        }
        
        # 2. Мапінг результату ШІ на ТОЧНІ назви у твоїй C# базі даних
        # Зміни праву частину (значення), щоб вони збігалися з твоїм довідником у БД!
        self.db_mapping = {
            "denim": "denim",       # наприклад, якщо в БД це слово "Джинс" або "denim"
            "leather": "leather",   # якщо в БД "Шкіра" або "leather"
            "cotton": "cotton",
            "wool": "wool",
            "silk": "silk",
            "linen": "linen",
            "polyester": "synthetic",
            "corduroy": "corduroy"
        }

    def predict_material(self, crop_bgr: np.ndarray) -> str:
        # Захист: якщо кроп порожній, повертаємо дефолтне значення
        if crop_bgr is None or crop_bgr.size == 0:
            print("⚠️ [Material CLIP] Порожній кроп зображення. Повертаю дефолт: cotton")
            return "cotton"

        try:
            # CLIP очікує формат PIL Image в RGB гамі. Конвертуємо наш OpenCV BGR кроп
            crop_rgb = cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB)
            pil_image = Image.fromarray(crop_rgb)

            # Формуємо списки для моделі
            labels_keys = list(self.material_prompts.keys())
            prompts = list(self.material_prompts.values())

            # Токенізуємо текст та обробляємо картинку для ШІ
            inputs = self.processor(
                text=prompts, 
                images=pil_image, 
                return_tensors="pt", 
                padding=True
            )

            # Запускаємо inference без вирахування градієнтів (для швидкості)
            with torch.no_grad():
                outputs = self.model(**inputs)
                
            # Отримуємо оцінки схожості картинки з кожним текстом
            logits_per_image = outputs.logits_per_image  
            probs = logits_per_image.softmax(dim=-1).cpu().numpy()[0]

            # Знаходимо індекс з найбільшою ймовірністю
            best_match_idx = np.argmax(probs)
            detected_key = labels_keys[best_match_idx]
            confidence = probs[best_match_idx]

            print("📊 [Material CLIP] Результати аналізу тканини:")
            for key, prob in zip(labels_keys, probs):
                print(f"  - {key}: {prob*100:.1f}%")

            # Отримуємо фінальне ім'я для C# довідника
            final_db_value = self.db_mapping.get(detected_key, "cotton")
            print(f"🎯 [Material CLIP] Визначено матеріал: {detected_key} ({confidence*100:.1f}%) -> Мапимо в БД як: '{final_db_value}'")

            return final_db_value

        except Exception as e:
            print(f"❌ Помилка всередині модуля MaterialDetector: {e}")
            return "cotton" # Безпечний фолбек у разі збою