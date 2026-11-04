using System.Threading.Channels;

namespace Claims.Auditing;

/// <summary>
/// Writes queued audit records to the audit database, one at a time and in order.
/// A record that cannot be written is logged and skipped, so one failure does not stop auditing.
/// </summary>
public sealed class AuditWorker(
    Channel<IAuditRecord> queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditWorker> logger) : BackgroundService
{
    /// <summary>Stops accepting records. The records still queued get written, within the host's shutdown timeout.</summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Writer.TryComplete();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Deliberately not reading with stoppingToken: the loop ends when StopAsync has
        // completed the queue and it is empty, so a shutdown does not lose queued records.
        await foreach (var record in queue.Reader.ReadAllAsync(CancellationToken.None))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
                context.Add((object)record);
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to write audit record {@Record}", record);
            }
        }
    }
}
