using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Prixo.Application.Common.Interfaces;
using Prixo.Infrastructure.Identity;
using Prixo.Infrastructure.Persistence;
using Prixo.Infrastructure.Persistence.Interceptors;
using Prixo.Infrastructure.Services;

namespace Prixo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key) && o.Key.Length >= 32,
                "Jwt:Key must be configured and at least 32 characters.")
            .ValidateOnStart();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<DbSeeder>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<SpeedMultiSeeder>();

        // Patients (Phase 3)
        services.AddScoped<ISequenceService, SequenceService>();

        // ABDM / ABHA (Wave-I integration, M1). Config-gated: inert until "Abdm" credentials
        // are supplied — IAbdmGateway.IsConfigured then flips and the live sandbox is driven
        // without any change to AbdmService or the API surface.
        services.AddOptions<Integrations.Abdm.AbdmOptions>()
            .Bind(configuration.GetSection(Integrations.Abdm.AbdmOptions.SectionName));
        services.AddHttpClient<IAbdmGateway, Integrations.Abdm.AbdmGatewayClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<Integrations.Abdm.AbdmOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
            {
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            }
        });

        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IPdfService, QuestPdfService>();
        services.AddSingleton<IReportExportService, ReportExportService>();
        services.AddScoped<IReportViewRefresher, ReportViewRefresher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Appointments (Phase 4) — Hangfire backs the no-show auto-marking recurring job.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
}
