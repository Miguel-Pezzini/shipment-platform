using System.Diagnostics.CodeAnalysis;
using ShipmentPlatform.Application.Events;

namespace ShipmentPlatform.Infrastructure.Messaging;

public static class OutboxEventTypeMap
{
    private static readonly Dictionary<string, Type> TypesByName = new(StringComparer.Ordinal)
    {
        [NameOf<ShipmentCreatedEvent>()] = typeof(ShipmentCreatedEvent),
        [NameOf<ShipmentStatusChangedEvent>()] = typeof(ShipmentStatusChangedEvent)
    };

    public static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.FullName ?? throw new InvalidOperationException($"Type '{type}' has no full name.");
    }

    public static bool TryGetType(string eventType, [NotNullWhen(true)] out Type? type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        return TypesByName.TryGetValue(eventType, out type);
    }

    private static string NameOf<TEvent>() => GetTypeName(typeof(TEvent));
}
