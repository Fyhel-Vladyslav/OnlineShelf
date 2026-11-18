namespace OutfitOfferService.src.OutfitOfferService.Common.Models;

/// <summary>Попарна сумісність двох речей, яку повернула модель (для пояснення результату).</summary>
public sealed record PairwiseScore(string ItemIdA, string ItemIdB, double Score);

/// <summary>Скор образу: S = GraphLevelScore · Π C_j (див. OutfitNetworkService).</summary>
public sealed record OutfitScore(
    double FinalScore,
    double GraphLevelScore,
    double PenaltyMultiplier,
    IReadOnlyDictionary<string, double> AppliedPenalties,
    IReadOnlyList<PairwiseScore> PairwiseScores)
{
    public static readonly OutfitScore Empty = new(0, 0, 1, new Dictionary<string, double>(), []);
}

public sealed record SlottedItem(WardrobeItem Item, OutfitSlot Slot);

public sealed record ScoredOutfit(IReadOnlyList<SlottedItem> Items, OutfitScore Score);
