using Grpc.Core;
using ImageService.Protos;

namespace ShelfsService.src.ShelfsService.Host.Helpers;

public static class ImageProcessorClientExtensions
{
    /// <summary>
    /// Завантажує фото з ImageService повністю (склеює чанки стріму GetPhotoByName).
    /// RpcException(NotFound) прокидається далі — викликач сам вирішує, що робити з відсутнім файлом.
    /// </summary>
    public static async Task<byte[]> DownloadPhotoAsync(
        this ImageProcessor.ImageProcessorClient client, string fileName, CancellationToken ct)
    {
        using var call = client.GetPhotoByName(new GetPhotoRequest { FileName = fileName }, cancellationToken: ct);
        using var ms = new MemoryStream();

        await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
        {
            await ms.WriteAsync(chunk.ChunkData.Memory, ct);
        }

        return ms.ToArray();
    }
}
