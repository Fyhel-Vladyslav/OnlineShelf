using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;

namespace ShelfsService.src.ShelfsService.Host.Helpers;

public static class ItemEmbeddingExtensions
{
    /// <summary>
    /// Рахує візуальний ембединг фото через ParseImageService і записує його в айтем.
    /// Помилка ембединга не блокує збереження речі: айтем просто лишається без вектора (його можна дорахувати пізніше).
    /// </summary>
    public static async Task ApplyVisualEmbeddingAsync(
        this IImageAnalyzer analyzer, Item item, byte[] imageBytes, ILogger logger, CancellationToken ct)
    {
        var response = await analyzer.EmbedAsync(imageBytes, ct);

        if (!response.Success || response.Embedding.Count == 0)
        {
            logger.LogWarning("Visual embedding for item {ItemId} was not computed: {Error}", item.Id, response.ErrorMessage);
            item.VisualEmbedding = null;
            item.EmbeddingModel = null;
            return;
        }

        item.VisualEmbedding = response.Embedding.ToArray();
        item.EmbeddingModel = response.EmbeddingModel;
    }
}
