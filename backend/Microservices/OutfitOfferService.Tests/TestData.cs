using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

namespace OutfitOfferService.Tests;

/// <summary>Ключі AttributeType з довідника ShelfsService, які використовуються в тестах.</summary>
internal static class Types
{
    public const int TShirt = 1;
    public const int Shirt = 2;
    public const int Jacket = 6;
    public const int Coat = 7;
    public const int Pants = 8;
    public const int Skirt = 11;
    public const int Dress = 12;
    public const int Sneakers = 13;
    public const int Boots = 14;
    public const int Hat = 17;
    public const int Unknown = 99;
}

internal static class TestData
{
    public static OutfitGenerationOptions Options(Action<OutfitGenerationOptions>? configure = null)
    {
        var options = new OutfitGenerationOptions
        {
            BeamWidth = 10,
            MaxFirstSlotCandidates = 50,
            SeasonTolerance = 2,
            Slots = new Dictionary<string, int[]>
            {
                ["Top"] = [1, 2, 3, 4, 5],
                ["Outerwear"] = [6, 7],
                ["Bottom"] = [8, 9, 10, 11],
                ["FullBody"] = [12],
                ["Shoes"] = [13, 14, 15, 16, 21],
                ["Accessory"] = [17, 18, 19, 20, 22, 23, 24, 25],
            }
        };
        configure?.Invoke(options);
        return options;
    }

    public static IOptions<OutfitGenerationOptions> Wrap(OutfitGenerationOptions options) =>
        Microsoft.Extensions.Options.Options.Create(options);

    public static WardrobeItem Item(string name, int type, int season = 0, bool favorite = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        AttributeType = type,
        AttributeSeason = season,
        IsFavorite = favorite,
        VisualEmbedding = [0.1f, 0.2f],
    };

    public static ConstraintFilter Filter(OutfitGenerationOptions? options = null)
    {
        var wrapped = Wrap(options ?? Options());
        return new ConstraintFilter(new SlotResolver(wrapped), wrapped);
    }

    public static OfferContext Context(
        int seasonPhase = 8,
        WeatherSnapshot? weather = null,
        IEnumerable<Guid>? include = null,
        IEnumerable<Guid>? exclude = null) => new()
    {
        SeasonPhase = seasonPhase,
        Weather = weather,
        IncludeItemIds = (include ?? []).ToHashSet(),
        ExcludeItemIds = (exclude ?? []).ToHashSet(),
    };
}

/// <summary>
/// Скорер для тестів: скор образу = середнє попарних «симпатій» між речами (за замовчуванням 0.5).
/// Рахує кількість пакетних викликів, щоб перевіряти, що beam search не бомбить модель поштучно.
/// </summary>
internal sealed class FakeScorer : IOutfitScorer
{
    private readonly Dictionary<(Guid, Guid), double> _affinity = new();

    public int Calls { get; private set; }
    public int ScoredOutfits { get; private set; }

    public FakeScorer Likes(WardrobeItem a, WardrobeItem b, double score)
    {
        _affinity[(a.Id, b.Id)] = score;
        _affinity[(b.Id, a.Id)] = score;
        return this;
    }

    public Task<IReadOnlyList<OutfitScore>> ScoreAsync(
        IReadOnlyList<IReadOnlyList<WardrobeItem>> outfits, CancellationToken cancellationToken = default)
    {
        Calls++;
        ScoredOutfits += outfits.Count;

        IReadOnlyList<OutfitScore> scores = outfits.Select(outfit =>
        {
            Assert.True(outfit.Count >= 2, "Scorer must only receive outfits with at least 2 items");

            var pairs = (from a in outfit from b in outfit where a.Id != b.Id select _affinity.GetValueOrDefault((a.Id, b.Id), 0.5)).ToList();
            var score = pairs.Average();
            return new OutfitScore(score, score, 1, new Dictionary<string, double>(), []);
        }).ToList();

        return Task.FromResult(scores);
    }
}
