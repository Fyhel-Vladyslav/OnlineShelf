using FastEndpoints;
using OutfitNetworkService.Extentions;
using OutfitNetworkService.src.OutfitNetworkService.Host.Grpc;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();




var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
builder.Services.Configure<PenaltyRulesOptions>(
    builder.Configuration.GetSection("PenaltyRules"));

// Add services to the container.

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ShelfsService"));

builder.Services
    .AddMainInfrastructure(builder.Configuration, builder.Environment)
;


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();
app.MapGrpcService<OutfitNetworkGrpcService>();



app.Run();



