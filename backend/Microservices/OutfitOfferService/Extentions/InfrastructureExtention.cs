using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;
using OutfitOfferService.src.OutfitOfferService.Host.Services.Grpc;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;
using NetworkClient = OutfitNetworkService.Protos.OutfitNetworkService.OutfitNetworkServiceClient;
using WardrobeClient = ShelfsService.Protos.Wardrobe.WardrobeClient;

namespace OutfitOfferService.Extentions
{
    public static class InfrastructureExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddSingleton(TimeProvider.System);

            // --- Погода ---
            services.Configure<WeatherOptions>(configuration.GetSection(WeatherOptions.SectionName));
            services.AddHttpClient<IWeatherClient, OpenWeatherClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org/");
            });
            services.AddDistributedMemoryCache();
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddSingleton<WeatherMappingExtension>();

            // --- gRPC-клієнти: гардероб (ShelfsService) і модель сумісності (OutfitNetworkService) ---
            services.AddGrpcClient<WardrobeClient>(o =>
                o.Address = new Uri(configuration["GrpcSettings:ShelfsServiceUrl"] ?? "http://shelfsservice:8082"));
            services.AddGrpcClient<NetworkClient>(o =>
                o.Address = new Uri(configuration["GrpcSettings:OutfitNetworkServiceUrl"] ?? "http://outfitnetworkservice:8080"));

            services.AddScoped<IWardrobeProvider, ShelfsWardrobeProvider>();
            services.AddScoped<IOutfitScorer, NetworkOutfitScorer>();

            // --- Бізнес-логіка підбору образу ---
            services.Configure<OutfitGenerationOptions>(configuration.GetSection(OutfitGenerationOptions.SectionName));
            services.AddSingleton<ISlotResolver, SlotResolver>();
            services.AddSingleton<IConstraintFilter, ConstraintFilter>();
            services.AddScoped<IOutfitGenerator, BeamSearchOutfitGenerator>();
            services.AddScoped<IOutfitOfferOrchestrator, OutfitOfferOrchestrator>();

            services.AddFastEndpoints();

            return services;
        }
    }
}
