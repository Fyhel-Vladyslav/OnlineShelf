using FastEndpoints;
using UserService.Extentions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddAuthorization(builder.Configuration, builder.Environment)
    ;

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseFastEndpoints();

app.Run();