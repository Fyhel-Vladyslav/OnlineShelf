namespace ShelfsService.src.ShelfsService.Common;
public class ApiRoutes
{
    public const string ItemIdParam = "{itemId}";
    public const string ShelfIdParam = "{shelfId}";

    public const string Shelfs = "shelfs";
    public const string AddShelf = $"{Shelfs}/add-shelf";
    public const string GetItemById = $"{Shelfs}/items/{ItemIdParam}";
    public const string DeleteShelf = $"{Shelfs}/items/{ShelfIdParam}";
    public const string UpdateShelf = $"{Shelfs}/items";
}
