using FluentAssertions;
using ShipmentPlatform.Application.DTOs;

namespace ShipmentPlatform.UnitTests.Application;

public class PagedResultTests
{
    [Fact]
    public void Create_WhenEmpty_HasZeroPages()
    {
        var result = PagedResult<int>.Create([], 1, 20, 0);

        result.TotalPages.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_RoundsTotalPagesUp()
    {
        var result = PagedResult<int>.Create([1, 2], 1, 2, 5);

        result.TotalPages.Should().Be(3);
        result.TotalCount.Should().Be(5);
    }
}
