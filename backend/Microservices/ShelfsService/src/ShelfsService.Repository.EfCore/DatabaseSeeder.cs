using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System.Data;
using System.Xml.Linq;
using Attribute = ShelfsService.src.ShelfsService.Repository.EfCore.Entities.AttributeName;

namespace ShelfsService.src.ShelfsService.Repository.EfCore;
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ShelfsDataContext db)
    {
        // Ensure database and schema exist
        await db.Database.MigrateAsync();

        await SeedShelves(db);
        await SeedItems(db);
        await SeedAttributesValues(db);
        await SeedAttributes(db);

        await db.SaveChangesAsync();
    }

    // -------------------------
    // Shelves
    // -------------------------
    private static async Task SeedShelves(ShelfsDataContext db)
    {
        if (await db.Shelfs.AnyAsync())
            return;

        db.Shelfs.AddRange(
            new Shelf
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                Name = "Main Shelf"
            },
            new Shelf
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                Name = "Secondary Shelf"
            }
        );
    }

    // -------------------------
    // Items
    // -------------------------
    private static async Task SeedItems(ShelfsDataContext db)
    {
        if (await db.Items.AnyAsync())
            return;

        db.Items.AddRange(
            new Item
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Notebook",
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                ShelfId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                BigImage = null,
                SmallImage = null,
                AttributeColorMain = 1,
                AttributeColorSecond = 2,
                AttributeType = 3,
                AttributeSeason = 4,
                AttributePattern = 5,
                AttributeMatterial = 6,
                isFavorite = true
            },
            new Item
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Pen",
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                ShelfId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AttributeColorMain = 2,
                AttributeColorSecond = 4,
                AttributeType = 2,
                AttributeSeason = 1,
                AttributePattern = 3,
                AttributeMatterial = 7,
                isFavorite = false
            }
        );
    }


    // -------------------------
    // AttributesValues
    // -------------------------
    private static async Task SeedAttributesValues(ShelfsDataContext db)
    {
        if (await db.AttributesValues.AnyAsync())
            return;

        db.AttributesValues.AddRange(
            /// Colours
            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "Red",
                AttributeId = 1
            },
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Blue",
                AttributeId = 1
            },
            /// Types
            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "TShirt",
                AttributeId = 2
            },
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Shoose",
                AttributeId = 2
            }
        );
    }

    // -------------------------
    // Attributes
    // -------------------------
    private static async Task SeedAttributes(ShelfsDataContext db)
    {
        if (await db.Attributes.AnyAsync())
            return;

        db.Attributes.AddRange(
            new Attribute
            {
                AttributeKey = 1,
                Name = "Color"
            },
            new Attribute
            {
                AttributeKey = 2,
                Name = "Type"
            }
        );
    }
}
