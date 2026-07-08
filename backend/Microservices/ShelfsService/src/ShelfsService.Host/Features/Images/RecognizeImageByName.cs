using FastEndpoints;
using Grpc.Core;
using ImageService.Protos;
using ShelfsService.src.ShelfsService.Common;
using System.IO;

namespace ShelfsService.src.ShelfsService.Host.Features.Items
{

    internal sealed record HttpRecognizeImageByNameReqest(string ImageName);

    internal sealed class RecognizeImageByName : Endpoint<HttpRecognizeImageByNameReqest>
    {
        private readonly ImageProcessor.ImageProcessorClient _imageClient;

        public RecognizeImageByName(ImageProcessor.ImageProcessorClient imageClient)
        {
            _imageClient = imageClient;
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

            //TODO: Check if user has this image and can makerecognition request  

            var grpcRequest = new RecognizeImageByNameRequest { ImageName = req.ImageName };

            try
            {
                var response = _imageClient.RecognizeImageByName(grpcRequest, cancellationToken: ct);

                if (response.Success == false)
                {
                    await Send.OkAsync(new { Success = false, ErrorMessage = response.ErrorMessage }, cancellation: ct);
                }

                //TODO: log result 
                await Send.OkAsync(response.RecognizedDataJson, cancellation: ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                if (!HttpContext.Response.HasStarted)
                {
                    await Send.NotFoundAsync(ct);
                }
            }
        }
    }
}