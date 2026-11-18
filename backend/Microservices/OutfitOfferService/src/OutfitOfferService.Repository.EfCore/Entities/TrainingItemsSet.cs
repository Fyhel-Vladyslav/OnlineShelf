using System.ComponentModel.DataAnnotations;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities
{
    public class TrainingItemsSet
    {
        [Required]
        public required Guid Id { get; init; }
        public required Guid OwnerUserId { get; init; }

        public TrainingCollection TrainingCollection { get; set; }
        public Guid TrainingCollectionId { get; set; }

        public string? BigImage { get; set; }
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public Guid[] ItemsIds { get; set; } = Array.Empty<Guid>();

    }
}
