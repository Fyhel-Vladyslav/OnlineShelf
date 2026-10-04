namespace OutfitOfferService.src.OutfitOfferService.Common.Models;

/// <summary>Річ із гардероба користувача, як її бачить бізнес-логіка підбору образу.</summary>
public sealed record WardrobeItem
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? BigImage { get; init; }
    public bool IsFavorite { get; init; }

    public string ColorMain { get; init; } = string.Empty;
    public string ColorSecond { get; init; } = string.Empty;
    public int AttributeType { get; init; }
    public int AttributeSeason { get; init; }
    public int AttributePattern { get; init; }
    public int AttributeMaterial { get; init; }

    /// <summary>Візуальний ембединг з ShelfsService; порожній, якщо ще не пораховано.</summary>
    public float[] VisualEmbedding { get; init; } = [];
    public string? EmbeddingModel { get; init; }

    /// <summary>Віртуальна річ (Gap Analysis): її немає в гардеробі, система пропонує докупити.</summary>
    public bool IsVirtual { get; init; }
}
