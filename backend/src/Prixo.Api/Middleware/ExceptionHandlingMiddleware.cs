using FluentValidation;
using Prixo.Application.Common.Exceptions;
using Prixo.Application.Common.Models;
using Prixo.Domain.Exceptions;

namespace Prixo.Api.Middleware;

/// <summary>
/// Maps exceptions to the ApiResponse envelope. Internal details are logged but never
/// returned to clients; the traceId lets support correlate a user report with logs.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed.",
                (IDictionary<string, string[]>?)validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message, null),
            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, forbidden.Message, null),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication is required.", null),
            DomainException domain => (StatusCodes.Status400BadRequest, domain.Message, null),

            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred. Please contact support with the trace id.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path} (TraceId: {TraceId})",
                context.Request.Method, context.Request.Path, traceId);
        }
        else
        {
            logger.LogWarning("Handled {ExceptionType} for {Method} {Path}: {Message} (TraceId: {TraceId})",
                exception.GetType().Name, context.Request.Method, context.Request.Path,
                exception.Message, traceId);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, errors, traceId));
    }
}
