using FluentAssertions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Validators;

namespace ShipmentPlatform.UnitTests.Application;

public class CreateShipmentRequestValidatorTests
{
    private readonly CreateShipmentRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var request = new CreateShipmentRequest("ACME", "Cliente", "Curitiba", "São Paulo", 8);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptySender_ShouldFail()
    {
        var request = new CreateShipmentRequest("", "Cliente", "Curitiba", "São Paulo", 8);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SenderName");
    }
}
