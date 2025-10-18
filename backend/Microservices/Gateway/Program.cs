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

using Microsoft.AspNetCore.Mvc;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// --- 2. Register Ocelot Services ---
// This registers all the necessary Ocelot services (like routing engine) for dependency injection.
builder.Services.AddOcelot();


var app = builder.Build();

app.MapGet("/", () => "Gateway is running");

app.MapGet("/test/users", async ([FromServices] HttpClient http) =>
{
    return await http.GetStringAsync("http://user-service:8080/ping");
});

app.MapGet("/test/shelfs", async ([FromServices] HttpClient http) =>
{
    return await http.GetStringAsync("http://shelfs-service:8080/ping");
});

app.MapGet("/test/offers", async ([FromServices] HttpClient http) =>
{
    return await http.GetStringAsync("http://outfit-offer-service:8080/ping");
});
// Test endpoint: calls user-service
app.MapGet("/test", async () =>
{
    using var client = new HttpClient();
    var userResponse = await client.GetStringAsync("http://user-service/api/user/test");
    return $"Gateway received -> {userResponse}";
});
await app.UseOcelot();

app.Run();