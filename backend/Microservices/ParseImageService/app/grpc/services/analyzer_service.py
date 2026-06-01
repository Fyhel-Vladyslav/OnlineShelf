import grpc
import io
from PIL import Image
import numpy as np

# Імпортуємо згенеровані класи
import app.grpc.generated.parse_image_pb2 as pb2
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc

class ClothingAnalyzerService(pb2_grpc.ClothingAnalyzerServicer):
    
    def AnalyzeClothing(self, request, context):
        try:
            # 1. Декодуємо байти зображення (наші 20 кБ WebP)
            image_bytes = request.image_data
            if not image_bytes:
                return pb2.AnalyzeClothingResponse(success=False, error_message="Empty image data")
            
            image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
            image_np = np.array(image) # Готово для OpenCV чи YOLO
            
            # TODO: Тут буде виклик моделей з app/models/
            # Наразі робимо Mock-відповідь для тестування зв'язку з C#
            
            detected_items = [
                pb2.DetectedItem(
                    clothing_type="Футболка",
                    color="Чорний",
                    season="Літо",
                    confidence=0.95
                )
            ]
            
            return pb2.AnalyzeClothingResponse(success=True, items=detected_items)
            
        except Exception as e:
            return pb2.AnalyzeClothingResponse(success=False, error_message=str(e))