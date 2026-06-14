using ImageService.Protos;
using System;

namespace ShelfsService.Extentions;
public static class GrpcExtention
{
    public static IServiceCollection AddGrpcConnections(
        this IServiceCollection services,
                    WebApplicationBuilder builder
        )
    {
        services.AddGrpcClient<ImageProcessor.ImageProcessorClient>(o =>
        {
            var url = builder.Configuration["GrpcConfigs:ImageServiceUrl"];
            o.Address = new Uri(url ?? "http://imageservice:8080");
        });


        return services;
    }

}
