import grpc
from concurrent import futures
import sys
import os

# Додаємо кореневу папку у шлях, щоб імпорти app.... працювали коректно
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Імпортуємо перейменований gRPC модуль (дефіс замінено на підкреслення)
import app.grpc.generated.parse_image_pb2_grpc as pb2_grpc
from app.grpc.services.analyzer_service import ClothingAnalyzerService

def serve():
    # Створюємо gRPC сервер
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    
    # Реєструємо наш сервіс у сервері
    pb2_grpc.add_ClothingAnalyzerServicer_to_server(ClothingAnalyzerService(), server)
    
    # Слухаємо на всіх інтерфейсах на порті 50051
    server.add_insecure_port('[::]:50051')
    print("🚀 Python gRPC сервер успішно запущено на порті 50051...")
    server.start()
    server.wait_for_termination()

if __name__ == '__main__':
    serve()