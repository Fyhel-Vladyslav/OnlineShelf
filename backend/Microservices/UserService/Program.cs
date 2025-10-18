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
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => $"{builder.Environment.ApplicationName} is running");
app.MapGet("/ping", () => $"{builder.Environment.ApplicationName} pong!");

// Test endpoint: calls shelfs-service
app.MapGet("/api/user/test", async () =>
{
    using var client = new HttpClient();
    var shelfResponse = await client.GetStringAsync("http://shelfs-service/api/shelfs/test");
    return $"UserService received -> {shelfResponse}";
});

app.Run();