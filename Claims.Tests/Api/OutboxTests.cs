using Claims.Domain;
using Claims.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Claims.Tests.Api;

[Collection(nameof(ApiCollection))]
public class OutboxTests(ApiFixture api)
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    // The outbox only works if a change and its audit event are saved all or nothing.
    // This needs a real MongoDB: the second insert breaks on a duplicate id and must undo the first.
    [Fact]
    public async Task A_failed_save_leaves_nothing_behind()
    {
        var existing = NewClaim();
        var other = NewClaim();
        await using (var context = NewContext())
        {
            context.Add(existing);
            await context.SaveChangesAsync(_cancellationToken);
        }

        await using (var context = NewContext())
        {
            context.Add(other);
            context.Add(NewClaim(existing.Id));
            await Assert.ThrowsAnyAsync<Exception>(() => context.SaveChangesAsync(_cancellationToken));
        }

        await using (var context = NewContext())
        {
            Assert.False(await context.Set<Claim>().AnyAsync(claim => claim.Id == other.Id, _cancellationToken));
        }
    }

    private ClaimsContext NewContext() =>
        api.Services.CreateScope().ServiceProvider.GetRequiredService<ClaimsContext>();

    private static Claim NewClaim(string? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            CoverId = "cover-1",
            Name = "Hull damage"
        };
}
