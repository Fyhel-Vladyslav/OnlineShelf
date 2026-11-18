using Microsoft.Extensions.Logging.Abstractions;
using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;
using static OutfitOfferService.Tests.TestData;

namespace OutfitOfferService.Tests;

public class OutfitOfferOrchestratorTests
{
    private sealed class FakeWardrobe(params WardrobeItem[] items) : IWardrobeProvider
    {
        public Task<IReadOnlyList<WardrobeItem>> GetWardrobeAsync(Guid userId, bool onlyFavorite, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WardrobeItem>>(items.Where(i => !onlyFavorite || i.IsFavorite).ToList());
    }

    private sealed class FakeWeather(WeatherSnapshot? snapshot) : IWeatherService
    {
        public Task<WeatherSnapshot> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default) =>
            snapshot is null ? throw new HttpRequestException("provider down") : Task.FromResult(snapshot);
    }

    private sealed class FixedTime(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private static OutfitOfferOrchestrator Create(IWardrobeProvider wardrobe, IWeatherService weather, FakeScorer? scorer = null)
    {
        var options = Wrap(Options());
        return new OutfitOfferOrchestrator(
            wardrobe,
            weather,
            Filter(),
            new BeamSearchOutfitGenerator(scorer ?? new FakeScorer(), options),
            options,
            new FixedTime(new DateTime(2026, 7, 15)),
            NullLogger<OutfitOfferOrchestrator>.Instance);
    }

    private static readonly WardrobeItem Tee = Item("tee", Types.TShirt);
    private static readonly WardrobeItem Pants = Item("pants", Types.Pants);
    private static readonly WardrobeItem Sneakers = Item("sneakers", Types.Sneakers);

    [Fact]
    public async Task Generate_ReturnsOutfits_WithWeatherAndSeason()
    {
        var orchestrator = Create(new FakeWardrobe(Tee, Pants, Sneakers), new FakeWeather(new WeatherSnapshot(Weather.Clear, 24)));

        var result = await orchestrator.GenerateAsync(new GenerateOfferCommand { UserId = Guid.NewGuid(), Latitude = 50.6, Longitude = 26.2 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Outfits);
        Assert.Equal(Weather.Clear, result.Weather?.Condition);
        Assert.Equal(8, result.SeasonPhase); // середина липня — середина літа
    }

    [Fact]
    public async Task Generate_IncludedItemNotInWardrobe_IsError()
    {
        var orchestrator = Create(new FakeWardrobe(Tee, Pants, Sneakers), new FakeWeather(null));

        var result = await orchestrator.GenerateAsync(new GenerateOfferCommand
        {
            UserId = Guid.NewGuid(),
            IncludeItemIds = [Guid.NewGuid()],
        });

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Outfits);
    }

    [Fact]
    public async Task Generate_WeatherUnavailable_StillGenerates_WithWarning()
    {
        var orchestrator = Create(new FakeWardrobe(Tee, Pants, Sneakers), new FakeWeather(null));

        var result = await orchestrator.GenerateAsync(new GenerateOfferCommand { UserId = Guid.NewGuid(), Latitude = 50.6, Longitude = 26.2 });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Weather);
        Assert.Single(result.Outfits);
        Assert.Contains(result.Warnings, w => w.Contains("Weather service is unavailable"));
    }

    [Fact]
    public async Task Generate_ItemsWithoutEmbedding_AreReportedInWarnings()
    {
        var noEmbedding = Item("old tee", Types.TShirt) with { VisualEmbedding = [] };
        var orchestrator = Create(new FakeWardrobe(noEmbedding, Pants, Sneakers), new FakeWeather(null));

        var result = await orchestrator.GenerateAsync(new GenerateOfferCommand { UserId = Guid.NewGuid() });

        Assert.Contains(result.Warnings, w => w.StartsWith("1 of 3 items have no visual embedding"));
    }

    [Fact]
    public async Task Generate_TopN_IsClampedToConfiguredMaximum()
    {
        var wardrobe = Enumerable.Range(0, 6)
            .SelectMany(i => new[] { Item($"t{i}", Types.TShirt), Item($"b{i}", Types.Pants), Item($"s{i}", Types.Sneakers) })
            .ToArray();
        var orchestrator = Create(new FakeWardrobe(wardrobe), new FakeWeather(null));

        var result = await orchestrator.GenerateAsync(new GenerateOfferCommand { UserId = Guid.NewGuid(), TopN = 1000 });

        Assert.True(result.Outfits.Count <= Options().MaxTopN);
    }
}
