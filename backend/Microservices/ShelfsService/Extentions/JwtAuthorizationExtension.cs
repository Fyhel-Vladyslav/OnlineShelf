using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ShelfsService.src.ShelfsService.Host.Features.JwtToken;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShelfService.Extensions.DependencyInjection;
    public static class JwtAuthorizationExtension
    {
        public static IServiceCollection AddJwtAuthorization(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment env)
        {
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

            var jwtSigningKey = configuration["Jwt:SigningKey"];
            var jwtIssuer = configuration["Jwt:Authority"];
            var jwtAudience = configuration["Jwt:Audience"];

            var key = Encoding.UTF8.GetBytes(jwtSigningKey!);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),

                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,

                        ValidateAudience = true,
                        ValidAudience = jwtAudience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        // IMPORTANT
                        RoleClaimType = ClaimTypes.Role,
                        NameClaimType = JwtRegisteredClaimNames.NameId,
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", p => p.RequireRole("Admin"));
                options.AddPolicy("DesignerPolicy", p => p.RequireRole("Designer"));
                options.AddPolicy("UserPolicy", p => p.RequireRole("User"));
            });

            return services;
        }
    }