namespace ShipmentPlatform.Application.Caching;

public static class ShipmentCacheKeys
{
    public static string ById(Guid id) => $"shipment:id:{id:D}";

    public static string ByTracking(string trackingCode) =>
        $"shipment:tracking:{trackingCode.ToUpperInvariant()}";

    public static IReadOnlyList<string> For(Guid id, string trackingCode) =>
        [ById(id), ByTracking(trackingCode)];
}
