//using ImageService.Extentions;
using ImageService.src.ImageService.Host.Protos.Service;
using Serilog;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting ImageService");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ImageService"));

// Add services to the container.

builder.Services
    //.AddInfrastructure(builder.Environment)
    .AddGrpc();

//builder.Services.AddGrpcClient<ClothingAnalyzer.ClothingAnalyzerClient>(options =>
//{
//    string url = builder.Configuration["GrpcSettings:ParseImageServiceUrl"]
//                 ?? "http://parseimageservice:5000";
//    options.Address = new Uri(url);
//});

var app = builder.Build();
app.MapGrpcService<ImageGrpcService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
