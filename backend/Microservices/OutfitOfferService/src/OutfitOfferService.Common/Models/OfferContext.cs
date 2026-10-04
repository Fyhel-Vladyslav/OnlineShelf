using OutfitOfferService.OutfitOfferService.Common.Enums;

namespace OutfitOfferService.src.OutfitOfferService.Common.Models;

public sealed record WeatherSnapshot(Weather Condition, double TemperatureCelsius);

/// <summary>Контекст запиту на генерацію: погода, сезон, побажання користувача (Include/Exclude).</summary>
public sealed record OfferContext
{
    public WeatherSnapshot? Weather { get; init; }

    /// <summary>Поточна фаза сезону 1..12 у тих самих одиницях, що й AttributeSeason (1 = рання зима … 12 = пізня осінь).</summary>
    public required int SeasonPhase { get; init; }

    public IReadOnlySet<Guid> IncludeItemIds { get; init; } = new HashSet<Guid>();
    public IReadOnlySet<Guid> ExcludeItemIds { get; init; } = new HashSet<Guid>();
}
