using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;
using static OutfitOfferService.Tests.TestData;

namespace OutfitOfferService.Tests;

public class BeamSearchOutfitGeneratorTests
{
    private static async Task<OutfitGenerationResult> Generate(
        FakeScorer scorer, IReadOnlyList<WardrobeItem> wardrobe, OfferContext? context = null, int topN = 5,
        OutfitGenerationOptions? options = null)
    {
        options ??= Options();
        var pool = Filter(options).Apply(wardrobe, context ?? Context());
        var generator = new BeamSearchOutfitGenerator(scorer, Wrap(options));
        return await generator.GenerateAsync(pool, topN);
    }

    private static HashSet<Guid> Ids(ScoredOutfit outfit) => outfit.Items.Select(i => i.Item.Id).ToHashSet();

    [Fact]
    public async Task Generate_ComposesTopBottomShoes_AndPicksBestCombinationByScore()
    {
        var tee = Item("tee", Types.TShirt);
        var shirt = Item("shirt", Types.Shirt);
        var pants = Item("pants", Types.Pants);
        var skirt = Item("skirt", Types.Skirt);
        var sneakers = Item("sneakers", Types.Sneakers);
        var boots = Item("boots", Types.Boots);

        var scorer = new FakeScorer()
            .Likes(shirt, skirt, 0.95)
            .Likes(shirt, boots, 0.9)
            .Likes(skirt, boots, 0.9)
            .Likes(tee, pants, 0.1);

        var result = await Generate(scorer, [tee, shirt, pants, skirt, sneakers, boots]);

        var best = result.Outfits[0];
        Assert.Equal(new HashSet<Guid> { shirt.Id, skirt.Id, boots.Id }, Ids(best));
        Assert.Equal(
            new[] { OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes },
            best.Items.Select(i => i.Slot));
        Assert.True(result.Outfits.Zip(result.Outfits.Skip(1)).All(p => p.First.Score.FinalScore >= p.Second.Score.FinalScore));
    }

    [Fact]
    public async Task Generate_ScoresEachBeamStepInOneBatch()
    {
        var wardrobe = new List<WardrobeItem>();
        for (var i = 0; i < 5; i++)
        {
            wardrobe.Add(Item($"top{i}", Types.TShirt));
            wardrobe.Add(Item($"bottom{i}", Types.Pants));
            wardrobe.Add(Item($"shoes{i}", Types.Sneakers));
        }
        var scorer = new FakeScorer();

        await Generate(scorer, wardrobe);

        // Крок 1 (лише верх) не оцінюється; крок 2 (верх+низ) і крок 3 (+взуття) — по одному пакету
        Assert.Equal(2, scorer.Calls);
    }

    [Fact]
    public async Task Generate_UsesFullBodyTemplate_ForDresses()
    {
        var dress = Item("dress", Types.Dress);
        var sneakers = Item("sneakers", Types.Sneakers);

        var result = await Generate(new FakeScorer(), [dress, sneakers]);

        var outfit = Assert.Single(result.Outfits);
        Assert.Equal(new HashSet<Guid> { dress.Id, sneakers.Id }, Ids(outfit));
    }

    [Fact]
    public async Task Generate_PinnedItem_AppearsInEveryOutfit()
    {
        var tee = Item("tee", Types.TShirt);
        var shirt = Item("shirt", Types.Shirt);
        var pants = Item("pants", Types.Pants);
        var skirt = Item("skirt", Types.Skirt);
        var sneakers = Item("sneakers", Types.Sneakers);
        var scorer = new FakeScorer().Likes(shirt, skirt, 0.99);

        var result = await Generate(scorer, [tee, shirt, pants, skirt, sneakers], Context(include: [tee.Id]));

        Assert.NotEmpty(result.Outfits);
        Assert.All(result.Outfits, o => Assert.Contains(tee.Id, Ids(o)));
    }

    [Fact]
    public async Task Generate_PinnedDress_SkipsTopBottomTemplate()
    {
        var dress = Item("dress", Types.Dress);
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);
        var sneakers = Item("sneakers", Types.Sneakers);

        var result = await Generate(new FakeScorer(), [dress, tee, pants, sneakers], Context(include: [dress.Id]));

        Assert.All(result.Outfits, o => Assert.Contains(dress.Id, Ids(o)));
        Assert.DoesNotContain(result.Outfits, o => Ids(o).Contains(tee.Id));
    }

    [Fact]
    public async Task Generate_ColdWeather_RequiresOuterwear()
    {
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);
        var boots = Item("boots", Types.Boots);
        var coat = Item("coat", Types.Coat);

        var result = await Generate(new FakeScorer(), [tee, pants, boots, coat],
            Context(weather: new WeatherSnapshot(Weather.Snowy, -5)));

        Assert.NotEmpty(result.Outfits);
        Assert.All(result.Outfits, o => Assert.Contains(coat.Id, Ids(o)));
    }

    [Fact]
    public async Task Generate_MildWeather_OuterwearIsOptional_ScoreDecides()
    {
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);
        var sneakers = Item("sneakers", Types.Sneakers);
        var jacket = Item("jacket", Types.Jacket);
        var scorer = new FakeScorer()
            .Likes(jacket, tee, 0.1).Likes(jacket, pants, 0.1).Likes(jacket, sneakers, 0.1);

        var result = await Generate(scorer, [tee, pants, sneakers, jacket],
            Context(weather: new WeatherSnapshot(Weather.Cloudy, 16)));

        Assert.Equal(2, result.Outfits.Count);
        Assert.DoesNotContain(jacket.Id, Ids(result.Outfits[0])); // без куртки скор вищий
        Assert.Contains(result.Outfits, o => Ids(o).Contains(jacket.Id));
    }

    [Fact]
    public async Task Generate_NoShoes_BuildsOutfitWithoutShoes_AndWarns()
    {
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);

        var result = await Generate(new FakeScorer(), [tee, pants]);

        var outfit = Assert.Single(result.Outfits);
        Assert.Equal(new HashSet<Guid> { tee.Id, pants.Id }, Ids(outfit));
        Assert.Contains(result.Warnings, w => w.Contains("shoes"));
    }

    [Fact]
    public async Task Generate_NoBottomAndNoDress_ReturnsNothing_WithWarning()
    {
        var tee = Item("tee", Types.TShirt);
        var sneakers = Item("sneakers", Types.Sneakers);

        var result = await Generate(new FakeScorer(), [tee, sneakers]);

        Assert.Empty(result.Outfits);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task Generate_PinnedAccessory_IsPartOfEveryOutfit()
    {
        var hat = Item("hat", Types.Hat);
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);
        var sneakers = Item("sneakers", Types.Sneakers);

        var result = await Generate(new FakeScorer(), [hat, tee, pants, sneakers], Context(include: [hat.Id]));

        var outfit = Assert.Single(result.Outfits);
        Assert.Contains(outfit.Items, i => i.Item.Id == hat.Id && i.Slot == OutfitSlot.Accessory);
    }

    [Fact]
    public async Task Generate_RespectsTopN_AndReturnsDistinctOutfits()
    {
        var wardrobe = new List<WardrobeItem>();
        for (var i = 0; i < 4; i++)
        {
            wardrobe.Add(Item($"top{i}", Types.TShirt));
            wardrobe.Add(Item($"bottom{i}", Types.Pants));
            wardrobe.Add(Item($"shoes{i}", Types.Sneakers));
        }

        var result = await Generate(new FakeScorer(), wardrobe, topN: 3);

        Assert.Equal(3, result.Outfits.Count);
        Assert.Equal(3, result.Outfits.Select(o => string.Join('|', Ids(o).Order())).Distinct().Count());
    }

    [Fact]
    public async Task Generate_BeamWidth_LimitsSurvivingPartialOutfits()
    {
        var wardrobe = new List<WardrobeItem>();
        for (var i = 0; i < 6; i++)
        {
            wardrobe.Add(Item($"top{i}", Types.TShirt));
            wardrobe.Add(Item($"bottom{i}", Types.Pants));
            wardrobe.Add(Item($"shoes{i}", Types.Sneakers));
        }
        var scorer = new FakeScorer();

        await Generate(scorer, wardrobe, options: Options(o => o.BeamWidth = 4));

        // Крок 2: 6 верхів × 6 низів = 36 оцінок; крок 3: лише 4 вцілілі × 6 пар взуття = 24
        Assert.Equal(36 + 24, scorer.ScoredOutfits);
    }
}
