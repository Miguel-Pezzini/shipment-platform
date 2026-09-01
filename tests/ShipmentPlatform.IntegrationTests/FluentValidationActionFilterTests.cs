using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ShipmentPlatform.Api.Validation;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Validators;

namespace ShipmentPlatform.IntegrationTests;

public class FluentValidationActionFilterTests
{
    [Fact]
    public async Task InvalidBody_ThrowsValidationException_BeforeAction()
    {
        var filter = new FluentValidationActionFilter();
        var context = CreateContext(new CreateShipmentRequest("", "Cliente", "Curitiba", "São Paulo", 8));
        var nextCalled = false;

        var act = () => filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(Executed(context));
        });

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidBody_CallsNext()
    {
        var filter = new FluentValidationActionFilter();
        var context = CreateContext(new CreateShipmentRequest("ACME", "Cliente", "Curitiba", "São Paulo", 8));
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(Executed(context));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ArgumentWithoutValidator_CallsNext()
    {
        var filter = new FluentValidationActionFilter();
        var context = CreateContext(Guid.NewGuid());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(Executed(context));
        });

        nextCalled.Should().BeTrue();
    }

    private static ActionExecutingContext CreateContext(object argument)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateShipmentRequest>, CreateShipmentRequestValidator>();
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["arg"] = argument },
            controller: null!);
    }

    private static ActionExecutedContext Executed(ActionExecutingContext context) =>
        new(context, [], context.Controller);
}
