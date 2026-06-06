using Microsoft.OpenApi.Models;

namespace MediTrack.ReminderService.Api.Extensions;

/// <summary>
/// Configura la documentación OpenAPI/Swagger (exigida en 5.2.1). Incluye el esquema
/// de seguridad Bearer para poder probar los endpoints autenticados desde Swagger UI.
/// </summary>
public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MediTrack — Reminder Service API",
                Version = "v1",
                Description = "Microservicio de recordatorios de MediTrack. Genera, programa, "
                            + "entrega (vía FCM) y cancela recordatorios de medicación, citas y exámenes."
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Introduce el token JWT (sin el prefijo 'Bearer ').",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            };

            options.AddSecurityDefinition("Bearer", jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
