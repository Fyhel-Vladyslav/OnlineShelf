using ImageService.src.ImageService.Common.Interfaces;

namespace ImageService.src.ImageService.Host.ImageRecognizer;
public class ImageRecognizer : IImageRecognizer
{
    public async Task<string> DetectAsync(byte[] imageBytes)
    {
        return "Результат розпізнавання зображення (заглушка)";
    }
}
