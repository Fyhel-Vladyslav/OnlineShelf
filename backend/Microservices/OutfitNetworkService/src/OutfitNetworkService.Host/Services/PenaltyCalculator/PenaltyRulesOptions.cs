namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator
{
    public sealed class PenaltyRulesOptions
    {
        public List<ColorPairOption> ClashingColorPairs { get; set; } = new();
    }

    public sealed class ColorPairOption
    {
        public string ColorA { get; set; } = string.Empty;  // "#FF0000"
        public string ColorB { get; set; } = string.Empty;
    }
}
