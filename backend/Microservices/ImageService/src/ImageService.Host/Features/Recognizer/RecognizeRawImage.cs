//using Clothing;
//using Grpc.Core;
//using ImageService.src.ImageService.Common.Interfaces;

//namespace ImageService.src.ImageService.Host.Features.Recognizer;
//public class RecognizeRawImage : IImageAnalyzer
//{
//    private readonly ClothingAnalyzer.ClothingAnalyzerClient _grpcClient;

//    // Впроваджуємо згенерований gRPC-клієнт через конструктор
//    public RecognizeRawImage(ClothingAnalyzer.ClothingAnalyzerClient grpcClient)
//    {
//        _grpcClient = grpcClient;
//    }

//    public async Task<AnalyzeClothingResponse> AnalyzeAsync(byte[] imageBytes)
//    {
//        // Пакуємо сирі байти C# у спеціальний тип ByteString для Protobuf
//        var request = new AnalyzeClothingRequest
//        {
//            ImageData = Google.Protobuf.ByteString.CopyFrom(imageBytes)
//        };

//        try
//        {
//            // Робимо асинхронний виклик до Python сервісу
//            AnalyzeClothingResponse response = await _grpcClient.AnalyzeClothingAsync(request);
//            return response;
//        }
//        catch (RpcException ex)
//        {
//            // Обробка помилок зв'язку (наприклад, якщо Python-сервіс вимкнено)
//            return new AnalyzeClothingResponse
//            {
//                Success = false,
//                ErrorMessage = $"gRPC Error: {ex.Status.Detail} (StatusCode: {ex.StatusCode})"
//            };
//        }
//    }
//}
