using FastEndpoints;

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

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.UseFastEndpoints();

            return app;
        }    
    }
}
