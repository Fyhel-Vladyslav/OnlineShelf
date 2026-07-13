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

def serve():
    # Пробуємо підкачати дані при старті
    print("⏳ Первинне завантаження довідників атрибутів...")
    attributes_client.fetch_attributes()
    
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    # Передаємо клієнт у сервіс через конструктор
    pb2_grpc.add_ClothingAnalyzerServicer_to_server(ClothingAnalyzerService(attributes_client), server)
    
    server.add_insecure_port('[::]:50051')
    print("🚀 Python gRPC сервер успішно запущено на порті 50051...")
    server.start()
    server.wait_for_termination()

if __name__ == '__main__':
    serve()