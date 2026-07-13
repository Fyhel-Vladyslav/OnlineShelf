using UserService.Extentions;
using UserService.Extentions.DependencyInjection;
using Serilog;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting ShelfsService");


try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddAuthorization(builder.Configuration, builder.Environment)
        ;
    
    var app = builder.Build();

    app.UseUsersInfrastucture();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "ShelfsService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}