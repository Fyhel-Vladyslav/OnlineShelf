using ImageService.src.ImageService.Common.Interfaces;
using ImageService.src.ImageService.Host.ImageRecognizer;

namespace ImageService.Extentions
{
    public static class ImageExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IHostEnvironment env)
        {
            services.AddScoped<IImageRecognizer, ImageRecognizer>();
            return services;
        }
    }
}