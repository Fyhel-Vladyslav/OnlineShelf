using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

namespace OutfitOfferService.src.OutfitOfferService.Repository.EfCore.Entities
{
    /// <summary>Партія фото, яку дизайнер завантажує за одну сесію. Лічильники статусів рахуються з фото.</summary>
    public class ReferenceBatch
    {
        public Guid Id { get; init; }
        public Guid OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<ReferencePhoto> Photos { get; set; } = [];
    }

    /// <summary>Фото людини в цілому образі; одночасно рядок черги обробки.</summary>
    public class ReferencePhoto
    {
        public Guid Id { get; init; }
        public Guid BatchId { get; set; }
        public ReferenceBatch Batch { get; set; } = null!;
        public Guid OwnerUserId { get; set; }

        /// <summary>SHA-256 файла (hex): дубль дизайнера не створює нової черги.</summary>
        public string ContentHash { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        /// <summary>Назви в ImageService; оригінал — {id}_orig за домовленістю цього сервісу.</summary>
        public string BigImage { get; set; } = string.Empty;
        public string SmallImage { get; set; } = string.Empty;

        /// <summary>Розміри фото, у пікселях яких задано рамки; відомі після обробки.</summary>
        public int? Width { get; set; }
        public int? Height { get; set; }

        public ReferencePhotoStatus Status { get; set; } = ReferencePhotoStatus.Queued;
        /// <summary>Скільки разів воркер брав фото. Перезапуск посеред обробки теж рахується як спроба.</summary>
        public int Attempts { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public DateTime? LockedUntil { get; set; }
        public string? LastError { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }

        public ReferenceOutfit? Outfit { get; set; }
    }

    /// <summary>Образ, знайдений на фото. Лише Approved бере участь у порадах.</summary>
    public class ReferenceOutfit
    {
        public Guid Id { get; init; }
        public Guid PhotoId { get; set; }
        public ReferencePhoto Photo { get; set; } = null!;

        public ReferenceOutfitStatus Status { get; set; }
        /// <summary>Модель ембедингів речей; речі з іншою моделлю не порівнюються з гардеробом.</summary>
        public string EmbeddingModel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        /// <summary>Чи вже використано образ у донавчанні моделі (фаза 6).</summary>
        public bool IsUsedForTraining { get; set; }

        public List<ReferenceItem> Items { get; set; } = [];
    }

    /// <summary>Річ еталонного образу. Не належить жодному гардеробу, на відміну від Item у ShelfsService.</summary>
    public class ReferenceItem
    {
        public Guid Id { get; init; }
        public Guid OutfitId { get; set; }
        public ReferenceOutfit Outfit { get; set; } = null!;

        public OutfitSlot Slot { get; set; }
        /// <summary>Ключі довідників ShelfsService; null, якщо Parse не визначив значення.</summary>
        public int? AttributeType { get; set; }
        public int? AttributeSeason { get; set; }
        public int? AttributePattern { get; set; }
        public int? AttributeMaterial { get; set; }
        public string ColorMain { get; set; } = string.Empty;
        public string ColorSecond { get; set; } = string.Empty;
        public float Confidence { get; set; }

        /// <summary>Рамка в пікселях фото (ReferencePhoto.Width x Height), окремі кропи не зберігаються.</summary>
        public int BoxX1 { get; set; }
        public int BoxY1 { get; set; }
        public int BoxX2 { get; set; }
        public int BoxY2 { get; set; }

        /// <summary>L2-нормований вектор кропа, real[] як Item.VisualEmbedding у ShelfsService.</summary>
        public float[] Embedding { get; set; } = [];
    }
}
