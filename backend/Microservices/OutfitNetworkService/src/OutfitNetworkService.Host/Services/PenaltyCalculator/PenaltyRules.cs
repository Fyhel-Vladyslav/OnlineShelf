using Microsoft.Extensions.Options;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;

/// <summary>
/// Штраф за категорично несумісні пари кольорів, визначені дизайнерами
/// (наприклад, конкретні відтінки, що клешують за правилами кольорового кола).
/// Це свідомо детерміноване правило на рівні атрибутів, а не навчене на ембедингах.
/// </summary>
public sealed class ColorClashPenaltyRule : IPenaltyRule
{
    public string Name => nameof(ColorClashPenaltyRule);

    private readonly IOptionsMonitor<PenaltyRulesOptions> _options;
    private HashSet<(string, string)> _clashingPairs;

    public ColorClashPenaltyRule(IOptionsMonitor<PenaltyRulesOptions> options)
    {
        _options = options;
        _clashingPairs = BuildLookup(options.CurrentValue);
        _options.OnChange(updated => _clashingPairs = BuildLookup(updated));
    }

    private static HashSet<(string, string)> BuildLookup(PenaltyRulesOptions options) =>
        options.ClashingColorPairs
            .SelectMany(p => new[]
            {
                (Normalize(p.ColorA), Normalize(p.ColorB)),
                (Normalize(p.ColorB), Normalize(p.ColorA))
            })
            .ToHashSet();

    private static string Normalize(string color) => color.ToLowerInvariant();

    public double Evaluate(OutfitGraph graph)
    {
        foreach (var (sourceIndex, targetIndex) in graph.Edges)
        {
            var source = graph.Nodes[sourceIndex];
            var target = graph.Nodes[targetIndex];
            if (IsClashing(source, target))
                return 0.0;
        }
        return 1.0;
    }

    private bool IsClashing(ItemNode a, ItemNode b) =>
        Check(a.AttributeColorMain, b.AttributeColorMain)
        || Check(a.AttributeColorMain, b.AttributeColorSecond)
        || Check(a.AttributeColorSecond, b.AttributeColorMain)
        || Check(a.AttributeColorSecond, b.AttributeColorSecond);

    private bool Check(string? colorA, string? colorB)
    {
        if (string.IsNullOrEmpty(colorA) || string.IsNullOrEmpty(colorB))
            return false;

        return _clashingPairs.Contains((Normalize(colorA), Normalize(colorB)));
    }
}

/// <summary>
/// Легкий штраф за наявність віртуальних (ще не куплених) айтемів у образі —
/// система менш впевнена в остаточному вигляді, тож трохи занижує довіру до скору
/// пропорційно кількості віртуальних елементів.
/// </summary>
public sealed class VirtualItemPenaltyRule : IPenaltyRule
{
    public string Name => nameof(VirtualItemPenaltyRule);

    private const double PenaltyPerVirtualItem = 0.9;

    public double Evaluate(OutfitGraph graph)
    {
        var virtualCount = graph.Nodes.Count(n => n.IsVirtual);
        return Math.Pow(PenaltyPerVirtualItem, virtualCount);
    }
}