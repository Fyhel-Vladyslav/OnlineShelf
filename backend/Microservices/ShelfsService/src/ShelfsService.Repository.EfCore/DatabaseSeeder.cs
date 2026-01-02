using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System.Data;

namespace ShelfsService.src.ShelfsService.Repository.EfCore;
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ShelfsDataContext db)
    {
        // Ensure database and schema exist
        await db.Database.MigrateAsync();

        await SeedTagTypes(db);
        await SeedShelves(db);
        await SeedItems(db);
        await SeedItemTags(db);

        await db.SaveChangesAsync();
    }

    // -------------------------
    // TagTypes (dictionary)
    // -------------------------
    private static async Task SeedTagTypes(ShelfsDataContext db)
    {
        if (await db.Set<TagType>().AnyAsync())
            return;

        db.Set<TagType>().AddRange(
            new TagType
            {
                Id = 1,
                Name = "Category",
                Description = "Item category"
            },
            new TagType
            {
                Id = 2,
                Name = "Color",
                Description = "Color related tag"
            },
            new TagType
            {
                Id = 3,
                Name = "Material",
                Description = "Material related tag"
            }
        );
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
                Name = "Main Shelf"
            },
            new Shelf
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
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
                SmallImage = null
            },
            new Item
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Pen",
                UserId = Guid.Parse("d1437f42-8b54-4a02-b0a5-800426cd7580"),
                ShelfId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            }
        );
    }

    // -------------------------
    // ItemTags
    // -------------------------
    private static async Task SeedItemTags(ShelfsDataContext db)
    {
        if (await db.ItemTags.AnyAsync())
            return;

        db.ItemTags.AddRange(
            new ItemTag
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                TagTypeId = 1,
                Name = "Stationery",
                Source = "System"
            },
            new ItemTag
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                TagTypeId = 2,
                Name = "Blue",
                Source = "User"
            }
        );
    }
}
