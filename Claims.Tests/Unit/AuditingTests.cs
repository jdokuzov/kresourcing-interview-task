using Azure.Messaging.ServiceBus;
using Claims.Auditing;
using Claims.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Claims.Tests.Unit;

public class AuditingTests
{
    private static readonly DateTime Now = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly ServiceBusSender _sender = Substitute.For<ServiceBusSender>();
    private readonly ServiceProvider _services;
    private readonly OutboxRelay _relay;

    public AuditingTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _services = new ServiceCollection()
            .AddDbContext<ClaimsContext>(options => options.UseInMemoryDatabase(databaseName))
            .AddDbContext<AuditContext>(options => options.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider();
        _relay = new OutboxRelay(_services.GetRequiredService<IServiceScopeFactory>(), _sender, NullLogger<OutboxRelay>.Instance);
    }

    [Fact]
    public void Auditer_adds_the_event_to_the_outbox()
    {
        var outbox = Substitute.For<IRepository<AuditEvent>>();

        new Auditer(outbox, new FixedTimeProvider(Now)).AuditClaim("claim-1", "POST");

        outbox.Received(1).Add(Arg.Is<AuditEvent>(e =>
            e.EntityType == AuditedEntity.Claim
            && e.EntityId == "claim-1"
            && e.HttpRequestType == "POST"
            && e.Created == Now));
    }

    [Fact]
    public async Task Relay_publishes_events_oldest_first_and_removes_them_from_the_outbox()
    {
        await AddToOutboxAsync(Event("newer", Now.AddMinutes(1)), Event("older", Now));
        var published = new List<string>();
        _sender.SendMessageAsync(Arg.Do<ServiceBusMessage>(message => published.Add(message.MessageId)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        Assert.Equal(2, await _relay.PublishPendingAsync(_cancellationToken));

        Assert.Equal(["older", "newer"], published);
        Assert.Empty(await OutboxIdsAsync());
    }

    [Fact]
    public async Task Relay_keeps_an_event_it_could_not_publish()
    {
        await AddToOutboxAsync(Event("event-1", Now));
        _sender.SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceBusException("Unavailable", ServiceBusFailureReason.ServiceBusy));

        await Assert.ThrowsAsync<ServiceBusException>(() => _relay.PublishPendingAsync(_cancellationToken));

        Assert.Equal(["event-1"], await OutboxIdsAsync());
    }

    [Fact]
    public async Task Writer_stores_claim_and_cover_events_in_their_own_tables()
    {
        await WriteAsync(Event("event-1", Now));
        await WriteAsync(Event("event-2", Now, AuditedEntity.Cover));

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        var claimAudit = Assert.Single(context.ClaimAudits);
        Assert.Equal("event-1", claimAudit.MessageId);
        Assert.Equal("entity-1", claimAudit.ClaimId);
        Assert.Equal("POST", claimAudit.HttpRequestType);
        Assert.Equal(Now, claimAudit.Created);
        Assert.Equal("event-2", Assert.Single(context.CoverAudits).MessageId);
    }

    [Fact]
    public async Task Writer_ignores_a_repeated_delivery()
    {
        await WriteAsync(Event("event-1", Now));
        await WriteAsync(Event("event-1", Now));
        await WriteAsync(Event("event-2", Now, AuditedEntity.Cover));
        await WriteAsync(Event("event-2", Now, AuditedEntity.Cover));

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        Assert.Single(context.ClaimAudits);
        Assert.Single(context.CoverAudits);
    }

    private static AuditEvent Event(string id, DateTime created, AuditedEntity entityType = AuditedEntity.Claim) =>
        new()
        {
            Id = id,
            EntityType = entityType,
            EntityId = "entity-1",
            HttpRequestType = "POST",
            Created = created
        };

    private async Task AddToOutboxAsync(params AuditEvent[] events)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsContext>();
        context.AddRange(events.Cast<object>());
        await context.SaveChangesAsync(_cancellationToken);
    }

    private async Task<List<string>> OutboxIdsAsync()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsContext>();
        return await context.Set<AuditEvent>().Select(e => e.Id).ToListAsync(_cancellationToken);
    }

    private async Task WriteAsync(AuditEvent auditEvent)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        await new AuditWriter(context).WriteAsync(auditEvent, _cancellationToken);
    }
}
