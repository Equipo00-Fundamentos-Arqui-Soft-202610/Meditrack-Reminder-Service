using MediTrack.ReminderService.Api.Extensions;
using MediTrack.ReminderService.Api.Middleware;
using MediTrack.ReminderService.Application;
using MediTrack.ReminderService.Infrastructure;
using MediTrack.ReminderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios ---
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();

var disableAuth = builder.Configuration.GetValue<bool>("DisableAuth");
if (disableAuth)
{
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization(opts =>
        opts.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build());
}
else
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReminderDbContext>(name: "mysql", tags: new[] { "ready" });

var app = builder.Build();

// --- Migración automática opcional (entornos de desarrollo/contenedor) ---
if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ReminderDbContext>();
    await dbContext.Database.MigrateAsync();
}

// --- Pipeline HTTP ---
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reminder Service API v1");
        options.DocumentTitle = "MediTrack — Reminder Service";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Punto de entrada expuesto para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program { }
