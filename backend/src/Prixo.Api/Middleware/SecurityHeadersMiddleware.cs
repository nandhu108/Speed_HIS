namespace Prixo.Api.Middleware;

/// <summary>
/// Adds standard browser-hardening response headers the framework does not emit by default:
/// MIME-sniffing, clickjacking and referrer-leak protection on every response, HSTS over TLS,
/// and a locked-down CSP on the JSON API. The dev-only doc/dashboard surfaces (Scalar, OpenAPI,
/// Hangfire) serve HTML with inline scripts/styles, so the strict CSP is skipped for those
/// paths — they are gated behind IsDevelopment() and never reach production.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string[] CspExemptPrefixes = ["/scalar", "/openapi", "/hangfire"];

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Set on the response as it starts, before the body is written.
        context.Response.OnStarting(() =>
        {
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            var path = context.Request.Path.Value ?? string.Empty;
            var cspExempt = CspExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!cspExempt)
            {
                headers["Content-Security-Policy"] =
                    "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
