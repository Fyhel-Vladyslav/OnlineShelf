namespace ShelfsService.src.ShelfsService.Common;
public class ApiRoutes
{
    public const string ItemIdParam = "{itemId}";
    public const string ShelfIdParam = "{shelfId}";

    public const string Shelfs = "shelfs";
    public const string AddShelf = $"{Shelfs}/add-shelf";
    public const string DeleteShelf = $"{Shelfs}/{ShelfIdParam}";
    public const string UpdateShelf = $"{Shelfs}";

    public const string AddItem = $"{Shelfs}/items/add-item";
    public const string GetItemById = $"{Shelfs}/items/{ItemIdParam}";
    public const string DeleteItem = $"{Shelfs}/items/{ItemIdParam}";
    public const string UpdateItem = $"{Shelfs}/items";

    public const string AddTagsToItem = $"{Shelfs}/item-tags/add";
    public const string DeleteTagsFromItem = $"{Shelfs}/item-tags/remove";
}
