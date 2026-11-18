using System.ComponentModel.DataAnnotations;

namespace ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
public class Item
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } 
    public required Guid UserId { get; init; }

    public Guid ShelfId { get; set; }
    public string positionKey { get; set; } = string.Empty;

    public string? BigImage { get; set; }
    public string? SmallImage { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    
    
    public Shelf Shelf { get; set; }

    public string AttributeColorMain { get; set; }
    public string AttributeColorSecond { get; set; }
    public int AttributeType { get; set; } = 0;
    public int AttributeSeason { get; set; } = 0;
    public int AttributePattern { get; set; } = 0;
    public int AttributeMatterial { get; set; } = 0;
    public bool isFavorite { get; set; } = false;

    /// <summary>
    /// Візуальний ембединг речі (CLIP image features, L2-нормований), рахується ParseImageService при завантаженні фото.
    /// Категоріальні атрибути сюди не входять — їх кодує сама модель-скорер, тому редагування атрибутів не потребує перерахунку.
    /// </summary>
    public float[]? VisualEmbedding { get; set; }

    /// <summary>Модель, якою пораховано VisualEmbedding — щоб не змішувати вектори різних версій.</summary>
    [MaxLength(100)]
    public string? EmbeddingModel { get; set; }
}