using Microsoft.Extensions.Options;
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
            services.Configure<PenaltyRulesOptions>(configuration.GetSection("PenaltyRules"));

            // Натренована GNN (.onnx), якщо файл моделі є; інакше — заглушка, щоб решта пайплайну працювала без моделі
            services.AddSingleton<IGraphCompatibilityScorer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<GnnScorerOptions>>();
                var logger = sp.GetRequiredService<ILogger<IGraphCompatibilityScorer>>();
                var modelPath = Path.Combine(env.ContentRootPath, options.Value.ModelPath ?? string.Empty);

                if (File.Exists(modelPath))
                {
                    logger.LogInformation("Using ONNX GNN scorer from {ModelPath}", modelPath);
                    return new OnnxGraphCompatibilityScorer(Options.Create(new GnnScorerOptions { ModelPath = modelPath }));
                }

                logger.LogWarning("GNN model {ModelPath} not found, falling back to StubGraphCompatibilityScorer", modelPath);
                return new StubGraphCompatibilityScorer();
            });

            services.AddScoped<IMultiplicativePenaltyCalculator, MultiplicativePenaltyCalculator>();
            services.AddScoped<IScoringOrchestrator, ScoringOrchestrator>();
            services.AddScoped<IPenaltyRule, ColorClashPenaltyRule>();
            services.AddScoped<IPenaltyRule, VirtualItemPenaltyRule>();

            services.AddGrpc();

            return services;
        }
    }
}
