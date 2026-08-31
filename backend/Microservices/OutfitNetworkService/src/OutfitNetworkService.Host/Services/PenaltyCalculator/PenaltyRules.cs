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

    // TODO: заповнити реальними парами з дизайнерського довідника кольорових конфліктів.
    // Значення (colorA, colorB) неспрямовані — перевіряються в обидва боки в IsClashing.
    private static readonly HashSet<(int, int)> ClashingColorPairs = new();

    public double Evaluate(OutfitGraph graph)
    {
        foreach (var (sourceIndex, targetIndex) in graph.Edges)
        {
            var source = graph.Nodes[sourceIndex];
            var target = graph.Nodes[targetIndex];

            if (IsClashing(source, target))
                return 0.0; // категорична несумісність — множник обвалює весь добуток Π C_j
        }

        return 1.0;
    }

    private static bool IsClashing(ItemNode a, ItemNode b)
    {
        return ClashingColorPairs.Contains((a.AttributeColorMain, b.AttributeColorMain))
            || ClashingColorPairs.Contains((b.AttributeColorMain, a.AttributeColorMain))
            || ClashingColorPairs.Contains((a.AttributeColorMain, b.AttributeColorSecond))
            || ClashingColorPairs.Contains((a.AttributeColorSecond, b.AttributeColorMain));
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