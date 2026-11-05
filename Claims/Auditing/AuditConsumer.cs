using Azure.Messaging.ServiceBus;

namespace Claims.Auditing;

/// <summary>
/// Receives audit events from Service Bus and has them written to the audit database. The processor
/// completes a message when the handler succeeds. When it throws, the message is delivered again,
/// and after the queue's maximum delivery count it moves to the dead-letter queue.
/// </summary>
public sealed class AuditConsumer(
    ServiceBusProcessor processor,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditConsumer> logger) : IHostedService
{
    /// <summary>Also declared in servicebus-emulator.json and infra/main.bicep.</summary>
    public const string QueueName = "audit-events";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;
        return processor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        processor.StopProcessingAsync(cancellationToken);

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var writer = scope.ServiceProvider.GetRequiredService<AuditWriter>();
        await writer.WriteAsync(args.Message.Body.ToObjectFromJson<AuditEvent>()!, args.CancellationToken);
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Failed to process audit messages from {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }
}
