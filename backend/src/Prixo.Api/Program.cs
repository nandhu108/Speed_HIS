using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Prixo.Api.Authorization;
using Prixo.Api.Filters;
using Prixo.Api.Middleware;
using Prixo.Api.Services;
using Prixo.Application;
using Prixo.Application.Common.Interfaces;
using Prixo.Application.Common.Models;
using Prixo.Infrastructure;
using Prixo.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Prixo HIS API");

    var builder = WebApplication.CreateBuilder(args);

    // Don't advertise the server implementation in every response header.
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
    builder.Services.AddScoped<IPortalCurrentUser, PortalCurrentUserService>();

    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
    builder.Services.AddOpenApi();

    // Malformed/unbindable request bodies are rejected by the model binder before the
    // exception middleware runs — wrap that 400 in the same ApiResponse envelope every
    // other endpoint uses instead of the framework's default ProblemDetails.
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(ApiResponse<object>.Fail(
                "The request could not be processed.", errors, context.HttpContext.TraceIdentifier));
        };
    });

    // JWT bearer: 15-minute access tokens; permissions are checked against the
    // database per request (PermissionAuthorizationHandler), never carried in the JWT.
    // A second "Portal" scheme validates a distinct audience for the Patient Portal (plan
    // §21) — a staff token can never satisfy a [PortalAuth] endpoint and vice versa.
    var jwt = builder.Configuration.GetSection("Jwt");
    static JwtBearerEvents ApiResponseEvents() => new()
    {
        // Keep the ApiResponse envelope on auth failures too.
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                "Authentication required.", traceId: context.HttpContext.TraceIdentifier));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                "You do not have permission to perform this action.",
                traceId: context.HttpContext.TraceIdentifier));
        },
    };
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwt["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            options.Events = ApiResponseEvents();
        })
        .AddJwtBearer(PortalAuthAttribute.SchemeName, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwt["PortalAudience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            options.Events = ApiResponseEvents();
        });

    // Portal auth/register gets its own rate limit (plan §21: "own rate limits") — a fixed
    // window per client IP, well below what a legitimate patient would ever hit but tight
    // enough to blunt a credential-stuffing/brute-force run against the portal.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("PortalAuth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(5), PermitLimit = 10, QueueLimit = 0 }));

        // Staff login/refresh: a per-IP window in front of the per-account lockout, so a
        // password-spray across many usernames (which never trips a single account's counter)
        // and an unauthenticated brute-force run are both throttled at the source IP.
        options.AddPolicy("StaffAuth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, QueueLimit = 0 }));

        // Keep the ApiResponse envelope on a 429 instead of an empty body.
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                "Too many attempts. Please wait a moment and try again.",
                traceId: context.HttpContext.TraceIdentifier), ct);
        };
    });

    // Everything requires an authenticated user unless explicitly [AllowAnonymous].
    builder.Services.AddAuthorization(options =>
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Default")!,
            name: "postgres",
            tags: ["ready"]);

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous(); // interactive API docs at /scalar/v1
    }

    app.UseCors("Frontend");
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseMiddleware<SessionValidationMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    if (app.Environment.IsDevelopment())
    {
        app.MapHangfireDashboard("/hangfire").AllowAnonymous(); // local dev only, same posture as Scalar
    }

    // No-show auto-marking: hourly sweep of past-due Scheduled appointments.
    RecurringJob.AddOrUpdate<INoShowJob>("mark-no-shows", job => job.RunAsync(CancellationToken.None), Cron.Hourly);

    // Reports & Dashboards (Phase 23): refresh the `reporting` schema's materialized views every 15 minutes.
    RecurringJob.AddOrUpdate<IReportRefreshJob>("refresh-reporting-views", job => job.RunAsync(CancellationToken.None), "*/15 * * * *");

    // Billing (Phase 6 gap closure): nightly refresh of every open IP bill's bed-charge lines
    // + auto-sweep of admission deposits against the recomputed balance.
    RecurringJob.AddOrUpdate<IIpBedChargeRefreshJob>("refresh-ip-bed-charges", job => job.RunAsync(CancellationToken.None), Cron.Daily);

    // Liveness: process is up. Readiness: dependencies (PostgreSQL) reachable.
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
        .AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") })
        .AllowAnonymous();

    // Idempotent seed (permissions, MAIN branch, system roles, admin user, menus,
    // settings). Migrations stay explicit — this never creates schema.
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();

        // Demo/dummy transactional data — strictly opt-in (Seed:DemoData=true) and Development-only.
        // Kept out of DbSeeder above because it invents patients/claims/incidents, which must
        // never reach a real hospital database. See DemoDataSeeder's class comment.
        if (app.Configuration.GetValue<bool>("Seed:DemoData"))
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }
    }

    // Speed Multi Speciality Hospital's real reference data (branch SPEED_MULTI). Unlike demo data
    // this is genuine configuration, so it is NOT Development-gated — opt in with Seed:SpeedMulti=true.
    // DbSeeder is idempotent and runs first because doctors need the roles/permissions it creates.
    if (app.Configuration.GetValue<bool>("Seed:SpeedMulti"))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<SpeedMultiSeeder>().SeedAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException) // EF design-time tools abort the host by design
{
    Log.Fatal(ex, "Prixo HIS API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
