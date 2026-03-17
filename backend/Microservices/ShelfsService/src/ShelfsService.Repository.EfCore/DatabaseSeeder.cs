using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShelfsService.src.ShelfsService.Repository.EfCore.Entities;
using System;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Reflection.Metadata.BlobBuilder;
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
                AttributeColorMain = "#656",
                AttributeColorSecond = "#656",
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
                AttributeColorMain = "#656",
                AttributeColorSecond = "#656",
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
        /// Types
            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "TShirt",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Shirt",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 3,
                AttributeValue = "Blouse",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 4,
                AttributeValue = "Sweater",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 5,
                AttributeValue = "Hoodie",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 6,
                AttributeValue = "Jacket",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 7,
                AttributeValue = "Coat",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 8,
                AttributeValue = "Pants",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 9,
                AttributeValue = "Trousers",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 10,
                AttributeValue = "Shorts",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 11,
                AttributeValue = "Skirt",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 12,
                AttributeValue = "Dress",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 13,
                AttributeValue = "Sneakers",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 14,
                AttributeValue = "Boots",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 15,
                AttributeValue = "Slippers",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 16,
                AttributeValue = "Formal Shoes",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 17,
                AttributeValue = "Hat",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 18,
                AttributeValue = "Cap",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 19,
                AttributeValue = "Scarf",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 20,
                AttributeValue = "Gloves",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 21,
                AttributeValue = "Slippers",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 22,
                AttributeValue = "Belt",
                AttributeId = 3
            },

            new AttributesValue
            {
                AttributeKey = 23,
                AttributeValue = "Tie",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 24,
                AttributeValue = "Sunglasses",
                AttributeId = 3
            },
            new AttributesValue
            {
                AttributeKey = 25,
                AttributeValue = "Watch",
                AttributeId = 3
            },


            /// Seasons
            
            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "Early winter",
                AttributeId = 4
            }, 
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Midle winter",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 3,
                AttributeValue = "Late winter",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 4,
                AttributeValue = "Early spring",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 5,
                AttributeValue = "Midle spring",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 6,
                AttributeValue = "Late spring",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 7,
                AttributeValue = "Early summer",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 8,
                AttributeValue = "Midle summer",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 9,
                AttributeValue = "Late summer",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 10,
                AttributeValue = "Early autumn",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 11,
                AttributeValue = "Midle autumn",
                AttributeId = 4
            },
            new AttributesValue
            {
                AttributeKey = 12,
                AttributeValue = "Late autumn",
                AttributeId = 4
            },


            /// Petterns
            
            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "Solid (No Pattern)",
                AttributeId = 5
            }, 
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Stripes",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 3,
                AttributeValue = "Floral",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 4,
                AttributeValue = "Camouflage",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 5,
                AttributeValue = "Dots",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 6,
                AttributeValue = "Logo Print",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 7,
                AttributeValue = "Glitter",
                AttributeId = 5
            },
            new AttributesValue
            {
                AttributeKey = 8,
                AttributeValue = "Distressed",
                AttributeId = 5
            },

            /// Matterials

            new AttributesValue
            {
                AttributeKey = 1,
                AttributeValue = "Cotton",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 2,
                AttributeValue = "Polyester",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 3,
                AttributeValue = "Wool",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 4,
                AttributeValue = "Silk",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 5,
                AttributeValue = "Linen",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 6,
                AttributeValue = "Denim",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 7,
                AttributeValue = "Leather",
                AttributeId = 6
            },
            new AttributesValue
            {
                AttributeKey = 8,
                AttributeValue = "Nylon",
                AttributeId = 6
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
                Name = "Color 1"
            },
            new Attribute
            {
                AttributeKey = 2,
                Name = "Color 2"
            },
            new Attribute
            {
                AttributeKey = 3,
                Name = "Type"
            },
            new Attribute
            {
                AttributeKey = 4,
                Name = "Season"
            },
            new Attribute
            {
                AttributeKey = 5,
                Name = "Pattern"
            },
            new Attribute
            {
                AttributeKey = 6,
                Name = "Material"
            },
            new Attribute
            {
                AttributeKey = 7,
                Name = "Favourite"
            }
        );
    }
}
