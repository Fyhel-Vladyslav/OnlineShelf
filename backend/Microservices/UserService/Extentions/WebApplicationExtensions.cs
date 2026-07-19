using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Repository.EfCore;

namespace UserService.Extentions.DependencyInjection
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseUsersInfrastucture (this WebApplication app)
        {

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                if (app.Environment.IsDevelopment())
                {
                    db.Database.ExecuteSqlRaw(@"CREATE SCHEMA IF NOT EXISTS user_service;");

                    db.Database.Migrate();
                }
            }
            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseRouting();
            //app.UseCors("FrontendPolicy");

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseFastEndpoints(c =>
            {
                // Цей лямбда-вираз автоматично додає налаштування до кожного твого ендпоінту перед його реєстрацією
                c.Endpoints.Configurator = ep =>
                {
                    // Ми дозволяємо анонімні OPTIONS-запити для абсолютно всіх ендпоінтів у системі,
                    // щоб браузерні CORS preflight-запити ніколи не відбивалися фреймворком
                    ep.Options(b => b.AllowAnonymous());
                };
            });

            return app;
        }    
    }
}
