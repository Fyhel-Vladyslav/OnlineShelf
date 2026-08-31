using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;

/// <summary>Результат агрегації правил: підсумковий множник Π C_j + розбивка по кожному правилу.</summary>
public sealed record PenaltyCalculationResult(
    double TotalMultiplier,
    IReadOnlyDictionary<string, double> AppliedRules);

public interface IMultiplicativePenaltyCalculator
{
    PenaltyCalculationResult Calculate(OutfitGraph graph);
}

/// <summary>
/// Перемножує результати всіх зареєстрованих IPenaltyRule у єдиний множник Π C_j,
/// яким пізніше масштабується learned-скор від GNN.
/// </summary>
public sealed class MultiplicativePenaltyCalculator : IMultiplicativePenaltyCalculator
{
    private readonly IReadOnlyList<IPenaltyRule> _rules;

    public MultiplicativePenaltyCalculator(IEnumerable<IPenaltyRule> rules)
    {
        _rules = rules.ToList();
    }

    public PenaltyCalculationResult Calculate(OutfitGraph graph)
    {
        var applied = new Dictionary<string, double>();
        var product = 1.0;

        foreach (var rule in _rules)
        {
            var factor = rule.Evaluate(graph);
            applied[rule.Name] = factor;
            product *= factor;

            // Категорична несумісність (C_j = 0) — рахувати решту правил уже немає сенсу.
            if (product == 0.0)
                break;
        }

        return new PenaltyCalculationResult(product, applied);
    }
}
