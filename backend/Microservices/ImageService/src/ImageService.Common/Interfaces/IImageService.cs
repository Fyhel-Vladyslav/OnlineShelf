namespace ImageService.src.ImageService.Common.Interfaces;
public interface IImageService
{
    Task<(string BigPath, string SmallPath)> SaveImageAsync(IFormFile file);

    void DeleteImage(string? path);
}