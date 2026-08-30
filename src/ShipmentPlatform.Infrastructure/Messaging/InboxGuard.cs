using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShipmentPlatform.Infrastructure.Persistence;
using ShipmentPlatform.Infrastructure.Persistence.Entities;

namespace ShipmentPlatform.Infrastructure.Messaging;

public sealed class InboxGuard(AppDbContext db)
{
    public async Task<bool> TryClaimAsync(
        Guid messageId,
        string consumerName,
        Func<CancellationToken, Task> stageSideEffect,
        CancellationToken cancellationToken = default)
    {
        var alreadyProcessed = await db.InboxMessages.AnyAsync(
            x => x.MessageId == messageId && x.ConsumerName == consumerName,
            cancellationToken);

        if (alreadyProcessed)
            return false;

        db.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            ConsumerName = consumerName,
            ProcessedAtUtc = DateTime.UtcNow
        });

        await stageSideEffect(cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
                entry.State = EntityState.Detached;

            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
