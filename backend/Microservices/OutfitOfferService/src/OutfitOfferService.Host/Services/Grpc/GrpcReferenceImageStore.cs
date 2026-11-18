using Google.Protobuf;
using Grpc.Core;
using ImageService.Protos;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Grpc;

/// <summary>Фото еталонних образів у ImageService: завантаження, читання оригіналу, видалення.</summary>
public sealed class GrpcReferenceImageStore(ImageProcessor.ImageProcessorClient client) : IReferenceImageStore
{
    private const string BigSuffix = "_big.webp";
    private const string OriginalSuffix = "_orig";

    public async Task<(string BigImage, string SmallImage)?> UploadAsync(byte[] imageBytes, string fileName, CancellationToken ct)
    {
        var response = await client.UploadImageAsync(
            new UploadImageRequest { Data = ByteString.CopyFrom(imageBytes), FileName = fileName },
            cancellationToken: ct);
        return response.Success ? (response.BigImageName, response.SmallImageName) : null;
    }

    /// <summary>
    /// Оригінальні байти ({id}_orig): саме їх бачить Parse і саме в їхніх пікселях задано рамки. Для фото без оригіналу
    /// береться WebP. NotFound для самого фото прокидається далі.
    /// </summary>
    public async Task<byte[]> DownloadOriginalAsync(string bigImage, CancellationToken ct)
    {
        if (bigImage.EndsWith(BigSuffix, StringComparison.Ordinal))
        {
            try
            {
                return await DownloadAsync(bigImage[..^BigSuffix.Length] + OriginalSuffix, ct);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // оригінал не збережено: нижче візьмемо WebP
            }
        }

        return await DownloadAsync(bigImage, ct);
    }

    public async Task DeleteAsync(string bigImage, string smallImage, CancellationToken ct) =>
        await client.DeleteImageAsync(
            new DeleteImageRequest { BigImageName = bigImage, SmallImageName = smallImage }, cancellationToken: ct);

    private async Task<byte[]> DownloadAsync(string fileName, CancellationToken ct)
    {
        using var call = client.GetPhotoByName(new GetPhotoRequest { FileName = fileName }, cancellationToken: ct);
        using var stream = new MemoryStream();
        await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
        {
            await stream.WriteAsync(chunk.ChunkData.Memory, ct);
        }

        return stream.ToArray();
    }
}
