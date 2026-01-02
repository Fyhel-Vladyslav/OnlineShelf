using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ShelfsService.src.ShelfsService.Common.Interfaces;
using ShelfsService.src.ShelfsService.Repository.EfCore;

namespace ShelfsService.Extentions
{
    public static class ShelfsExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            services.AddScoped<IShelfsRepository, ShelfsRepository>();
            //services.AddScoped<IRoleResolver, RoleResolver>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var connectionString = configuration.GetConnectionString("ShelfDbConnection");
            services.AddDbContext<ShelfsDataContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsHistoryTable(
                            "__EFMigrationsHistory",
                            "shelf_service"
                        );
                    }

                )
            );

            services.AddShelfDatabaseInitialization();


            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddFastEndpoints();

            return services;
        }
    }
}
