using Clothing;

namespace ShelfsService.src.ShelfsService.Common.Interfaces;

public interface IImageAnalyzer
{
    Task<AnalyzeClothingResponse> AnalyzeAsync(byte[] imageBytes);

    Task<EmbedClothingResponse> EmbedAsync(byte[] imageBytes, CancellationToken ct = default);
}
