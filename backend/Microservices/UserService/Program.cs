using UserService.Extentions;
using UserService.Extentions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddAuthorization(builder.Configuration, builder.Environment)
;
 


var app = builder.Build();

app.UseUsersInfrastucture();

app.Run();