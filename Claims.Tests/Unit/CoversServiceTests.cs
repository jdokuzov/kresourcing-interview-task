using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Services;
using Claims.Validation;
using NSubstitute;
using Xunit;

namespace Claims.Tests.Unit;

public class CoversServiceTests
{
    private static readonly CreateCoverRequest Request = new()
    {
        StartDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        Type = CoverType.Yacht
    };

    private readonly IRepository<Cover> _covers = Substitute.For<IRepository<Cover>>();
    private readonly IValidator<CreateCoverRequest> _validator = Substitute.For<IValidator<CreateCoverRequest>>();
    private readonly IPremiumCalculator _premiumCalculator = Substitute.For<IPremiumCalculator>();
    private readonly IAuditer _auditer = Substitute.For<IAuditer>();
    private readonly CoversService _service;

    public CoversServiceTests()
    {
        _validator.ValidateAsync(Request).Returns([]);
        _premiumCalculator.Compute(Request.StartDate, Request.EndDate, Request.Type).Returns(123m);
        _service = new CoversService(_covers, _validator, _premiumCalculator, _auditer);
    }

    [Fact]
    public async Task Create_stores_and_audits_a_cover_with_a_generated_id_and_computed_premium()
    {
        var cover = await _service.CreateAsync(Request);

        Assert.True(Guid.TryParse(cover.Id, out _));
        Assert.Equal(Request.StartDate, cover.StartDate);
        Assert.Equal(Request.EndDate, cover.EndDate);
        Assert.Equal(Request.Type, cover.Type);
        Assert.Equal(123m, cover.Premium);
        await _covers.Received(1).AddAsync(cover);
        await _auditer.Received(1).AuditCoverAsync(cover.Id, "POST");
    }

    [Fact]
    public async Task Create_with_an_invalid_request_stores_and_audits_nothing()
    {
        _validator.ValidateAsync(Request).Returns([new ValidationFailure("StartDate", "In the past")]);

        await Assert.ThrowsAsync<RequestValidationException>(() => _service.CreateAsync(Request));

        await _covers.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await _auditer.DidNotReceiveWithAnyArgs().AuditCoverAsync(default!, default!);
    }

    [Fact]
    public async Task Delete_audits_a_deleted_cover()
    {
        _covers.DeleteAsync("cover-1").Returns(true);

        Assert.True(await _service.DeleteAsync("cover-1"));

        await _auditer.Received(1).AuditCoverAsync("cover-1", "DELETE");
    }

    [Fact]
    public async Task Delete_of_an_unknown_cover_audits_nothing()
    {
        _covers.DeleteAsync("unknown").Returns(false);

        Assert.False(await _service.DeleteAsync("unknown"));

        await _auditer.DidNotReceiveWithAnyArgs().AuditCoverAsync(default!, default!);
    }
}
