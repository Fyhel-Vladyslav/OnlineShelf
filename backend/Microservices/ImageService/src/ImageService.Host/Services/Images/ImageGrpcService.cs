using Grpc.Core;
using ImageService.Protos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageService.src.ImageService.Host.Services.Images;

public class ImageGrpcService : ImageProcessor.ImageProcessorBase
{
    private string _storagePath;
    public ImageGrpcService(IConfiguration configuration)
    {
        _storagePath = configuration["StorageSettings:ImagesPath"] ?? "/app/images";
    }

    public override async Task<UploadImageResponse> UploadImage(UploadImageRequest request, ServerCallContext context)
    {
        try
        {
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);

            var uniqueId = Guid.NewGuid().ToString();
            var bigFileName = $"{uniqueId}_big.webp";
            var smallFileName = $"{uniqueId}_small.webp";

            // Читаємо байти з gRPC запиту
            using var inputStream = new MemoryStream(request.Data.ToByteArray());
            using var image = await Image.LoadAsync(inputStream);

            // 1. Зберігаємо велике фото (авто-конвертація в WebP за розширенням)
            await image.SaveAsWebpAsync(Path.Combine(_storagePath, bigFileName));

            // 2. Робимо маленьку копію (пропорційно)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(100, 100),
                Mode = ResizeMode.Max
            }));
            await image.SaveAsWebpAsync(Path.Combine(_storagePath, smallFileName));

            return new UploadImageResponse
            {
                BigImageName = bigFileName,
                SmallImageName = smallFileName,
                Success = true
            };
        }
        catch (Exception ex)
        {
            // Тут можна додати логування ex.Message
            return new UploadImageResponse { Success = false };
        }
    }

    public override Task<DeleteImageResponse> DeleteImage(DeleteImageRequest request, ServerCallContext context)
    {
        try
        {
            var files = new[] { request.BigImageName, request.SmallImageName };
            foreach (var file in files.Where(f => !string.IsNullOrEmpty(f)))
            {
                var fullPath = Path.Combine(_storagePath, file);
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            return Task.FromResult(new DeleteImageResponse { Success = true });
        }
        catch
        {
            return Task.FromResult(new DeleteImageResponse { Success = false });
        }
    }
}