using ErrorOr;
using FluentValidation;
using FastEndpoints;
using ImageService.Protos;
using ImageService.src.ImageService.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Clothing;
using Grpc.Core;

namespace ImageService.src.ImageService.Host.Features.Recognizer
{
    public sealed record RecognizeClothesRequest(
        String ImageName
    ) : IRequest<ErrorOr<AnalyzeClothingResponse>>;

    internal sealed class RecognizeClothesRequestValidator : AbstractValidator<RecognizeClothesRequest>
    {
        public RecognizeClothesRequestValidator()
        {
            RuleFor(v => v.ImageName)
                .NotEmpty().WithMessage("ImageName is required.");
        }
    }


public class RecognizeClothesRequestHandler(
    ClothingAnalyzer.ClothingAnalyzerClient grpcClient,
    IConfiguration configuration
) : Endpoint<RecognizeClothesRequest, AnalyzeClothingResponse>
        {
            private readonly string _storagePath = configuration["StorageSettings:ImagesPath"] ?? "/app/images";
            private readonly ClothingAnalyzer.ClothingAnalyzerClient _grpcClient = grpcClient;

            public override void Configure()
            {
                Get(ApiRoutes.RecognizeClothes);
                AllowAnonymous();
            }

            public override async Task HandleAsync(RecognizeClothesRequest request, CancellationToken ct)
            {

                //var userId = User.GetUserId();
                //if (userId == Guid.Empty)
                //{
                //    await Send.UnauthorizedAsync(ct);
                //    return;
                //}
                if (String.IsNullOrEmpty(request.ImageName))
                {
                    await Send.ErrorsAsync(400, ct);
                    return;
                }

                var fileName = Path.GetFileName(request.ImageName);
                var filePath = Path.Combine(_storagePath, fileName);

                if (!File.Exists(filePath))
                {
                    // Note: Since this is an HTTP endpoint, throwing an RpcException might result in a 500 error.
                    // Consider using Send.NotFoundAsync(ct) instead!
                    await Send.NotFoundAsync(ct);
                    return;
                }

                const int chunkSize = 1024 * 1024;
                var buffer = new byte[chunkSize];

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                while (true)
                {
                // Перевіряємо, чи клієнт не скасував запит
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                // Якщо прочитали 0 байт, значить файл закінчився
                if (bytesRead == 0)
                {
                    break;
                }


                var recognizeRequest = new AnalyzeClothingRequest
                {
                    ImageData = Google.Protobuf.ByteString.CopyFrom(buffer)
                };

                try
                {
                    // Робимо асинхронний виклик до Python сервісу
                    AnalyzeClothingResponse response = await _grpcClient.AnalyzeClothingAsync(recognizeRequest);

                    if (response == null)
                        return;



                    if(!response.Success)
                    {
                        await Send.ResponseAsync(response, 502, ct);
                        return;
                    }
                    
                    await Send.OkAsync(response, cancellation: ct);
                    return;
                }
                catch (RpcException ex)
                {
                    await Send.ResponseAsync(new AnalyzeClothingResponse {ErrorMessage=ex.Message }, 502, cancellation: ct);
                    return;
                }
            }
        }
    }
}



