using FastEndpoints;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using UserService.src.Data;

namespace UserService.Extentions.DependencyInjection
{
    public static class UsersExtention
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
    IHostEnvironment env)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            var connectionString = configuration.GetConnectionString("UserDbConnection");
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
            services.AddFastEndpoints();

            return services;

            //    ArgumentNullException.ThrowIfNull(configuration);
            //    ArgumentNullException.ThrowIfNull(env);

            //    services
            //        .AddAuthAndJwtTokens(configuration)
            //        .AddControllersAndJson(env)
            //        .AddMemoryCache();

            //    // Allow CORS only in dev mode
            //    if (env.IsDevelopment())
            //    {
            //        services.AddCors(options => options.AddPolicy(HostConsts.CorsPolicyName, x =>
            //        {
            //            x.AllowAnyHeader()
            //            .AllowAnyMethod()
            //            .SetIsOriginAllowed(_ => true)
            //            .AllowCredentials();
            //        }));
            //    }

            //    // Add swagger and request/response logging only in development
            //    if (env.IsDevelopment())
            //    {
            //        services
            //            .AddEndpointsApiExplorer()
            //            .AddSwaggerWithCustomization();
            //    }

            //    services.AddHttpLogging(logging =>
            //    {
            //        logging.LoggingFields = HttpLoggingFields.All;
            //    });

            //    return services;


        }
    }
}
