using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Reference;
using OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities;

namespace OutfitOfferService.Tests;

public class ReviewStatusTests
{
    private static ReferenceOutfit Outfit(ReferenceOutfitStatus status, params OutfitSlot[] slots)
    {
        var outfit = new ReferenceOutfit { Id = Guid.NewGuid(), Status = status };
        outfit.Items = slots.Select(s => new ReferenceItem { Id = Guid.NewGuid(), Outfit = outfit, Slot = s }).ToList();
        return outfit;
    }

    [Fact]
    public void Pending_outfit_becomes_ready_when_slot_change_completes_it()
    {
        var outfit = Outfit(ReferenceOutfitStatus.NeedsReview, OutfitSlot.Top, OutfitSlot.Shoes);
        var items = outfit.Items.Append(new ReferenceItem { Id = Guid.NewGuid(), Outfit = outfit, Slot = OutfitSlot.Bottom }).ToList();

        UpdateReferenceItemEndpoint.RecomputeStatusIfPending(outfit, items);

        Assert.Equal(ReferenceOutfitStatus.Ready, outfit.Status);
    }

    [Fact]
    public void Pending_outfit_needs_review_after_removing_a_required_item()
    {
        var outfit = Outfit(ReferenceOutfitStatus.Ready, OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes);
        var remaining = outfit.Items.Where(i => i.Slot != OutfitSlot.Bottom).ToList();

        UpdateReferenceItemEndpoint.RecomputeStatusIfPending(outfit, remaining);

        Assert.Equal(ReferenceOutfitStatus.NeedsReview, outfit.Status);
    }

    [Fact]
    public void Decided_outfit_keeps_its_status_when_items_change()
    {
        var approved = Outfit(ReferenceOutfitStatus.Approved, OutfitSlot.Top);
        UpdateReferenceItemEndpoint.RecomputeStatusIfPending(approved, []);
        Assert.Equal(ReferenceOutfitStatus.Approved, approved.Status);

        var rejected = Outfit(ReferenceOutfitStatus.Rejected, OutfitSlot.Top);
        UpdateReferenceItemEndpoint.RecomputeStatusIfPending(rejected, []);
        Assert.Equal(ReferenceOutfitStatus.Rejected, rejected.Status);
    }
}
