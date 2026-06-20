namespace ImageService.src.ImageService.Common
{
    public class ApiRoutes
    {
        public const string ImageNameParam = "{*imageName}";
        public const string RecognizeClothes = $"image/recognize/{ImageNameParam}";

    }
}

