using FastEndpoints;
using Grpc.Core;
using ImageService.Protos;
using ShelfsService.src.ShelfsService.Common;
using System.IO;

namespace ShelfsService.src.ShelfsService.Host.Features.Items;

internal sealed record GetImageByName(string ImageName);

internal sealed class GetPhotoByName : Endpoint<GetImageByName>
{
    private readonly ImageProcessor.ImageProcessorClient _imageClient;

    public GetPhotoByName(ImageProcessor.ImageProcessorClient imageClient)
    {
        _imageClient = imageClient;
    }

    public override void Configure()
    {
        Get(ApiRoutes.GetPhotoByName);
        // Policies("AdminPolicy");
        AllowAnonymous();
    }

        public override async Task<object?> ExecuteAsync(GetImageByName req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ImageName))
        {
            await Send.NotFoundAsync(ct);
            return null;
        }

        var grpcRequest = new GetPhotoRequest { FileName = req.ImageName };

        try
        {
            using var call = _imageClient.GetPhotoByName(grpcRequest, cancellationToken: ct);

            var extension = Path.GetExtension(req.ImageName).ToLowerInvariant();
            HttpContext.Response.ContentType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/jpeg"
            };

            await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
            {
                await HttpContext.Response.Body.WriteAsync(response.ChunkData.Memory, ct);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            if (!HttpContext.Response.HasStarted)
            {
                await Send.NotFoundAsync(ct);
            }
        }

        return null; // Return null as the method requires a Task<object?> return type
    }
}