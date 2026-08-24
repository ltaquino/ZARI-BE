namespace ZARI.Application.Features.Workflow.Notifications.MarkRead;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Batches what the FE prototype did as N sequential per-id calls into one round trip. Idempotent:
/// already-read ids are silently skipped, both here (existence check) and at the DB level (unique
/// index on NotificationId+UserId) as a second line of defense against a racing duplicate call.
/// </summary>
public sealed class MarkNotificationsReadCommandHandler(IAppDbContext dbContext) : ICommandHandler<MarkNotificationsReadCommand, Result>
{
    public async Task<Result> HandleAsync(MarkNotificationsReadCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Ids.Count == 0)
            return Result.Success();

        var alreadyRead = await dbContext.NotificationReads
            .Where(r => command.Ids.Contains(r.NotificationId) && r.UserId == command.UserId)
            .Select(r => r.NotificationId)
            .ToListAsync(cancellationToken);

        var toInsert = command.Ids.Distinct().Except(alreadyRead);
        var now = DateTimeOffset.UtcNow;

        foreach (var id in toInsert)
        {
            dbContext.NotificationReads.Add(new NotificationRead
            {
                NotificationId = id,
                UserId = command.UserId,
                ReadAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
