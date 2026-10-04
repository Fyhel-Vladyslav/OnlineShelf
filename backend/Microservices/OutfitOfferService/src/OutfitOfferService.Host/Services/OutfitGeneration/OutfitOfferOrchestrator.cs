using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

public sealed record GenerateOfferCommand
{
    public required Guid UserId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IReadOnlyCollection<Guid> IncludeItemIds { get; init; } = [];
    public IReadOnlyCollection<Guid> ExcludeItemIds { get; init; } = [];
    public bool OnlyFavorite { get; init; }
    public int? TopN { get; init; }
}

public sealed record OfferResult(
    IReadOnlyList<ScoredOutfit> Outfits,
    WeatherSnapshot? Weather,
    int SeasonPhase,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;
}

public interface IOutfitOfferOrchestrator
{
    Task<OfferResult> GenerateAsync(GenerateOfferCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Сценарій Б: погода → гардероб (Shelfs) → прохід 1 (обмеження C_j) → прохід 2 (beam search + скор моделі) → top-N.
/// </summary>
public sealed class OutfitOfferOrchestrator(
    IWardrobeProvider wardrobeProvider,
    IWeatherService weatherService,
    IConstraintFilter constraintFilter,
    IOutfitGenerator outfitGenerator,
    IOptions<OutfitGenerationOptions> options,
    TimeProvider timeProvider,
    ILogger<OutfitOfferOrchestrator> logger) : IOutfitOfferOrchestrator
{
    public async Task<OfferResult> GenerateAsync(GenerateOfferCommand command, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var warnings = new List<string>();

        var weatherTask = GetWeatherAsync(command, warnings, cancellationToken);
        var wardrobeTask = wardrobeProvider.GetWardrobeAsync(command.UserId, command.OnlyFavorite, cancellationToken);
        await Task.WhenAll(weatherTask, wardrobeTask);

        var weather = weatherTask.Result;
        var wardrobe = wardrobeTask.Result;
        var seasonPhase = SeasonCalculator.CurrentPhase(timeProvider.GetUtcNow().UtcDateTime, command.Latitude);

        var missingIncludes = command.IncludeItemIds.Where(id => wardrobe.All(i => i.Id != id)).ToList();
        if (missingIncludes.Count > 0)
        {
            return Fail($"Included items not found in the user's wardrobe: {string.Join(", ", missingIncludes)}");
        }

        var withoutEmbedding = wardrobe.Count(i => i.VisualEmbedding.Length == 0);
        if (withoutEmbedding > 0)
        {
            warnings.Add($"{withoutEmbedding} of {wardrobe.Count} items have no visual embedding yet; they are scored by attributes only.");
        }

        var context = new OfferContext
        {
            Weather = weather,
            SeasonPhase = seasonPhase,
            IncludeItemIds = command.IncludeItemIds.ToHashSet(),
            ExcludeItemIds = command.ExcludeItemIds.ToHashSet(),
        };

        var pool = constraintFilter.Apply(wardrobe, context);
        warnings.AddRange(pool.Warnings);
        if (pool.Errors.Count > 0)
        {
            return new OfferResult([], weather, seasonPhase, warnings, pool.Errors);
        }

        var topN = Math.Clamp(command.TopN ?? settings.DefaultTopN, 1, settings.MaxTopN);
        var generation = await outfitGenerator.GenerateAsync(pool, topN, cancellationToken);
        warnings.AddRange(generation.Warnings);

        logger.LogInformation(
            "Generated {OutfitCount} outfits for user {UserId} from {WardrobeCount} items (season phase {SeasonPhase}, weather {Weather})",
            generation.Outfits.Count, command.UserId, wardrobe.Count, seasonPhase, weather?.Condition);

        return new OfferResult(generation.Outfits, weather, seasonPhase, warnings, []);

        OfferResult Fail(string error) => new([], weather, seasonPhase, warnings, [error]);
    }

    private async Task<WeatherSnapshot?> GetWeatherAsync(GenerateOfferCommand command, List<string> warnings, CancellationToken ct)
    {
        if (command.Latitude is not { } lat || command.Longitude is not { } lon)
        {
            warnings.Add("No coordinates provided; weather constraints are not applied.");
            return null;
        }

        try
        {
            return await weatherService.GetCurrentWeatherAsync(lat, lon, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Погода — не критична: без неї образ все одно підбирається, лише без погодних обмежень
            logger.LogWarning(ex, "Weather is unavailable, continuing without weather constraints");
            warnings.Add("Weather service is unavailable; weather constraints are not applied.");
            return null;
        }
    }
}
