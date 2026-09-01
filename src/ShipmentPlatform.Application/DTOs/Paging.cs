namespace ShipmentPlatform.Application.DTOs;

public record PagedQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPerPage = 20;
    public const int MaxPerPage = 100;

    public int Page { get; init; } = DefaultPage;
    public int PerPage { get; init; } = DefaultPerPage;
}

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PerPage,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int perPage, int totalCount) =>
        new(
            items,
            page,
            perPage,
            totalCount,
            perPage <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)perPage));
}
