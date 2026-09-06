using FastEndpoints;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.ScoringOrchestrator;

namespace OutfitNetworkService.Extentions
{
    public static class OutfitNetworkServiceExtentions
    {
        public static IServiceCollection AddMainInfrastructure(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            services.Configure<GnnScorerOptions>(configuration.GetSection(GnnScorerOptions.SectionName));

            services.AddSingleton<IGraphCompatibilityScorer, StubGraphCompatibilityScorer>();
            // TODO: add model gnn
            //services.AddSingleton<IGraphCompatibilityScorer, OnnxGraphCompatibilityScorer>();

            services.AddScoped<IMultiplicativePenaltyCalculator, MultiplicativePenaltyCalculator>();
            services.AddScoped<IScoringOrchestrator, ScoringOrchestrator>();
            services.AddScoped<IPenaltyRule, ColorClashPenaltyRule>();




            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddGrpc();

            //services.AddScoped<IImageAnalyzer, ImageAnalyzer>();

            //services.AddScoped<IShelfsRepository, ShelfsRepository>();
            //services.AddScoped<IItemRepository, ItemRepository>();
            //services.AddSingleton<IAttributeResolver, AttributeResolver>();

            //services.AddControllers();
            //services.AddEndpointsApiExplorer();
            //services.AddSwaggerGen();

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
            //services.AddFastEndpoints();

            return services;
        }
}
}
