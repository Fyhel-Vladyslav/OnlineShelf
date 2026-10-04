using System.Globalization;
using OutfitOfferService.Extentions;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

// Query-параметри (координати тощо) парсяться з крапкою незалежно від локалі машини (на uk-UA кома)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
        .AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration[OpenWeatherClient.ApiKeySetting]))
{
    app.Logger.LogWarning(
        "{Setting} is not configured: weather constraints will not be applied to outfit generation",
        OpenWeatherClient.ApiKeySetting);
}

app.UseInfrastucture();

app.Run();

// Для WebApplicationFactory у тестах
public partial class Program;
