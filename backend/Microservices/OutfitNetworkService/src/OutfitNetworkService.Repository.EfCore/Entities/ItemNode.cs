namespace OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;
/// <summary>
/// Вузол графа сумісності — один айтем гардероба (реальний або віртуальний,
/// підставлений на етапі Gap Analysis) у контексті конкретного кандидата-образу.
/// </summary>
public sealed class ItemNode
{
    public required string ItemId { get; init; }

    /// <summary>Відповідає AttributeType з WardrobeItemDto (верх, низ, взуття тощо).</summary>
    public required int AttributeType { get; init; }

    /// <summary>true — підставлений шаблон із Gap Analysis, а не реальна річ користувача.</summary>
    public required bool IsVirtual { get; init; }

    /// <summary>Жорстко зафіксований користувачем айтем (IncludeItems) — не підлягає заміні на етапі candidate generation.</summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Об'єднаний ембединг айтема (категоріальна частина + візуальна CLIP-частина),
    /// порахований і закешований у Parse Image Service — сюди приходить вже готовим.
    /// </summary>
    public required float[] FeatureVector { get; init; }

    // Сирі атрибути з WardrobeItemDto — дублюються тут навмисно, щоб детерміновані
    // IPenaltyRule (наприклад, ColorClashPenaltyRule) не смикали Shelfs Service
    // окремим запитом за кожен айтем на кожен виклик scoring. Заповнюються один раз
    // ще на етапі Candidate Generation, разом із FeatureVector.
    public required int AttributeColorMain { get; init; }
    public required int AttributeColorSecond { get; init; }
    public required int AttributeSeason { get; init; }
    public required int AttributePattern { get; init; }
    public required int AttributeMatterial { get; init; }
}
