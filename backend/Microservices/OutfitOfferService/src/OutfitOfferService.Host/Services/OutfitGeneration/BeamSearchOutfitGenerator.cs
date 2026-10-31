using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

public sealed record OutfitGenerationResult(IReadOnlyList<ScoredOutfit> Outfits, IReadOnlyList<string> Warnings);

public interface IOutfitGenerator
{
    Task<OutfitGenerationResult> GenerateAsync(CandidatePool pool, int topN, CancellationToken cancellationToken = default);
}

/// <summary>
/// Прохід 2: побудова образів beam search'ем по слотах шаблону з оцінкою часткових образів моделлю (IOutfitScorer).
/// Шаблони: ⟨top, bottom, shoes, outerwear?⟩ і ⟨full-body, shoes, outerwear?⟩.
/// На кожному кроці всі розширення оцінюються одним пакетним викликом, лишаються BeamWidth найкращих
/// (після останнього кроку — усі, щоб відбору топу було з чого обирати різні образи).
/// Топ відбирається жадібно зі штрафом за повтор речей (DiversityPenalty), а повертається за спаданням скору.
/// </summary>
public sealed class BeamSearchOutfitGenerator(IOutfitScorer scorer, IOptions<OutfitGenerationOptions> options) : IOutfitGenerator
{
    private static readonly OutfitSlot[][] Templates =
    [
        [OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes, OutfitSlot.Outerwear],
        [OutfitSlot.FullBody, OutfitSlot.Shoes, OutfitSlot.Outerwear],
    ];

    private sealed record Step(OutfitSlot Slot, IReadOnlyList<WardrobeItem> Options, bool IsOptional);

    private sealed record State(IReadOnlyList<SlottedItem> Items, OutfitScore? Score);

    public async Task<OutfitGenerationResult> GenerateAsync(CandidatePool pool, int topN, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var outfits = new List<ScoredOutfit>();

        foreach (var template in Templates)
        {
            var steps = BuildSteps(template, pool, warnings);
            if (steps is null)
            {
                continue;
            }

            outfits.AddRange(await RunBeamAsync(steps, pool, cancellationToken));
        }

        if (outfits.Count == 0)
        {
            warnings.Add("No outfit could be composed: the wardrobe lacks a top + bottom or a full-body item that satisfies the constraints.");
        }

        var candidates = outfits
            .DistinctBy(o => string.Join('|', o.Items.Select(i => i.Item.Id).Order()))
            .OrderByDescending(o => o.Score.FinalScore)
            .ToList();

        var pinned = pool.Pinned.Values.Concat(pool.PinnedAccessories).Select(i => i.Id).ToHashSet();
        var best = SelectDiverse(candidates, topN, options.Value.DiversityPenalty, pinned);

        return new OutfitGenerationResult(best, warnings.Distinct().ToList());
    }

    /// <summary>
    /// Жадібний відбір топу (у дусі MMR): на кожному кроці береться кандидат з найбільшим
    /// FinalScore × penalty^(скільки разів його речі вже трапились в обраних образах).
    /// Так одна «універсальна» річ (нейтральний светр, базові кросівки) не займає весь топ,
    /// а перший образ завжди найкращий за скором. Закріплені речі є в усіх образах і не штрафуються.
    /// </summary>
    public static List<ScoredOutfit> SelectDiverse(
        IReadOnlyList<ScoredOutfit> candidates, int topN, double penalty, IReadOnlySet<Guid> pinnedItemIds)
    {
        var usage = new Dictionary<Guid, int>();
        var remaining = candidates.OrderByDescending(o => o.Score.FinalScore).ToList();
        var selected = new List<ScoredOutfit>();

        while (selected.Count < topN && remaining.Count > 0)
        {
            var bestIndex = 0;
            var bestValue = double.NegativeInfinity;
            for (var k = 0; k < remaining.Count; k++)
            {
                var repeats = remaining[k].Items
                    .Where(i => !pinnedItemIds.Contains(i.Item.Id))
                    .Sum(i => usage.GetValueOrDefault(i.Item.Id));
                var value = remaining[k].Score.FinalScore * Math.Pow(penalty, repeats);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestIndex = k;
                }
            }

            var chosen = remaining[bestIndex];
            remaining.RemoveAt(bestIndex);
            selected.Add(chosen);
            foreach (var item in chosen.Items)
                usage[item.Item.Id] = usage.GetValueOrDefault(item.Item.Id) + 1;
        }

        // Різноманітність впливає на те, ЯКІ образи в топі; показуємо їх, як і раніше, від найкращого
        return selected.OrderByDescending(o => o.Score.FinalScore).ToList();
    }

    /// <summary>Кроки шаблону або null, якщо шаблон неможливий (немає речей для основного слота чи конфлікт із Include).</summary>
    private static List<Step>? BuildSteps(OutfitSlot[] template, CandidatePool pool, List<string> warnings)
    {
        // Закріплена річ зі слота, якого немає в шаблоні (напр. сукня для шаблону top+bottom) — шаблон не підходить
        if (pool.Pinned.Keys.Any(slot => !template.Contains(slot)))
        {
            return null;
        }

        var steps = new List<Step>();
        foreach (var slot in template)
        {
            var candidates = pool.For(slot);

            switch (slot)
            {
                case OutfitSlot.Outerwear:
                    if (pool.Outerwear == SlotRequirement.Forbidden && !pool.Pinned.ContainsKey(slot))
                        continue;
                    if (candidates.Count == 0)
                    {
                        if (pool.Outerwear == SlotRequirement.Required)
                            warnings.Add("Weather requires outerwear, but no suitable outerwear was found in the wardrobe.");
                        continue;
                    }
                    var optional = pool.Outerwear == SlotRequirement.Optional && !pool.Pinned.ContainsKey(slot);
                    steps.Add(new Step(slot, candidates, optional));
                    break;

                case OutfitSlot.Shoes:
                    if (candidates.Count == 0)
                    {
                        warnings.Add("No suitable shoes found in the wardrobe; outfits are composed without shoes.");
                        continue;
                    }
                    steps.Add(new Step(slot, candidates, IsOptional: false));
                    break;

                default:
                    if (candidates.Count == 0)
                        return null;
                    steps.Add(new Step(slot, candidates, IsOptional: false));
                    break;
            }
        }

        return steps;
    }

    private async Task<IEnumerable<ScoredOutfit>> RunBeamAsync(List<Step> steps, CandidatePool pool, CancellationToken ct)
    {
        var settings = options.Value;
        var accessories = pool.PinnedAccessories.Select(a => new SlottedItem(a, OutfitSlot.Accessory)).ToList();
        var states = new List<State> { new(accessories, null) };

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var stepOptions = i == 0 ? LimitFirstSlot(step.Options, settings.MaxFirstSlotCandidates) : step.Options;

            var expanded = states
                .SelectMany(state => stepOptions.Select(item =>
                    new State([.. state.Items, new SlottedItem(item, step.Slot)], null)))
                .ToList();

            if (step.IsOptional)
            {
                // Варіант «без цього слота» зберігає вже пораховані скори
                expanded.AddRange(states);
            }

            expanded = await ScoreUnscoredAsync(expanded, ct);

            // Після останнього кроку нічого не відкидаємо: усі оцінені образи — кандидати для різноманітного топу
            var isLast = i == steps.Count - 1;
            states = expanded.Any(s => s.Score is not null) && !isLast
                ? expanded.OrderByDescending(s => s.Score?.FinalScore ?? -1).Take(settings.BeamWidth).ToList()
                : expanded; // на першому кроці без аксесуарів оцінювати ще нічого — лишаємо всіх (вже обмежено LimitFirstSlot)
        }

        return states
            .Where(s => s.Score is not null && s.Items.Count >= 2)
            .Select(s => new ScoredOutfit(s.Items, s.Score!));
    }

    private async Task<List<State>> ScoreUnscoredAsync(List<State> states, CancellationToken ct)
    {
        var toScore = states
            .Select((state, index) => (state, index))
            .Where(x => x.state.Score is null && x.state.Items.Count >= 2)
            .ToList();

        if (toScore.Count == 0)
        {
            return states;
        }

        var scores = await scorer.ScoreAsync(
            toScore.Select(x => (IReadOnlyList<WardrobeItem>)x.state.Items.Select(i => i.Item).ToList()).ToList(),
            ct);

        var result = new List<State>(states);
        for (var k = 0; k < toScore.Count; k++)
        {
            result[toScore[k].index] = toScore[k].state with { Score = scores[k] };
        }

        return result;
    }

    // Улюблені речі — першими, щоб при обмеженні не губилися
    private static IReadOnlyList<WardrobeItem> LimitFirstSlot(IReadOnlyList<WardrobeItem> items, int max) =>
        items.Count <= max ? items : items.OrderByDescending(i => i.IsFavorite).Take(max).ToList();
}
