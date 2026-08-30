using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ShipmentPlatform.Api.ExceptionHandling;
using ShipmentPlatform.Domain.Exceptions;

namespace ShipmentPlatform.IntegrationTests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task DomainException_ReturnsBadRequest()
    {
        var (context, handled) = await HandleAsync(new DomainException("Cannot move from Created to Delivered."));

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var payload = await ReadBodyAsync(context);
        payload.GetProperty("error").GetString().Should().Be("Cannot move from Created to Delivered.");
    }

    [Fact]
    public async Task ValidationException_ReturnsBadRequestWithDetails()
    {
        var exception = new ValidationException([
            new ValidationFailure("SenderName", "Sender name is required.")
        ]);

        var (context, handled) = await HandleAsync(exception);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var payload = await ReadBodyAsync(context);
        payload.GetProperty("error").GetString().Should().Be("Validation failed");
        payload.GetProperty("details")[0].GetString().Should().Be("Sender name is required.");
    }

    [Fact]
    public async Task UnknownException_ReturnsInternalServerErrorWithoutDetails()
    {
        var (context, handled) = await HandleAsync(new InvalidOperationException("secret"));

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var payload = await ReadBodyAsync(context);
        payload.GetProperty("error").GetString().Should().Be("An unexpected error occurred.");
        payload.ToString().Should().NotContain("secret");
    }

    private static async Task<(DefaultHttpContext Context, bool Handled)> HandleAsync(Exception exception)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        return (context, handled);
    }

    private static async Task<JsonElement> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return (await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body))!;
    }
}
