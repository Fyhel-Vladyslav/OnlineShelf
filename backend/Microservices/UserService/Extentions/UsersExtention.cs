using FastEndpoints;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using UserService.src.UserService.Common.Interfaces;
using UserService.src.UserService.Repository.EfCore;

namespace UserService.Extentions.DependencyInjection
{
    public static class UsersExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
            IHostEnvironment env)
        {
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            var connectionString = configuration.GetConnectionString("UserDbConnection");
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddFastEndpoints();

            return services;
        }
    }
}
