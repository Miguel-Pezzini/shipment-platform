using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ShipmentPlatform.Domain.Exceptions;

namespace ShipmentPlatform.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, body) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }

    private static (int StatusCode, object Body) Map(Exception exception) => exception switch
    {
        ValidationException ex => (
            StatusCodes.Status400BadRequest,
            new { error = "Validation failed", details = ex.Errors.Select(e => e.ErrorMessage) }),
        DomainException ex => (
            StatusCodes.Status400BadRequest,
            new { error = ex.Message }),
        _ => (
            StatusCodes.Status500InternalServerError,
            new { error = "An unexpected error occurred." })
    };
}
