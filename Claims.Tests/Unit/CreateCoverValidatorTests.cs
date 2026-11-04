using Claims.Contracts;
using Claims.Domain;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Unit;

public class CreateCoverValidatorTests
{
    private static readonly DateTime Now = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly CreateCoverValidator _validator = new(new FixedTimeProvider(Now));

    [Fact]
    public async Task Accepts_a_cover_starting_earlier_today()
    {
        Assert.Empty(await ValidateAsync(start: Now.Date, end: Now.Date.AddDays(10)));
    }

    [Fact]
    public async Task Rejects_a_start_date_in_the_past()
    {
        var failures = await ValidateAsync(start: Now.Date.AddDays(-1), end: Now.Date.AddDays(10));

        Assert.Equal(nameof(CreateCoverRequest.StartDate), Assert.Single(failures).Field);
    }

    [Fact]
    public async Task Accepts_a_period_of_exactly_one_year()
    {
        Assert.Empty(await ValidateAsync(start: Now, end: Now.AddYears(1)));
    }

    [Fact]
    public async Task Rejects_a_period_longer_than_one_year()
    {
        var failures = await ValidateAsync(start: Now, end: Now.AddYears(1).AddDays(1));

        Assert.Equal(nameof(CreateCoverRequest.EndDate), Assert.Single(failures).Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rejects_an_end_date_not_after_the_start_date(int days)
    {
        var failures = await ValidateAsync(start: Now, end: Now.AddDays(days));

        Assert.Equal(nameof(CreateCoverRequest.EndDate), Assert.Single(failures).Field);
    }

    [Fact]
    public async Task Reports_every_broken_rule()
    {
        var failures = await ValidateAsync(start: Now.AddDays(-1), end: Now.AddYears(2));

        Assert.Equal(2, failures.Count);
    }

    private Task<IReadOnlyList<ValidationFailure>> ValidateAsync(DateTime start, DateTime end) =>
        _validator.ValidateAsync(new CreateCoverRequest { StartDate = start, EndDate = end, Type = CoverType.Tanker });
}
