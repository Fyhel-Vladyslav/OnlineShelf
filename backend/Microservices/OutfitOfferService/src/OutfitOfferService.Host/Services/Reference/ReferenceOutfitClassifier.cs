using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

/// <summary>Чи можна приймати образ без перегляду: потрібні взуття і сукня, або верх з низом.</summary>
public static class ReferenceOutfitClassifier
{
    public static bool IsComplete(IEnumerable<OutfitSlot> slots)
    {
        var present = slots.ToHashSet();
        return present.Contains(OutfitSlot.Shoes)
               && (present.Contains(OutfitSlot.FullBody)
                   || (present.Contains(OutfitSlot.Top) && present.Contains(OutfitSlot.Bottom)));
    }

    public static ReferenceOutfitStatus InitialStatus(IEnumerable<OutfitSlot> slots) =>
        IsComplete(slots) ? ReferenceOutfitStatus.Ready : ReferenceOutfitStatus.NeedsReview;
}
