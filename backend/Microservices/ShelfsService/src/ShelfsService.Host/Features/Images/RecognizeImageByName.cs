using Clothing;
using FastEndpoints;
using Grpc.Core;
using ImageService.Protos;
using ShelfsService.src.ShelfsService.Common;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using System.IO;

namespace ShelfsService.src.ShelfsService.Host.Features.Items
{

    internal sealed record HttpRecognizeImageByNameReqest(string ImageName);

    internal sealed class RecognizeImageByName : Endpoint<HttpRecognizeImageByNameReqest>
    {
        private readonly ImageProcessor.ImageProcessorClient _imageClient;
        private readonly IImageAnalyzer _imageAnalyzer;

        public RecognizeImageByName(ImageProcessor.ImageProcessorClient imageClient, IImageAnalyzer imageAnalyzer)
        {
            _imageClient = imageClient;
            _imageAnalyzer = imageAnalyzer;
        }

        public override void Configure()
        {
            Get(ApiRoutes.RecognizeImageByName);
            // Policies("AdminPolicy");  
            AllowAnonymous();
        }

        public override async Task HandleAsync(HttpRecognizeImageByNameReqest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.ImageName))
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            var getImageRequest = new GetPhotoRequest { FileName = req.ImageName };
            byte[] imageBytes = null;

            try
            {
                using var call = _imageClient.GetPhotoByName(getImageRequest, cancellationToken: ct);

                // Use a MemoryStream to properly combine all incoming chunks
                using var ms = new MemoryStream();

                await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
                {
                    if (response.ChunkData != null)
                    {
                        await ms.WriteAsync(response.ChunkData.Memory, ct);
                    }
                }

                imageBytes = ms.ToArray();
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            //TODO: Check if user has this image and can make recognition request  

            try
            {
                // Pass the fully assembled byte array to the Python analyzer
                AnalyzeClothingResponse response = await _imageAnalyzer.AnalyzeAsync(imageBytes);

                // FastEndpoints will correctly set Content-Type to application/json here
                await Send.OkAsync(response, ct);
                return;
            }
            catch (RpcException ex)
            {
                AnalyzeClothingResponse response = new AnalyzeClothingResponse
                {
                    Success = false,
                    ErrorMessage = $"gRPC Error: {ex.Status.Detail} (StatusCode: {ex.StatusCode})"
                };
                await Send.OkAsync(response, ct);
                return;
            }
        }
    }
}