using FastEndpoints;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Host.Features.Roles;
using UserService.src.UserService.Repository.EfCore;

namespace UserService.Extentions.DependencyInjection
{
    public static class UsersExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleResolver, RoleResolver>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            /*services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });*/

            var connectionString = configuration.GetConnectionString("UserDbConnection");
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsHistoryTable(
                            "__EFMigrationsHistory",
                            "user_service"
                        );
                    }

                )
            );

            services.AddUserDatabaseInitialization();


            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddFastEndpoints();

            return services;
        }
    }
}
