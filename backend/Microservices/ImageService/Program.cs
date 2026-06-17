using Clothing;
using ImageService.Extentions;
using ImageService.src.ImageService.Host.Protos.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. 

builder.Services
    .AddInfrastructure(builder.Environment)
    .AddGrpc();

builder.Services.AddGrpcClient<ClothingAnalyzer.ClothingAnalyzerClient>(options =>
{
    string url = builder.Configuration["GrpcSettings:ParseImageServiceUrl"]
                 ?? "http://localhost:9090";
    options.Address = new Uri(url);
});

var app = builder.Build();

app.MapGrpcService<ImageGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
