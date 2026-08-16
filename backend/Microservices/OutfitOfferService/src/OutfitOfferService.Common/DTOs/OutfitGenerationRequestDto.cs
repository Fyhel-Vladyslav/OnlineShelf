namespace OutfitOfferService.OutfitOfferService.Common.DTOs;

public class OutfitGenerationRequestDto
{
    public Guid RequestId { get; init; }
    public Guid UserId { get; set; }

    public Guid[] IncludeItemsIds { get; set; }
    public Guid[] ExcludeItemsIds { get; set; }
    public byte WeatherId { get; set; }

    
    public byte Purpose { get; set; } = 0;
    public byte Mood { get; set; } = 0;
    public string ColorPalette { get; set; }
    public string Reference { get; set; } = string.Empty;
    public byte Sex { get; set; }

    public bool PoolOnlyFavourite { get; set; } = false;
    public byte NewItemsAmount { get; set; } = 0;

}