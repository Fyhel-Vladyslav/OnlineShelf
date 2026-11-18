using OutfitNetworkService.Extentions;
using OutfitNetworkService.src.OutfitNetworkService.Host.Grpc;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting OutfitNetworkService");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OutfitNetworkService")
    .WriteTo.Console());

builder.Services
    .AddMainInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// Сервіс доступний лише через gRPC (h2c, Kestrel налаштований на Http2 в appsettings)
app.MapGrpcService<OutfitNetworkGrpcService>();
app.MapGet("/", () => "OutfitNetworkService: communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
