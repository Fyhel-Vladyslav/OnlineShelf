namespace ImageService.src.ImageService.Common.Interfaces;
public interface IImageRecognizer
{
    Task<string> DetectAsync(byte[] imageBytes);
}