namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

public sealed class GnnScorerOptions
{
    public const string SectionName = "Gnn";

    public required string ModelPath { get; init; }
}
