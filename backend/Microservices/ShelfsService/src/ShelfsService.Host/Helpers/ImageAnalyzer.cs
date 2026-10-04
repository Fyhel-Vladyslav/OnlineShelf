using Clothing;
using Grpc.Core;
using ShelfsService.src.ShelfsService.Common.Interfaces;

namespace ShelfsService.src.ShelfsService.Host.Helpers
{
    public class ImageAnalyzer : IImageAnalyzer
    {
        private readonly ClothingAnalyzer.ClothingAnalyzerClient _grpcClient;

        // Впроваджуємо згенерований gRPC-клієнт через конструктор
        public ImageAnalyzer(ClothingAnalyzer.ClothingAnalyzerClient grpcClient)
        {
            _grpcClient = grpcClient;
        }

        public async Task<AnalyzeClothingResponse> AnalyzeAsync(byte[] imageBytes)
        {
            // Пакуємо сирі байти C# у спеціальний тип ByteString для Protobuf
            var request = new AnalyzeClothingRequest
            {
                ImageData = Google.Protobuf.ByteString.CopyFrom(imageBytes)
            };

            try
            {
                // Робимо асинхронний виклик до Python сервісу
                AnalyzeClothingResponse response = await _grpcClient.AnalyzeClothingAsync(request);
                return response;
            }
            catch (RpcException ex)
            {
                // Обробка помилок зв'язку (наприклад, якщо Python-сервіс вимкнено)
                return new AnalyzeClothingResponse
                {
                    Success = false,
                    ErrorMessage = $"gRPC Error: {ex.Status.Detail} (StatusCode: {ex.StatusCode})"
                };
            }
        }

        public async Task<EmbedClothingResponse> EmbedAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            var request = new EmbedClothingRequest
            {
                ImageData = Google.Protobuf.ByteString.CopyFrom(imageBytes)
            };

            try
            {
                return await _grpcClient.EmbedClothingAsync(request, cancellationToken: ct);
            }
            catch (RpcException ex)
            {
                return new EmbedClothingResponse
                {
                    Success = false,
                    ErrorMessage = $"gRPC Error: {ex.Status.Detail} (StatusCode: {ex.StatusCode})"
                };
            }
        }
    }
}