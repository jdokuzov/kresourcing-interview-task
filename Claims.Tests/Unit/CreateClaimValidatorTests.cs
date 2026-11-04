using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Validation;
using NSubstitute;
using Xunit;

namespace Claims.Tests.Unit;

public class CreateClaimValidatorTests
{
    private const string CoverId = "cover-1";

    private static readonly DateTime CoverStart = new(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CoverEnd = new(2030, 3, 10, 0, 0, 0, DateTimeKind.Utc);

    private readonly CreateClaimValidator _validator;

    public CreateClaimValidatorTests()
    {
        var covers = Substitute.For<IRepository<Cover>>();
        covers.GetByIdAsync(CoverId).Returns(new Cover
        {
            Id = CoverId,
            StartDate = CoverStart,
            EndDate = CoverEnd
        });
        _validator = new CreateClaimValidator(covers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100_000)]
    public async Task Accepts_a_damage_cost_up_to_the_limit(int damageCost)
    {
        Assert.Empty(await _validator.ValidateAsync(Request(damageCost: damageCost)));
    }

    [Theory]
    [InlineData(100_000.01)]
    [InlineData(-1)]
    public async Task Rejects_a_damage_cost_outside_the_allowed_range(double damageCost)
    {
        var failures = await _validator.ValidateAsync(Request(damageCost: (decimal)damageCost));

        Assert.Equal(nameof(CreateClaimRequest.DamageCost), Assert.Single(failures).Field);
    }

    [Fact]
    public async Task Accepts_any_time_on_the_first_and_last_day_of_the_cover()
    {
        Assert.Empty(await _validator.ValidateAsync(Request(created: CoverStart)));
        Assert.Empty(await _validator.ValidateAsync(Request(created: CoverEnd.AddHours(23))));
    }

    [Fact]
    public async Task Rejects_a_date_outside_the_cover_period()
    {
        var before = await _validator.ValidateAsync(Request(created: CoverStart.AddSeconds(-1)));
        var after = await _validator.ValidateAsync(Request(created: CoverEnd.AddDays(1)));

        Assert.Equal(nameof(CreateClaimRequest.Created), Assert.Single(before).Field);
        Assert.Equal(nameof(CreateClaimRequest.Created), Assert.Single(after).Field);
    }

    [Fact]
    public async Task Rejects_an_unknown_cover()
    {
        var failures = await _validator.ValidateAsync(Request(coverId: "unknown"));

        Assert.Equal(nameof(CreateClaimRequest.CoverId), Assert.Single(failures).Field);
    }

    private static CreateClaimRequest Request(string coverId = CoverId, DateTime? created = null, decimal damageCost = 1_000) =>
        new()
        {
            CoverId = coverId,
            Created = created ?? CoverStart.AddDays(1),
            Name = "Hull damage",
            Type = ClaimType.Collision,
            DamageCost = damageCost
        };
}
