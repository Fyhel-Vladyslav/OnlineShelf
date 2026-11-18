namespace OutfitOfferService.src.OutfitOfferService.Common.Models;

/// <summary>
/// Роль речі в образі O = ⟨top, bottom, shoes, outerwear?⟩. FullBody (сукня, комбінезон) займає і top, і bottom.
/// Мапінг AttributeType → слот задається в конфігурації (OutfitGeneration:Slots).
/// </summary>
public enum OutfitSlot
{
    Unknown = 0,
    Top,
    Bottom,
    FullBody,
    Shoes,
    Outerwear,
    Accessory
}
