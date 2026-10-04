using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

namespace OutfitOfferService.Tests;

public class SeasonAndSlotTests
{
    [Theory]
    [InlineData(12, 1)]  // грудень — рання зима
    [InlineData(1, 2)]
    [InlineData(2, 3)]   // лютий — пізня зима
    [InlineData(3, 4)]   // березень — рання весна
    [InlineData(7, 8)]   // липень — середина літа
    [InlineData(11, 12)] // листопад — пізня осінь
    public void CurrentPhase_NorthernHemisphere_MapsMonthToSeasonPhase(int month, int expectedPhase)
    {
        Assert.Equal(expectedPhase, SeasonCalculator.CurrentPhase(new DateTime(2026, month, 15), latitude: 50.6));
    }

    [Fact]
    public void CurrentPhase_SouthernHemisphere_IsShiftedByHalfYear()
    {
        // Липень у Сіднеї — середина зими
        Assert.Equal(2, SeasonCalculator.CurrentPhase(new DateTime(2026, 7, 15), latitude: -33.9));
        // Січень у Сіднеї — середина літа
        Assert.Equal(8, SeasonCalculator.CurrentPhase(new DateTime(2026, 1, 15), latitude: -33.9));
    }

    [Theory]
    [InlineData(1, 12, 1)] // рання зима й пізня осінь — сусіди по колу
    [InlineData(2, 8, 6)]  // зима проти літа — максимальна відстань
    [InlineData(5, 5, 0)]
    public void Distance_IsCyclic(int a, int b, int expected)
    {
        Assert.Equal(expected, SeasonCalculator.Distance(a, b));
    }

    [Theory]
    [InlineData(Types.TShirt, OutfitSlot.Top)]
    [InlineData(Types.Coat, OutfitSlot.Outerwear)]
    [InlineData(Types.Skirt, OutfitSlot.Bottom)]
    [InlineData(Types.Dress, OutfitSlot.FullBody)]
    [InlineData(Types.Boots, OutfitSlot.Shoes)]
    [InlineData(Types.Hat, OutfitSlot.Accessory)]
    [InlineData(Types.Unknown, OutfitSlot.Unknown)]
    [InlineData(Types.Flower, OutfitSlot.Unknown)]
    public void SlotResolver_UsesConfiguredMapping(int type, OutfitSlot expected)
    {
        var resolver = new SlotResolver(TestData.Wrap(TestData.Options()));
        Assert.Equal(expected, resolver.Resolve(type));
    }

    [Fact]
    public void SlotResolver_UnknownSlotName_Throws()
    {
        var options = TestData.Options(o => o.Slots["Hats"] = [17]);
        Assert.Throws<InvalidOperationException>(() => new SlotResolver(TestData.Wrap(options)));
    }
}
