import grpc
import io
import base64
from PIL import Image, ImageOps
import numpy as np

import app.grpc.generated.parse_image_pb2 as pb2
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc

from app.models.clothing_detector.clothing_detector import ClothingDetector
from app.models.color_detector.color_detector import ColorDetector
from app.models.material_detector.material_detector import MODEL_NAME, MaterialDetector
from app.models.outfit_detector.outfit_detector import OutfitDetector
from app.models.pattern_detector.pattern_detector import PatternDetector
from app.models.type_classifier.type_classifier import TypeClassifier
from app.models.visual_embedder.visual_embedder import VisualEmbedder

# Мапимо нові класи з твого датасету на базові сезони для .NET довідника
SUMMER_CLASSES = {
    "shorts", "t-shirt", "short_sleeve_top", "sling", 
    "skirt", "short_sleeve_dress", "sling_dress", "vest"
}

ATTRIBUTES_UNAVAILABLE = "Помилка: Не вдалося завантажити довідники атрибутів з ShelfService. Сервіс недоступний."

class ClothingAnalyzerService(pb2_grpc.ClothingAnalyzerServicer):
    def __init__(self, attributes_client):
        self.detector = ClothingDetector()
        self.color_detector = ColorDetector()
        self.pattern_detector = PatternDetector()
        self.material_detector = MaterialDetector()
        self.embedder = VisualEmbedder(
            self.material_detector.model,
            self.material_detector.processor,
            MODEL_NAME)
        self.outfit_detector = OutfitDetector()
        self.type_classifier = TypeClassifier(self.material_detector.model, self.material_detector.processor)
        self.attr_client = attributes_client  # HTTP/REST клієнт для зв'язку з .NET

    def AnalyzeClothing(self, request, context):
        try:
            # 1. ПЕРЕВІРКА НАЯВНОСТІ АТРИБУТІВ (Lazy Loading з .NET)
            if not self._ensure_attributes():
                return pb2.AnalyzeClothingResponse(success=False, error_message=ATTRIBUTES_UNAVAILABLE)

            # Отримуємо сирі дані з gRPC запиту
            raw_bytes = request.image_data
            if not raw_bytes:
                return pb2.AnalyzeClothingResponse(success=False, error_message="Empty image data")
            
            image_np, decode_error = self._decode_image(raw_bytes)
            if decode_error:
                return pb2.AnalyzeClothingResponse(success=False, error_message=decode_error)

            # --- ШІ ПАЙПЛАЙН АНАЛІЗУ ---
            allowed_clothing_types = self.attr_client.get_all_values_by_attribute("Type")
            detection_result = self.detector.detect_and_crop(image_np, allowed_classes=allowed_clothing_types)
            
            # КРИТИЧНИЙ ФІКС: Перевірка, чи YOLO взагалі знайшла хоч якийсь одяг
            if not detection_result or "crop_bgr" not in detection_result or detection_result["crop_bgr"] is None:
                print("⚠️ Об'єктів одягу на зображенні не виявлено.")
                return pb2.AnalyzeClothingResponse(
                    success=False, 
                    error_message="Модель не змогла локалізувати жодного елементу одягу на фото."
                )
                
            attributes = self._build_attributes(
                detection_result["crop_bgr"], detection_result["raw_class_name"], detection_result.get("confidence", 0.0))
            return pb2.AnalyzeClothingResponse(success=True, attributes=attributes)

        except Exception as e:
            print(f"❌ Загальна помилка в gRPC пайплайні: {e}")
            return pb2.AnalyzeClothingResponse(success=False, error_message=f"Internal service error: {str(e)}")

    def EmbedClothing(self, request, context):
        try:
            raw_bytes = request.image_data
            if not raw_bytes:
                return pb2.EmbedClothingResponse(success=False, error_message="Empty image data")

            image_np, decode_error = self._decode_image(raw_bytes)
            if decode_error:
                return pb2.EmbedClothingResponse(success=False, error_message=decode_error)

            # Ембединг рахуємо по кропу речі (без фону), а якщо YOLO нічого не знайшов — по всьому фото
            allowed_types = self.attr_client.get_all_values_by_attribute("Type") if self.attr_client.has_data() else None
            detection_result = self.detector.detect_and_crop(image_np, allowed_classes=allowed_types)
            crop = detection_result.get("crop_bgr")
            if crop is None or crop.size == 0:
                return pb2.EmbedClothingResponse(success=False, error_message="Empty crop")

            embedding = self.embedder.embed(crop)
            print(f"✅ Ембединг пораховано: dim={len(embedding)}, клас={detection_result.get('raw_class_name')}")
            return pb2.EmbedClothingResponse(
                success=True,
                embedding=embedding,
                embedding_model=self.embedder.model_name)

        except Exception as e:
            print(f"❌ Помилка під час обчислення ембединга: {e}")
            return pb2.EmbedClothingResponse(success=False, error_message=f"Internal service error: {str(e)}")

    def AnalyzeOutfit(self, request, context):
        try:
            if not self.outfit_detector.is_ready:
                return pb2.AnalyzeOutfitResponse(success=False, error_message="Ваги детектора образів не встановлено")
            if not self._ensure_attributes():
                return pb2.AnalyzeOutfitResponse(success=False, error_message=ATTRIBUTES_UNAVAILABLE)

            raw_bytes = request.image_data
            if not raw_bytes:
                return pb2.AnalyzeOutfitResponse(success=False, error_message="Empty image data")

            image_np, decode_error = self._decode_outfit_image(raw_bytes)
            if decode_error:
                return pb2.AnalyzeOutfitResponse(success=False, error_message=decode_error)
            image_height, image_width = image_np.shape[:2]

            items = []
            for detection in self.outfit_detector.detect(image_np):
                crop = detection["crop_bgr"]
                embedding = self.embedder.embed(crop)
                type_name = self.type_classifier.predict_type(embedding, detection["raw_class_name"])
                x1, y1, x2, y2 = detection["box"]
                items.append(pb2.OutfitItem(
                    slot=detection["slot"],
                    attributes=self._build_attributes(crop, type_name, detection["confidence"]),
                    box=pb2.BoundingBox(x1=x1, y1=y1, x2=x2, y2=y2),
                    embedding=embedding))

            print(f"✅ Образ розібрано: {[item.slot for item in items]}")
            return pb2.AnalyzeOutfitResponse(
                success=True, items=items, embedding_model=self.embedder.model_name,
                image_width=image_width, image_height=image_height)

        except Exception as e:
            print(f"❌ Помилка під час аналізу образу: {e}")
            return pb2.AnalyzeOutfitResponse(success=False, error_message=f"Internal service error: {str(e)}")

    def _ensure_attributes(self):
        """Довідники атрибутів потрібні для мапінгу на ID з .NET; якщо кеш порожній — пробуємо викачати ще раз."""
        if self.attr_client.has_data():
            return True
        print("🔄 Кеш порожній. Спроба повторного викачування атрибутів...")
        return self.attr_client.fetch_attributes()

    def _build_attributes(self, crop, class_name, confidence):
        # Запускаємо інші детектори на вирізаному шматочку
        colors = self.color_detector.get_dominant_colors(crop)
        pattern_text = self.pattern_detector.predict_pattern(crop)
        material_text = self.material_detector.predict_material(crop)

        # --- ДИНАМІЧНИЙ МАПІНГ НА ID З .NET БАЗИ ДАНИХ ---
        # Передаємо чисті текстові назви класів у твій attr_client, який поверне ID для БД
        final_type_id = self.attr_client.get_key_by_value("Type", class_name)
        final_pattern_id = self.attr_client.get_key_by_value("Pattern", pattern_text)
        final_material_id = self.attr_client.get_key_by_value("Material", material_text)

        if not final_type_id:
            print(f"⚠️ Попередження: Тип '{class_name}' відсутній у базі даних .NET. Потрібно додати сеед.")

        # Оновлена логіка визначення сезону під твій новий датасет
        detected_season_text = "summer" if class_name in SUMMER_CLASSES else "winter"
        final_season_id = self.attr_client.get_key_by_value("Season", detected_season_text)

        print(f"✅ Успішно розпізнано: {class_name} ({material_text}, {pattern_text})")
        return pb2.ClothingAttributes(
            attribute_color_main=colors.get("main", "#000000"),
            attribute_color_second=colors.get("second", "#000000"),
            attribute_type=final_type_id,
            attribute_season=final_season_id,
            attribute_pattern=final_pattern_id,
            attribute_material=final_material_id,
            confidence=confidence
        )

    @staticmethod
    def _decode_outfit_image(raw_bytes):
        """Фото образу з телефона: лише JPEG, PNG, WebP, з поворотом за EXIF, щоб рамки збігалися з фото на екрані.

        Повертає (image_np RGB, None) або (None, текст помилки). Base64 тут не підтримується: дизайнер завантажує файли.
        """
        is_webp = raw_bytes.startswith(b"RIFF") and raw_bytes[8:12] == b"WEBP"
        if not (is_webp or raw_bytes.startswith((b"\xff\xd8\xff", b"\x89PNG\r\n\x1a\n"))):
            return None, "Непідтримуваний формат фото: потрібні JPEG, PNG або WebP"
        try:
            image = ImageOps.exif_transpose(Image.open(io.BytesIO(raw_bytes))).convert("RGB")
            return np.array(image), None
        except Exception as img_err:
            return None, f"Помилка структури файлу зображення (Pillow): {str(img_err)}"

    @staticmethod
    def _decode_image(raw_bytes):
        """Повертає (image_np RGB, None) або (None, текст помилки). Підтримує сирі байти і base64/data-URL."""
        final_image_bytes = raw_bytes
        if raw_bytes.startswith(b"data:") or not raw_bytes.startswith((b"RIFF", b"\xff\xd8", b"\x89PNG")):
            print("⚠️ Виявлено base64 рядок у gRPC-запиті. Спроба автоматичного розкодування...")
            try:
                base64_data = raw_bytes.split(b",")[1] if b"," in raw_bytes else raw_bytes
                final_image_bytes = base64.b64decode(base64_data)
            except Exception as b64_err:
                return None, f"Невдала спроба декодувати Base64 рядок: {str(b64_err)}"

        try:
            image = Image.open(io.BytesIO(final_image_bytes)).convert("RGB")
            return np.array(image), None
        except Exception as img_err:
            return None, f"Помилка структури файлу зображення (Pillow): {str(img_err)}"
