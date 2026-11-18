using FastEndpoints;

namespace OutfitOfferService.Extentions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseInfrastucture(this WebApplication app)
        {

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("FrontendPolicy");

            //using (var scope = app.Services.CreateScope())
            //{
            //    var db = scope.ServiceProvider.GetRequiredService<ShelfsDataContext>();
            //    if (app.Environment.IsDevelopment())
            //    {
            //        db.Database.ExecuteSqlRaw(@"CREATE SCHEMA IF NOT EXISTS shelf_service;");

            //        db.Database.Migrate();
            //    }
            //}

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.UseFastEndpoints();


            // Test endpoint: returns static text
            app.MapGet("/api/outfit/test", () => "OutfitOfferService response");


            return app;
        }
    }
}
