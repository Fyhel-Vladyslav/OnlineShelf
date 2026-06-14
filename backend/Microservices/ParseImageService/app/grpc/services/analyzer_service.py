import grpc
import io
import base64
from PIL import Image
import numpy as np

import app.grpc.generated.parse_image_pb2 as pb2
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc

from app.models.clothing_detector.clothing_detector import ClothingDetector
from app.models.color_detector.color_detector import ColorDetector
from app.models.material_detector.material_detector import MaterialDetector
from app.models.pattern_detector.pattern_detector import PatternDetector

# Мапимо нові класи з твого датасету на базові сезони для .NET довідника
SUMMER_CLASSES = {
    "shorts", "t-shirt", "short_sleeve_top", "sling", 
    "skirt", "short_sleeve_dress", "sling_dress", "vest"
}

class ClothingAnalyzerService(pb2_grpc.ClothingAnalyzerServicer):
    def __init__(self, attributes_client):
        self.detector = ClothingDetector()
        self.color_detector = ColorDetector()
        self.pattern_detector = PatternDetector()
        self.material_detector = MaterialDetector()
        self.attr_client = attributes_client  # HTTP/REST клієнт для зв'язку з .NET

    def AnalyzeClothing(self, request, context):
        try:
            # 1. ПЕРЕВІРКА НАЯВНОСТІ АТРИБУТІВ (Lazy Loading з .NET)
            if not self.attr_client.has_data():
                print("🔄 Кеш порожній. Спроба повторного викачування атрибутів...")
                if not self.attr_client.fetch_attributes():
                    return pb2.AnalyzeClothingResponse(
                        success=False, 
                        error_message="Помилка: Не вдалося завантажити довідники атрибутів з ShelfService. Сервіс недоступний."
                    )

            # Отримуємо сирі дані з gRPC запиту
            raw_bytes = request.image_data
            if not raw_bytes:
                return pb2.AnalyzeClothingResponse(success=False, error_message="Empty image data")
            
            final_image_bytes = raw_bytes
            
            # --- БЛОК БЕЗПЕЧНОГО ДЕКОДУВАННЯ BASE64 ---
            if raw_bytes.startswith(b"data:") or not raw_bytes.startswith((b"RIFF", b"\xff\xd8", b"\x89PNG")):
                print("⚠️ Виявлено base64 рядок у gRPC-запиті. Спроба автоматичного розкодування...")
                try:
                    base64_data = raw_bytes
                    if b"," in raw_bytes:
                        base64_data = raw_bytes.split(b",")[1]
                    
                    final_image_bytes = base64.b64decode(base64_data)
                except Exception as b64_err:
                    print(f"❌ Помилка декодування Base64: {b64_err}")
                    return pb2.AnalyzeClothingResponse(
                        success=False, 
                        error_message=f"Невдала спроба декодувати Base64 рядок: {str(b64_err)}"
                    )

            # --- ЗЧИТУВАННЯ ЗОБРАЖЕННЯ ---
            try:
                image = Image.open(io.BytesIO(final_image_bytes)).convert("RGB")
                
                # ТИМЧАСОВИЙ ДЕБАГ (залиш за бажанням або закоментуй в прод)
                image.save("debug_yolo_input.webp")
                
                image_np = np.array(image)
            except Exception as img_err:
                print(f"❌ Pillow не зміг відкрити зображення: {img_err}")
                return pb2.AnalyzeClothingResponse(
                    success=False, 
                    error_message=f"Помилка структури файлу зображення (Pillow): {str(img_err)}"
                )
            
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
                
            crop = detection_result["crop_bgr"]
            
            # Запускаємо інші детектори на вирізаному шматочку
            colors = self.color_detector.get_dominant_colors(crop)
            pattern_text = self.pattern_detector.predict_pattern(crop)      
            material_text = self.material_detector.predict_material(crop)   

            # --- ДИНАМІЧНИЙ МАПІНГ НА ID З .NET БАЗИ ДАНИХ ---
            yolo_class_name = detection_result["raw_class_name"] 
            
            # Передаємо чисті текстові назви класів у твій attr_client, який поверне ID для БД
            final_type_id = self.attr_client.get_key_by_value("Type", yolo_class_name)
            final_pattern_id = self.attr_client.get_key_by_value("Pattern", pattern_text)
            final_material_id = self.attr_client.get_key_by_value("Material", material_text)
            
            if not final_type_id:
                print(f"⚠️ Попередження: Тип '{yolo_class_name}' відсутній у базі даних .NET. Потрібно додати сеед.")
            

            # Оновлена логіка визначення сезону під твій новий датасет
            detected_season_text = "summer" if yolo_class_name in SUMMER_CLASSES else "winter"
            final_season_id = self.attr_client.get_key_by_value("Season", detected_season_text)

            # --- ФОРМУВАННЯ ГАРДЕНРОБНОЇ ВІДПОВІДІ ---
            attributes = pb2.ClothingAttributes(
                attribute_color_main=colors.get("main", "#000000"), 
                attribute_color_second=colors.get("second", "#000000"),
                attribute_type=final_type_id,      
                attribute_season=final_season_id,     
                attribute_pattern=final_pattern_id,
                attribute_material=final_material_id,
                confidence=detection_result.get("confidence", 0.0)
            )
            
            print(f"✅ Успішно розпізнано: {yolo_class_name} ({material_text}, {pattern_text})")
            return pb2.AnalyzeClothingResponse(success=True, attributes=attributes)
            
        except Exception as e:
            print(f"❌ Загальна помилка в gRPC пайплайні: {e}")
            return pb2.AnalyzeClothingResponse(success=False, error_message=f"Internal service error: {str(e)}")