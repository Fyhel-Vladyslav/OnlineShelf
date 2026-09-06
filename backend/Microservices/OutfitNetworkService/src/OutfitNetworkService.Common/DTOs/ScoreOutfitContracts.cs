namespace OutfitNetworkService.src.OutfitNetworkService.Common.DTOs;

public sealed class ScoreOutfitRequest
{
    public required List<CandidateItemDto> Items { get; init; }
}

public sealed class CandidateItemDto
{
    public required string ItemId { get; init; }
    public required int AttributeType { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsPinned { get; init; }
    public required float[] FeatureVector { get; init; }
    public required string AttributeColorMain { get; init; }
    public required string AttributeColorSecond { get; init; }
    public required int AttributeSeason { get; init; }
    public required int AttributePattern { get; init; }
    public required int AttributeMatterial { get; init; }
}

public sealed class ScoreOutfitResponse
{
    public required double FinalScore { get; init; }
    public required double GraphLevelScore { get; init; }
    public required double PenaltyMultiplier { get; init; }
    public required Dictionary<string, double> AppliedPenalties { get; init; }
}

