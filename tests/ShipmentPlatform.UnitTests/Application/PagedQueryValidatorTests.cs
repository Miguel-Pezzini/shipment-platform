using FluentAssertions;
using ShipmentPlatform.Application.DTOs;
using ShipmentPlatform.Application.Validators;

namespace ShipmentPlatform.UnitTests.Application;

public class PagedQueryValidatorTests
{
    private readonly PagedQueryValidator _validator = new();

    [Fact]
    public void Defaults_ShouldPass()
    {
        _validator.Validate(new PagedQuery()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void OutOfRange_ShouldFail(int page, int perPage)
    {
        var result = _validator.Validate(new PagedQuery { Page = page, PerPage = perPage });

        result.IsValid.Should().BeFalse();
    }
}
