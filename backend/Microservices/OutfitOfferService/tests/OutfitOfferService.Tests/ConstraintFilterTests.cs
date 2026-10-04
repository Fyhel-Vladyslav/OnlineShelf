using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;
using static OutfitOfferService.Tests.TestData;

namespace OutfitOfferService.Tests;

public class ConstraintFilterTests
{
    [Fact]
    public void Apply_GroupsItemsBySlot_AndDropsUnknownTypesAndAccessories()
    {
        var tee = Item("tee", Types.TShirt);
        var pants = Item("pants", Types.Pants);
        var weird = Item("weird", Types.Unknown);
        var hat = Item("hat", Types.Hat);

        var pool = Filter().Apply([tee, pants, weird, hat], Context());

        Assert.Equal(new[] { tee }, pool.For(OutfitSlot.Top));
        Assert.Equal(new[] { pants }, pool.For(OutfitSlot.Bottom));
        Assert.Empty(pool.For(OutfitSlot.Unknown));
        Assert.Empty(pool.For(OutfitSlot.Accessory)); // аксесуари беруть участь лише якщо їх явно включили
        Assert.Empty(pool.Errors);
    }

    [Fact]
    public void Apply_RemovesExcludedItems()
    {
        var tee = Item("tee", Types.TShirt);
        var shirt = Item("shirt", Types.Shirt);

        var pool = Filter().Apply([tee, shirt], Context(exclude: [tee.Id]));

        Assert.Equal(new[] { shirt }, pool.For(OutfitSlot.Top));
    }

    [Fact]
    public void Apply_RemovesItemsFromDistantSeason_KeepsAllSeasonItems()
    {
        var summerTee = Item("summer tee", Types.TShirt, season: 8);
        var winterSweater = Item("winter top", Types.Shirt, season: 2);
        var anySeasonTee = Item("basic tee", Types.TShirt, season: 0);

        var pool = Filter().Apply([summerTee, winterSweater, anySeasonTee], Context(seasonPhase: 8));

        Assert.Equal(new[] { summerTee, anySeasonTee }, pool.For(OutfitSlot.Top));
    }

    [Fact]
    public void Apply_IncludedItem_IsPinned_EvenIfOutOfSeason()
    {
        var winterTop = Item("winter top", Types.Shirt, season: 2);
        var tee = Item("tee", Types.TShirt, season: 8);

        var pool = Filter().Apply([winterTop, tee], Context(seasonPhase: 8, include: [winterTop.Id]));

        Assert.Equal(new[] { winterTop }, pool.For(OutfitSlot.Top));
    }

    [Fact]
    public void Apply_IncludeWinsOverExclude_WithWarning()
    {
        var tee = Item("tee", Types.TShirt);

        var pool = Filter().Apply([tee], Context(include: [tee.Id], exclude: [tee.Id]));

        Assert.Equal(new[] { tee }, pool.For(OutfitSlot.Top));
        Assert.Single(pool.Warnings);
    }

    [Fact]
    public void Apply_TwoIncludedItemsInSameSlot_IsError()
    {
        var tee = Item("tee", Types.TShirt);
        var shirt = Item("shirt", Types.Shirt);

        var pool = Filter().Apply([tee, shirt], Context(include: [tee.Id, shirt.Id]));

        Assert.Single(pool.Errors);
    }

    [Fact]
    public void Apply_IncludedDressWithIncludedTop_IsError()
    {
        var dress = Item("dress", Types.Dress);
        var tee = Item("tee", Types.TShirt);

        var pool = Filter().Apply([dress, tee], Context(include: [dress.Id, tee.Id]));

        Assert.Single(pool.Errors);
    }

    [Fact]
    public void Apply_IncludedItemOfUnknownType_IsError()
    {
        var weird = Item("weird", Types.Unknown);

        var pool = Filter().Apply([weird], Context(include: [weird.Id]));

        Assert.Single(pool.Errors);
    }

    [Fact]
    public void Apply_IncludedAccessory_IsAddedToPinnedAccessories()
    {
        var hat = Item("hat", Types.Hat);

        var pool = Filter().Apply([hat], Context(include: [hat.Id]));

        Assert.Equal(new[] { hat }, pool.PinnedAccessories);
    }

    [Fact]
    public void Apply_HotWeather_ForbidsOuterwear()
    {
        var coat = Item("coat", Types.Coat);

        var pool = Filter().Apply([coat], Context(weather: new WeatherSnapshot(Weather.Clear, 28)));

        Assert.Equal(SlotRequirement.Forbidden, pool.Outerwear);
        Assert.Empty(pool.For(OutfitSlot.Outerwear));
    }

    [Fact]
    public void Apply_HotWeather_IncludedCoatStillAllowed()
    {
        var coat = Item("coat", Types.Coat);

        var pool = Filter().Apply([coat], Context(weather: new WeatherSnapshot(Weather.Clear, 28), include: [coat.Id]));

        Assert.Equal(SlotRequirement.Optional, pool.Outerwear);
        Assert.Equal(new[] { coat }, pool.For(OutfitSlot.Outerwear));
        Assert.Single(pool.Warnings);
    }

    [Theory]
    [InlineData(Weather.Clear, 5, SlotRequirement.Required)]
    [InlineData(Weather.Snowy, 15, SlotRequirement.Required)]
    [InlineData(Weather.Cloudy, 16, SlotRequirement.Optional)]
    [InlineData(Weather.Rainy, 25, SlotRequirement.Optional)] // тепло, але дощ — куртку не забороняємо
    [InlineData(Weather.Clear, 25, SlotRequirement.Forbidden)]
    public void ResolveOuterwearRequirement_DependsOnTemperatureAndCondition(Weather condition, double temperature, SlotRequirement expected)
    {
        Assert.Equal(expected, ConstraintFilter.ResolveOuterwearRequirement(new WeatherSnapshot(condition, temperature), Options()));
    }

    [Fact]
    public void ResolveOuterwearRequirement_NoWeather_IsOptional()
    {
        Assert.Equal(SlotRequirement.Optional, ConstraintFilter.ResolveOuterwearRequirement(null, Options()));
    }
}
