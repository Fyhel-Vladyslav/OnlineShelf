//using ShelfsService.Extentions;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services
//    .AddInfrastructure(builder.Configuration, builder.Environment)
//    //.AddAuthorization(builder.Configuration, builder.Environment)
//;


//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();



using ShelfService.Extensions.DependencyInjection;
using ShelfsService.Extentions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

//try
//{
    Log.Information("Starting ShelfsService");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShelfsService"));

    builder.Services
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddJwtAuthorization(builder.Configuration, builder.Environment)
        .AddGrpcConnections(builder)
    ;

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseShelfsInfrastucture();

    app.Run();
//}
//catch (Exception exception)
//{
//    Log.Fatal(exception, "ShelfsService terminated unexpectedly");
//}
//finally
//{
//    Log.CloseAndFlush();
//}
