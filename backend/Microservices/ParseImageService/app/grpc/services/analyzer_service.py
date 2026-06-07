import grpc
import io
from PIL import Image
import numpy as np

# Імпортуємо наші згенеровані модулі
import app.grpc.generated.parse_image_pb2 as pb2
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc

class ClothingAnalyzerService(pb2_grpc.ClothingAnalyzerServicer):
    
    def AnalyzeClothing(self, request, context):
        try:
            image_bytes = request.image_data
            if not image_bytes:
                return pb2.AnalyzeClothingResponse(success=False, error_message="Empty image data")
            
            # Конвертація байтів у формат для обробки (залишаємо на майбутнє для YOLO/OpenCV)
            try:
                image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
                image_np = np.array(image)
            except Exception as img_err:
                # Якщо тестять просто рандомним текстом у лапках, PIL може впасти. 
                # Для тестів заглушки просто пропустимо цю помилку.
                print(f"Лог: Не вдалося розпарсити байти як картинку (це ок для тесту текстом): {img_err}")
            
            # Формуємо об'єкт ClothingAttributes відповідно до нової структури з proto
            mock_attributes = pb2.ClothingAttributes(
                attribute_color_main="Чорний",
                attribute_color_second="Білий",
                attribute_type=1,       # Наприклад: 1 = Футболка
                attribute_season=2,     # Наприклад: 2 = Літо
                attribute_pattern=0,    # 0 = Однотонний
                attribute_material=0,   # 0 = Бавовна
                confidence=0.92
            )
            
            # Повертаємо правильну відповідь
            return pb2.AnalyzeClothingResponse(success=True, attributes=mock_attributes)
            
        except Exception as e:
            return pb2.AnalyzeClothingResponse(success=False, error_message=str(e))