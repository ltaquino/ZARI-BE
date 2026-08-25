using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Data.Repositories.Todo;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.DTOs.Swagger;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Identity;
using ZARI.Infrastructure.Persistence;
using ZARI.Infrastructure.Persistence.Repositories.Todo;

namespace ZARI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence();
        services.AddAuth(configuration);
        services.AddCachingServices();
        services.AddEssentials(configuration);
        return services;
    }

    public static void AddEssentials(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterSwagger(configuration);
        services.AddVersioning();
    }

    private static void RegisterSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var swaggerOptions = new List<SwaggerOptions>();
        configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

        services.AddSwaggerGen(c =>
        {
            //TODO - Lowercase Swagger Documents
            //c.DocumentFilter<LowercaseDocumentFilter>();
            //Refer - https://gist.github.com/rafalkasa/01d5e3b265e5aa075678e0adfd54e23f
            //c.IncludeXmlComments(string.Format(@"{0}ZARI.Api.xml", System.AppDomain.CurrentDomain.BaseDirectory));
            foreach (var opt in swaggerOptions)
            {
                c.SwaggerDoc(opt.Document.Name, new OpenApiInfo
                {
                    Version = "v1",
                    Title = opt.Document.Title,
                    Description = opt.Document.Description,
                    Contact = new OpenApiContact
                    {
                        Name = opt.Contact.Name,
                        Email = opt.Contact.Email,
                        Url = (String.IsNullOrEmpty(opt.Contact.Url) ? null : new Uri(opt.Contact.Url)),
                    },
                    License = new OpenApiLicense()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    }
                });
            }
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            //c.OperationFilter<FormFileSwaggerFilter>();
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste your raw JWT token value directly below (Do NOT include the 'Bearer ' prefix).",
            });
            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {

                {
                    // Pass the document instance into the Reference constructor context
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });
    }

    private static void AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });
    }

    private static void AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddTransient<ITodoItemRepository, TodoItemRepository>();
    }   

    public static void AddPersistenceContexts(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("cwm-db");
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 41));

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, serverVersion,
                builder => builder.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            ));
    }

    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero,                    
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        Console.WriteLine("JWT RECEIVED");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        Console.WriteLine("TOKEN VALIDATED SUCCESSFULLY");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine("AUTH FAILED");
                        Console.WriteLine(ctx.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine("AUTH ON CHALLENGE");
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(FluentResults.Result.Fail("You are not Authorized"));
                        return context.Response.WriteAsync(result);
                    },
                    OnForbidden = context =>
                    {
                        Console.WriteLine("AUTH FORBIDDEN");
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(FluentResults.Result.Fail("You are not authorized to access this resource"));
                        return context.Response.WriteAsync(result);
                    },
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IPermissionService, PermissionService>();
    }

    private static void AddCachingServices(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            };
        });
    }
}
