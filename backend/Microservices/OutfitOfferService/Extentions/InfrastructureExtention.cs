using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

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

            services.AddHttpClient<IWeatherClient, OpenWeatherClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org/");
            });
            services.AddDistributedMemoryCache();

            services.AddScoped<IWeatherService, WeatherService>();
            services.AddSingleton<WeatherMappingExtension>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            //services.AddCors(options =>
            //{
            //    options.AddPolicy("FrontendPolicy", policy =>
            //    {
            //        policy
            //            .WithOrigins("http://localhost:5173")
            //            .AllowAnyHeader()
            //            .AllowAnyMethod();
            //    });
            //});

            //var connectionString = configuration.GetConnectionString("ShelfDbConnection");
            //services.AddDbContext<ShelfsDataContext>(options =>
            //    options.UseNpgsql(connectionString,
            //        npgsql =>
            //        {
            //            npgsql.MigrationsHistoryTable(
            //                "__EFMigrationsHistory",
            //                "shelf_service"
            //            );
            //        }

            //    )
            //);

            //services.AddShelfDatabaseInitialization();


            //services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddFastEndpoints();

            return services;
        }
    }
}
