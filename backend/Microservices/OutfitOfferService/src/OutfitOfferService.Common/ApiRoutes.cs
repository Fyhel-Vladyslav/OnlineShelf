namespace OutfitOfferService.src.OutfitOfferService.Common;
public class ApiRoutes
{
    public const string Offers = "offers";

    public const string GetWeather = $"{Offers}/get-weather";
    public const string GenerateOutfits = $"{Offers}/generate";

    public const string Reference = $"{Offers}/reference";
    public const string ReferenceBatches = $"{Reference}/batches";
    public const string ReferenceBatchPhotos = $"{ReferenceBatches}/{{BatchId}}/photos";
    public const string ReferenceBatchApproveReady = $"{ReferenceBatches}/{{BatchId}}/approve-ready";
    public const string ReferencePhotoRetry = $"{Reference}/photos/{{PhotoId}}/retry";
    public const string ReferenceOutfitById = $"{Reference}/outfits/{{OutfitId}}";
    public const string ReferenceItemById = $"{Reference}/items/{{ItemId}}";
}
