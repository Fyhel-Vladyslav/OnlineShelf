import grpc
from concurrent import futures
import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Створюємо глобальний об'єкт клієнта атрибутів
from app.core.attributes_client import AttributesServiceClient
attributes_client = AttributesServiceClient()

import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc
from app.grpc.services.analyzer_service import ClothingAnalyzerService

# Фото образів від дизайнера більші за типовий кроп, а стандартний ліміт gRPC лише 4 МБ
MAX_MESSAGE_BYTES = 32 * 1024 * 1024

def serve():
    # Пробуємо підкачати дані при старті
    print("⏳ Первинне завантаження довідників атрибутів...")
    attributes_client.fetch_attributes()
    
    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=10),
        options=[("grpc.max_receive_message_length", MAX_MESSAGE_BYTES)])
    # Передаємо клієнт у сервіс через конструктор
    pb2_grpc.add_ClothingAnalyzerServicer_to_server(ClothingAnalyzerService(attributes_client), server)
    
    server.add_insecure_port('[::]:50051')
    print("🚀 Python gRPC сервер успішно запущено на порті 50051...")
    server.start()
    server.wait_for_termination()

if __name__ == '__main__':
    serve()