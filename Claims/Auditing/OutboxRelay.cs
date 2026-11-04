using Azure.Messaging.ServiceBus;
using Claims.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

/// <summary>
/// Publishes outbox events to Service Bus, oldest first, and deletes each one once it is sent.
/// A crash between those two steps sends the event again, which <see cref="AuditWriter"/> tolerates.
/// While Service Bus is unavailable, events wait in the outbox.
/// </summary>
public sealed class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    ServiceBusSender sender,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    /// <returns>The number of events published.</returns>
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsContext>();

        var pending = await context.Set<AuditEvent>()
            .OrderBy(auditEvent => auditEvent.Created)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var auditEvent in pending)
        {
            var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(auditEvent)) { MessageId = auditEvent.Id };
            await sender.SendMessageAsync(message, cancellationToken);

            context.Remove(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
        }

        return pending.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var published = 0;
            try
            {
                published = await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to publish audit events; they stay in the outbox");
            }

            if (published == 0)
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }
}
