using System.Threading.Channels;
using Claims.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Claims.Tests.Unit;

public class AuditingTests
{
    private static readonly DateTime Now = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly Channel<IAuditRecord> _queue = Channel.CreateUnbounded<IAuditRecord>();
    private readonly ServiceProvider _services;
    private readonly AuditWorker _worker;
    private readonly Auditer _auditer;

    public AuditingTests()
    {
        var databaseName = Guid.NewGuid().ToString();
        _services = new ServiceCollection()
            .AddDbContext<AuditContext>(options => options.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider();
        _worker = new AuditWorker(_queue, _services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AuditWorker>.Instance);
        _auditer = new Auditer(_queue, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Auditer_only_queues_the_record()
    {
        await _auditer.AuditClaimAsync("claim-1", "POST");

        var audit = Assert.IsType<ClaimAudit>(await _queue.Reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal("claim-1", audit.ClaimId);
        Assert.Equal("POST", audit.HttpRequestType);
        Assert.Equal(Now, audit.Created);
    }

    [Fact]
    public async Task Worker_writes_all_queued_records_before_stopping()
    {
        await _worker.StartAsync(TestContext.Current.CancellationToken);

        await _auditer.AuditClaimAsync("claim-1", "POST");
        await _auditer.AuditCoverAsync("cover-1", "DELETE");
        await _worker.StopAsync(TestContext.Current.CancellationToken);

        await using var context = NewContext();
        Assert.Equal("claim-1", Assert.Single(context.ClaimAudits).ClaimId);
        Assert.Equal("cover-1", Assert.Single(context.CoverAudits).CoverId);
    }

    [Fact]
    public async Task Worker_continues_after_a_record_fails()
    {
        await _worker.StartAsync(TestContext.Current.CancellationToken);

        await _queue.Writer.WriteAsync(new NotInTheModel(), TestContext.Current.CancellationToken);
        await _auditer.AuditClaimAsync("claim-1", "POST");
        await _worker.StopAsync(TestContext.Current.CancellationToken);

        await using var context = NewContext();
        Assert.Equal("claim-1", Assert.Single(context.ClaimAudits).ClaimId);
    }

    private AuditContext NewContext() =>
        _services.CreateScope().ServiceProvider.GetRequiredService<AuditContext>();

    private sealed class NotInTheModel : IAuditRecord;
}
