using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserService.src.UserService.Host.Features.JwtToken;
using UserService.src.UserService.Repository.EfCore.Entities;

namespace UserService.Extentions.DependencyInjection
{
    public static class JwtAuthorization
    {
        public static IServiceCollection AddAuthorization(this IServiceCollection services, IConfiguration configuration,
IHostEnvironment env)
        {
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

            var jwtSigningKey = configuration["Jwt:SigningKey"];
            var jwtIssuer = configuration["Jwt:Authority"];
            var jwtAudience = configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtSigningKey))
            {
                throw new InvalidOperationException("Jwt:SigningKey is not configured.");
            }
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole("User"));

                options.AddPolicy("PremiumUserPolicy", policy =>
                    policy.RequireRole("PremiumUser"));

                options.AddPolicy("DesignerPolicy", policy =>
                    policy.RequireRole("Designer"));

            });
            var key = Encoding.UTF8.GetBytes(jwtSigningKey);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtOptions =>
                {
                    jwtOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),

                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddSingleton<JwtTokenService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


            return services;
        }
    }
}
