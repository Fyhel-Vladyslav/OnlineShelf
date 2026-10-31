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

    /// <summary>
    /// Фото для візуального ембедингу: оригінальні байти ({id}_orig, ImageService зберігає їх з 2026-10-25),
    /// тобто той самий вхід, що отримує Parse при створенні речі. Для старих речей без оригіналу — {id}_big.webp.
    /// </summary>
    public static async Task<byte[]> DownloadEmbeddingSourceAsync(
        this ImageProcessor.ImageProcessorClient client, string bigImage, CancellationToken ct)
    {
        var original = OriginalImageName(bigImage);
        if (original is not null)
        {
            try
            {
                return await client.DownloadPhotoAsync(original, ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // річ завантажена до появи оригіналів — беремо WebP
            }
        }

        return await client.DownloadPhotoAsync(bigImage, ct);
    }

    /// <summary>{id}_big.webp → {id}_orig (домовленість з ImageService.ImageGrpcService); інша назва → null.</summary>
    public static string? OriginalImageName(string bigImage)
    {
        const string bigSuffix = "_big.webp";
        return bigImage.EndsWith(bigSuffix, StringComparison.Ordinal) ? bigImage[..^bigSuffix.Length] + "_orig" : null;
    }
}
