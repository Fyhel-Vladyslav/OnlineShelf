
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;

/// <summary>
/// Одне жорстке/евристичне правило штрафу, що не покладається на GNN —
/// детерміноване бізнес-правило, явно прописане дизайнерами/продуктом
/// (на відміну від learned-скору, який дає GNN).
/// Повертає множник C_j в діапазоні [0; 1]: 1 = без штрафу, 0 = категорична несумісність.
/// </summary>
public interface IPenaltyRule
{
    string Name { get; }

    double Evaluate(OutfitGraph graph);
}
