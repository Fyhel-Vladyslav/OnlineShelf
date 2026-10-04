namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

public sealed class OutfitGenerationOptions
{
    public const string SectionName = "OutfitGeneration";

    /// <summary>Скільки часткових образів лишається після кожного кроку beam search.</summary>
    public int BeamWidth { get; set; } = 20;

    /// <summary>Скільки кандидатів першого слота беремо в пошук (обулені, бо на першому кроці ще нема що оцінювати).</summary>
    public int MaxFirstSlotCandidates { get; set; } = 50;

    public int DefaultTopN { get; set; } = 5;
    public int MaxTopN { get; set; } = 20;

    /// <summary>Допустима відстань (у фазах 1..12) між сезоном речі та поточним сезоном.</summary>
    public int SeasonTolerance { get; set; } = 2;

    /// <summary>Нижче цієї температури верхній одяг обов'язковий.</summary>
    public double OuterwearRequiredBelowCelsius { get; set; } = 12;

    /// <summary>Вище цієї температури верхній одяг не пропонується.</summary>
    public double OuterwearForbiddenAboveCelsius { get; set; } = 22;

    /// <summary>Мапінг назви слота (OutfitSlot) → ключі AttributeType з довідника ShelfsService.</summary>
    public Dictionary<string, int[]> Slots { get; set; } = new();
}
