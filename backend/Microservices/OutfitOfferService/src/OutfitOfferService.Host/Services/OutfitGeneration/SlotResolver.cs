using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Models;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

public interface ISlotResolver
{
    OutfitSlot Resolve(int attributeType);
}

/// <summary>Визначає роль речі в образі за її AttributeType (мапінг з конфігурації).</summary>
public sealed class SlotResolver : ISlotResolver
{
    private readonly Dictionary<int, OutfitSlot> _slotByType = new();

    public SlotResolver(IOptions<OutfitGenerationOptions> options)
    {
        foreach (var (slotName, typeKeys) in options.Value.Slots)
        {
            if (!Enum.TryParse<OutfitSlot>(slotName, ignoreCase: true, out var slot))
            {
                throw new InvalidOperationException($"Unknown outfit slot '{slotName}' in {OutfitGenerationOptions.SectionName}:Slots");
            }

            foreach (var typeKey in typeKeys)
            {
                _slotByType[typeKey] = slot;
            }
        }
    }

    public OutfitSlot Resolve(int attributeType) =>
        _slotByType.TryGetValue(attributeType, out var slot) ? slot : OutfitSlot.Unknown;
}
