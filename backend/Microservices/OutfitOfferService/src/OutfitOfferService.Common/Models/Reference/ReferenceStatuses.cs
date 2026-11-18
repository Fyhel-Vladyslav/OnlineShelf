namespace OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

/// <summary>Стан фото в черзі обробки (ParseImageService.AnalyzeOutfit).</summary>
public enum ReferencePhotoStatus
{
    Queued,
    Processing,
    Done,
    /// <summary>Фото оброблено, але речей на ньому не знайдено.</summary>
    NoItems,
    Failed
}

/// <summary>Стан образу в огляді дизайнера. Еталоном для порад стає лише Approved.</summary>
public enum ReferenceOutfitStatus
{
    /// <summary>Повний образ (взуття плюс сукня або верх із низом): можна прийняти разом із рештою.</summary>
    Ready,
    NeedsReview,
    Approved,
    Rejected
}
