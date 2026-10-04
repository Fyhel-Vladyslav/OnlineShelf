using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Host.Grpc;
using ShelfsService.src.ShelfsService.Repository.EfCore;

namespace ShelfsService.Extentions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseShelfsInfrastucture(this WebApplication app)
        {

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("FrontendPolicy");

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ShelfsDataContext>();
                if (app.Environment.IsDevelopment())
                {
                    db.Database.ExecuteSqlRaw(@"CREATE SCHEMA IF NOT EXISTS shelf_service;");

                    db.Database.Migrate();
                }
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.UseFastEndpoints();
            app.MapGrpcService<WardrobeGrpcService>();

            return app;
        }
    }
}
