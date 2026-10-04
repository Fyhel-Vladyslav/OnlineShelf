using System.Globalization;
using OutfitOfferService.Extentions;

// Query-параметри (координати тощо) парсяться з крапкою незалежно від локалі машини (на uk-UA кома)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
        .AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseInfrastucture();

app.Run();

// Для WebApplicationFactory у тестах
public partial class Program;
