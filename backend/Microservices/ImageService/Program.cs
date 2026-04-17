using ImageService.Extentions;
using ImageService.src.ImageService.Host.Protos.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. 

builder.Services
    .AddInfrastructure(builder.Environment)
    .AddGrpc();

var app = builder.Build();

app.MapGrpcService<ImageGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
