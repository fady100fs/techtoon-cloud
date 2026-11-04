using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Techtoon.Api.Options;
using Techtoon.Api.Security;

namespace Techtoon.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTechtoonJwt(this IServiceCollection services, IConfiguration config)
    {
        var jwtSection = config.GetSection(JwtOptions.SectionName);
        var secret = jwtSection["Secret"];
        if (string.IsNullOrEmpty(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret must be configured and at least 32 characters. Set Jwt__Secret environment variable or use user-secrets.");
        }
        var issuer = jwtSection["Issuer"] ?? "Techtoon";
        var audience = jwtSection["Audience"] ?? "TechtoonClients";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }

    public static IServiceCollection AddTechtoonAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Permission policies will be registered in B-3
        });
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        return services;
    }

    public static IServiceCollection AddTechtoonCors(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(options =>
        {
            options.AddPolicy("TechtoonMobile", policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => true);
                }
                else
                {
                    policy.WithOrigins(allowedOrigins);
                }
                policy.AllowAnyMethod()
                      .AllowAnyHeader()
                      .SetPreflightMaxAge(TimeSpan.FromHours(1));
            });
        });
        return services;
    }

    public static IServiceCollection AddTechtoonSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Techtoon API",
                Version = "v1",
                Description = "Techtoon Accounting - Cloud API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT access token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }
}