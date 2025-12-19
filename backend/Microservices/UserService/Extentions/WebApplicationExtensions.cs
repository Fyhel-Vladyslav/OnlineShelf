using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Repository.EfCore;

namespace UserService.Extentions.DependencyInjection
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseUsersInfrastucture (this WebApplication app)
        {

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                if (app.Environment.IsDevelopment())
                {
                    db.Database.ExecuteSqlRaw(@"CREATE SCHEMA IF NOT EXISTS user_service;");

                    db.Database.Migrate();
                }
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.UseFastEndpoints();

            return app;
        }    
    }
}
