using System.ComponentModel.DataAnnotations;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities
{
    public class TrainingCollection
    {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        public Guid OwnerUserId { get; set; }

        public TrainingItemsSet[] TrainingItems { get; set; }

        public bool IsProccessed { get; set; } = false;
        public bool IsUsedForTraining { get; set; } = false;
         
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
