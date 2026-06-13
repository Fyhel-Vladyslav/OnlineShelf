import grpc
import io
import base64
from PIL import Image
import numpy as np

import app.grpc.generated.parse_image_pb2 as pb2
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc

from app.models.clothing_detector.detector import ClothingDetector
from app.models.color_detector.detector import ColorDetector
from app.models.material_detector.detector import MaterialDetector
from app.models.pattern_detector.detector import PatternDetector

class ClothingAnalyzerService(pb2_grpc.ClothingAnalyzerServicer):
    def __init__(self, attributes_client):
        self.detector = ClothingDetector()
        self.color_detector = ColorDetector()
        self.pattern_detector = PatternDetector()
        self.material_detector = MaterialDetector()
        self.attr_client = attributes_client # Наш HTTP-клієнт

    def AnalyzeClothing(self, request, context):
        try:
            # 1. ПЕРЕВІРКА НАЯВНОСТІ АТРИБУТІВ (Lazy Loading)
            if not self.attr_client.has_data():
                print("🔄 Кеш порожній. Спроба повторного викачування атрибутів...")
                success_fetch = self.attr_client.fetch_attributes()
                
                if not success_fetch:
                    return pb2.AnalyzeClothingResponse(
                        success=False, 
                        error_message="Помилка: Не вдалося завантажити довідники атрибутів з ShelfService. Сервіс недоступний."
                    )

            # Отримуємо сирі дані з gRPC запиту
            raw_bytes = request.image_data
            if not raw_bytes:
                return pb2.AnalyzeClothingResponse(success=False, error_message="Empty image data")
            
            # Ініціалізуємо змінну для фінальних байтів чистого зображення
            final_image_bytes = raw_bytes
            
            # --- БЛОК БЕЗПЕЧНОГО ДЕКОДУВАННЯ BASE64 ---
            # Перевіряємо, чи байти НЕ є початком стандартних файлів (WebP, JPEG, PNG)
            # Якщо запит починається з тексту (наприклад, "data:" або "ey"), розкодуємо його з base64
            if raw_bytes.startswith(b"data:") or not raw_bytes.startswith((b"RIFF", b"\xff\xd8", b"\x89PNG")):
                print("⚠️ Виявлено base64 рядок у gRPC-запиті. Спроба автоматичного розкодування...")
                try:
                    base64_data = raw_bytes
                    # Якщо Postman надіслав рядок з префіксом "data:image/webp;base64,", відрізаємо його
                    if b"," in raw_bytes:
                        base64_data = raw_bytes.split(b",")[1]
                    
                    final_image_bytes = base64.b64decode(base64_data)
                except Exception as b64_err:
                    print(f"❌ Помилка декодування Base64: {b64_err}")
                    return pb2.AnalyzeClothingResponse(
                        success=False, 
                        error_message=f"Невдала спроба декодувати Base64 рядок: {str(b64_err)}"
                    )
            # ------------------------------------------

            # 2. Робота ШІ-пайплайну
            try:
                image = Image.open(io.BytesIO(final_image_bytes)).convert("RGB")
                
                # ТИМЧАСОВИЙ ДЕБАГ: зберігаємо отримане зображення на диск в корінь проєкту
                image.save("debug_yolo_input.webp")
                print(f"✅ Зображення успішно зчитано. Розмір: {image.size}. Файл 'debug_yolo_input.webp' оновлено.")
                
                image_np = np.array(image)
            except Exception as img_err:
                print(f"❌ Pillow не зміг відкрити зображення: {img_err}")
                return pb2.AnalyzeClothingResponse(
                    success=False, 
                    error_message=f"Помилка структури файлу зображення (Pillow): {str(img_err)}"
                )
            
            # Передаємо в детекцію
            detection_result = self.detector.detect_and_crop(image_np)
            crop = detection_result["crop_bgr"]
            colors = self.color_detector.get_dominant_colors(crop)
            
            pattern_text = self.pattern_detector.predict_pattern(crop)      

            material_text = self.material_detector.predict_material(crop)   

            # 3. ДИНАМІЧНИЙ МАПІНГ ТЕКСТУ НА ID З БАЗИ ДАНИХ
            yolo_class_name = detection_result["raw_class_name"] 
            
            final_type_id = self.attr_client.get_key_by_value("Type", yolo_class_name)
            
            final_pattern_id = self.attr_client.get_key_by_value("Pattern", pattern_text)
            final_material_id = self.attr_client.get_key_by_value("Material", material_text)
            
            detected_season_text = "summer" if yolo_class_name in ["shorts", "t-shirt"] else "winter"
            final_season_id = self.attr_client.get_key_by_value("Season", detected_season_text)

            # Формуємо gRPC відповідь
            attributes = pb2.ClothingAttributes(
                attribute_color_main=colors["main"], 
                attribute_color_second=colors["second"],
                attribute_type=final_type_id,      
                attribute_season=final_season_id,     
                attribute_pattern=final_pattern_id,
                attribute_material=final_material_id,
                confidence=detection_result["confidence"]
            )
            
            return pb2.AnalyzeClothingResponse(success=True, attributes=attributes)
            
        except Exception as e:
            print(f"Помилка в пайплайні ШІ: {e}")
            return pb2.AnalyzeClothingResponse(success=False, error_message=str(e))