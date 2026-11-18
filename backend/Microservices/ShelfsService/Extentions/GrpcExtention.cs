using ImageService.Protos;
using System;
using Clothing;
using Microsoft.Extensions.Options;

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

        services.AddGrpcClient<Clothing.ClothingAnalyzer.ClothingAnalyzerClient>(o =>
        {
            string url = builder.Configuration["GrpcSettings:ParseImageServiceUrl"]
                 ?? "http://parseimageservice:5000";
            o.Address = new Uri(url);
        });

        // gRPC-сервер Wardrobe для OutfitOfferService
        services.AddGrpc();

        return services;
    }
}
