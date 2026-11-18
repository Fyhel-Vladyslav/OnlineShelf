using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Common.Interfaces;

/// <summary>Розбір фото людини на речі (ParseImageService.AnalyzeOutfit).</summary>
public interface IOutfitPhotoAnalyzer
{
    /// <summary>
    /// RpcException(Unavailable, DeadlineExceeded) означає тимчасову несправність і прокидається далі.
    /// Відмову Parse (success = false) повертає як результат.
    /// </summary>
    Task<OutfitAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken ct);
}

/// <summary>Сховище фото (ImageService): оригінал, велика і мала копії.</summary>
public interface IReferenceImageStore
{
    /// <summary>Повертає назви {id}_big.webp і {id}_small.webp або null, якщо файл не вдалося прочитати як картинку.</summary>
    Task<(string BigImage, string SmallImage)?> UploadAsync(byte[] imageBytes, string fileName, CancellationToken ct);

    /// <summary>Оригінальні байти ({id}_orig), а для фото без оригіналу — WebP.</summary>
    Task<byte[]> DownloadOriginalAsync(string bigImage, CancellationToken ct);

    Task DeleteAsync(string bigImage, string smallImage, CancellationToken ct);
}
