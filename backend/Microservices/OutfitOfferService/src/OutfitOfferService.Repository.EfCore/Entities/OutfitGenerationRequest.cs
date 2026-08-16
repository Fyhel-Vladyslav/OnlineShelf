using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities
{
    public class OutfitGenerationRequest
    {
        [Required]
        public Guid RequestId { get; init; }
        public Guid UserId { get; set; }

        public Guid[] IncludeItemsIds { get; set; }
        public Guid[] ExcludeItemsIds { get; set; }
        public byte WeatherId { get; set; }


        public byte Purpose { get; set; } = 0;
        public byte Mood { get; set; } = 0;
        public Color ColorPalette { get; set; } //TODO change to colour type
        public string Reference { get; set; } = string.Empty;
        public byte Sex { get; set; }

        public bool PoolOnlyFavourite { get; set; } = false;
        public byte NewItemsAmount { get; set; } = 0;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
