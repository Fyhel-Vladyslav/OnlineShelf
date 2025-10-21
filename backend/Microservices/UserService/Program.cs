//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
//using UserService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("UserDbConnection");
builder.Services.AddDbContext<UserService.Data.DataContext>(options =>
    options.UseNpgsql(connectionString));

//builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());


var app = builder.Build();


//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        // Get your DbContext
//        var dbContext = services.GetRequiredService<UserService.Data.DataContext>();

//        // This line runs all pending migrations
//        dbContext.Database.Migrate();
//    }
//    catch (Exception ex)
//    {
//        // Log the error
//        var logger = services.GetRequiredService<ILogger<Program>>();
//        logger.LogError(ex, "An error occurred while migrating the database.");
//    }
//}

if (app.Environment.IsDevelopment())
{
    // Apply database migrations on startup (common in development/microservices)

    app.UseSwagger();
    app.UseSwaggerUI();
}


//app.MapGet("/", () => $"{builder.Environment.ApplicationName} is running");
//app.MapGet("/ping", () => $"{builder.Environment.ApplicationName} pong!");

//// Test endpoint: calls shelfs-service
//app.MapGet("/api/user/test", async () =>
//{
//    using var client = new HttpClient();
//    var shelfResponse = await client.GetStringAsync("http://shelfs-service/api/shelfs/test");
//    return $"UserService received -> {shelfResponse}";
//});


app.UseAuthorization();    // Standard setup

app.MapControllers();      // Maps the controllers defined above

app.Run();