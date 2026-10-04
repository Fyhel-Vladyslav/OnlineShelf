using Microsoft.Extensions.Options;
using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

public enum SlotRequirement
{
    Optional,
    Required,
    Forbidden
}

/// <summary>Результат проходу 1 (булеві обмеження C_j): які речі взагалі можуть потрапити в кожен слот.</summary>
public sealed class CandidatePool
{
    public Dictionary<OutfitSlot, List<WardrobeItem>> Candidates { get; } = new();

    /// <summary>Речі з Include, закріплені за своїм слотом (не підлягають заміні).</summary>
    public Dictionary<OutfitSlot, WardrobeItem> Pinned { get; } = new();

    /// <summary>Аксесуари з Include: додаються до кожного образу й беруть участь в оцінці.</summary>
    public List<WardrobeItem> PinnedAccessories { get; } = new();

    public SlotRequirement Outerwear { get; set; } = SlotRequirement.Optional;

    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public IReadOnlyList<WardrobeItem> For(OutfitSlot slot) =>
        Pinned.TryGetValue(slot, out var pinned) ? [pinned]
        : Candidates.TryGetValue(slot, out var list) ? list
        : [];
}

public interface IConstraintFilter
{
    CandidatePool Apply(IReadOnlyList<WardrobeItem> wardrobe, OfferContext context);
}

/// <summary>
/// Прохід 1 математичної моделі: жорсткі (булеві) обмеження, що відсікають речі до оцінки моделлю.
/// Exclude, невідомий тип, невідповідний сезон, погодні вимоги до верхнього одягу.
/// </summary>
public sealed class ConstraintFilter(ISlotResolver slotResolver, IOptions<OutfitGenerationOptions> options) : IConstraintFilter
{
    public CandidatePool Apply(IReadOnlyList<WardrobeItem> wardrobe, OfferContext context)
    {
        var pool = new CandidatePool();
        var settings = options.Value;

        pool.Outerwear = ResolveOuterwearRequirement(context.Weather, settings);

        foreach (var itemId in context.IncludeItemIds.Where(context.ExcludeItemIds.Contains))
        {
            pool.Warnings.Add($"Item {itemId} is both included and excluded; include wins.");
        }

        foreach (var item in wardrobe)
        {
            var slot = slotResolver.Resolve(item.AttributeType);
            var isIncluded = context.IncludeItemIds.Contains(item.Id);

            if (isIncluded)
            {
                Pin(pool, item, slot);
                continue;
            }

            if (context.ExcludeItemIds.Contains(item.Id)
                || slot is OutfitSlot.Unknown or OutfitSlot.Accessory
                || !IsSeasonCompatible(item, context.SeasonPhase, settings.SeasonTolerance)
                || (slot == OutfitSlot.Outerwear && pool.Outerwear == SlotRequirement.Forbidden))
            {
                continue;
            }

            if (!pool.Candidates.TryGetValue(slot, out var list))
            {
                pool.Candidates[slot] = list = new List<WardrobeItem>();
            }
            list.Add(item);
        }

        ValidatePins(pool);
        return pool;
    }

    private static void Pin(CandidatePool pool, WardrobeItem item, OutfitSlot slot)
    {
        switch (slot)
        {
            case OutfitSlot.Unknown:
                pool.Errors.Add($"Included item {item.Id} ('{item.Name}') has unknown type {item.AttributeType} and cannot be placed in an outfit.");
                return;
            case OutfitSlot.Accessory:
                pool.PinnedAccessories.Add(item);
                return;
        }

        if (!pool.Pinned.TryAdd(slot, item))
        {
            pool.Errors.Add($"Only one {slot} item can be included, got '{pool.Pinned[slot].Name}' and '{item.Name}'.");
            return;
        }

        if (slot == OutfitSlot.Outerwear && pool.Outerwear == SlotRequirement.Forbidden)
        {
            // Явне побажання користувача сильніше за погодне правило
            pool.Outerwear = SlotRequirement.Optional;
            pool.Warnings.Add($"Outerwear '{item.Name}' is included although it is too warm for outerwear.");
        }
    }

    private static void ValidatePins(CandidatePool pool)
    {
        if (pool.Pinned.ContainsKey(OutfitSlot.FullBody)
            && (pool.Pinned.ContainsKey(OutfitSlot.Top) || pool.Pinned.ContainsKey(OutfitSlot.Bottom)))
        {
            pool.Errors.Add("A full-body item (e.g. dress) cannot be included together with a separate top or bottom.");
        }
    }

    private static bool IsSeasonCompatible(WardrobeItem item, int seasonPhase, int tolerance) =>
        item.AttributeSeason is < 1 or > SeasonCalculator.PhaseCount   // сезон не вказано — річ всесезонна
        || SeasonCalculator.Distance(item.AttributeSeason, seasonPhase) <= tolerance;

    public static SlotRequirement ResolveOuterwearRequirement(WeatherSnapshot? weather, OutfitGenerationOptions settings)
    {
        if (weather is null)
        {
            return SlotRequirement.Optional;
        }

        if (weather.Condition is Weather.Snowy || weather.TemperatureCelsius < settings.OuterwearRequiredBelowCelsius)
        {
            return SlotRequirement.Required;
        }

        if (weather.TemperatureCelsius > settings.OuterwearForbiddenAboveCelsius
            && weather.Condition is not (Weather.Rainy or Weather.Thunderstorm))
        {
            return SlotRequirement.Forbidden;
        }

        return SlotRequirement.Optional;
    }
}
