namespace ShelfsService.src.ShelfsService.Common;
public class ApiRoutes
{
    public const string ItemIdParam = "{itemId}";

    public const string Shelfs = "shelfs";
    public const string AddShelf = $"{Shelfs}/add-shelf";
    public const string GetItemById = $"{Shelfs}/items/{ItemIdParam}";
}
