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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CoversService _service;

    public CoversServiceTests()
    {
        _validator.ValidateAsync(Request).Returns([]);
        _premiumCalculator.Compute(Request.StartDate, Request.EndDate, Request.Type).Returns(123m);
        _service = new CoversService(_covers, _validator, _premiumCalculator, _auditer, _unitOfWork);
    }

    [Fact]
    public async Task Create_saves_the_cover_with_computed_premium_and_its_audit_together()
    {
        var cover = await _service.CreateAsync(Request);

        Assert.True(Guid.TryParse(cover.Id, out _));
        Assert.Equal(Request.StartDate, cover.StartDate);
        Assert.Equal(Request.EndDate, cover.EndDate);
        Assert.Equal(Request.Type, cover.Type);
        Assert.Equal(123m, cover.Premium);
        Received.InOrder(() =>
        {
            _covers.Add(cover);
            _auditer.AuditCover(cover.Id, "POST");
            _unitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Create_with_an_invalid_request_saves_nothing()
    {
        _validator.ValidateAsync(Request).Returns([new ValidationFailure("StartDate", "In the past")]);

        await Assert.ThrowsAsync<RequestValidationException>(() => _service.CreateAsync(Request));

        _auditer.DidNotReceiveWithAnyArgs().AuditCover(default!, default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_saves_the_removal_and_its_audit_together()
    {
        _covers.RemoveAsync("cover-1").Returns(true);

        Assert.True(await _service.DeleteAsync("cover-1"));

        Received.InOrder(() =>
        {
            _covers.RemoveAsync("cover-1");
            _auditer.AuditCover("cover-1", "DELETE");
            _unitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Delete_of_an_unknown_cover_audits_nothing()
    {
        _covers.RemoveAsync("unknown").Returns(false);

        Assert.False(await _service.DeleteAsync("unknown"));

        _auditer.DidNotReceiveWithAnyArgs().AuditCover(default!, default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }
}
