using global::ImageService.Protos;
using global::ImageService.src.ImageService.Common.Interfaces;
using Google.Protobuf;
using Grpc.Core;
using ImageService.Protos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageService.src.ImageService.Host.Protos.Service;

public class ImageGrpcService : ImageProcessor.ImageProcessorBase
{
    private string _storagePath;
    private IImageRecognizer _recognizer;
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

    public override async Task<RecognizeImageResponse> RecognizeImage(RecognizeImageRequest request, ServerCallContext context)
    {
        try
        {
            // 1. Отримуємо байти зображення  
            byte[] imageBytes = request.ImageData.ToByteArray();

            // 2. Викликаємо логіку розпізнавання (про це нижче)  
            var recognitionResult = await _recognizer.DetectAsync(imageBytes);

            // 3. Формуємо відповідь  
            return new RecognizeImageResponse
            {
                Success = true,
                RecognizedDataJson = recognitionResult
            };
        }
        catch (Exception ex)
        {
            return new RecognizeImageResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task GetPhotoByName(GetPhotoRequest request, IServerStreamWriter<GetPhotoResponse> responseStream, ServerCallContext context)
    {
        // Захист від Path Traversal вразливостей (щоб не могли вийти за межі папки)
        var fileName = Path.GetFileName(request.FileName);
        var filePath = Path.Combine(_storagePath, fileName);

        if (!File.Exists(filePath))
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Зображення з назвою {fileName} не знайдено."));
        }

        // Розмір чанку - 1 МБ (можна налаштувати під свої потреби)
        const int chunkSize = 1024 * 1024;
        var buffer = new byte[chunkSize];

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        while (true)
        {
            // Перевіряємо, чи клієнт не скасував запит
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

            // Якщо прочитали 0 байт, значить файл закінчився
            if (bytesRead == 0)
            {
                break;
            }

            // Конвертуємо прочитані байти у формат protobuf і відправляємо у стрім
            var response = new GetPhotoResponse
            {
                ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead)
            };

            await responseStream.WriteAsync(response);
        }
    }
} 