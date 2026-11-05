using Claims.Auditing;
using Claims.Contracts;
using Claims.Domain;
using Claims.Persistence;
using Claims.Services;
using Claims.Validation;
using NSubstitute;
using Xunit;

namespace Claims.Tests.Unit;

public class ClaimsServiceTests
{
    private static readonly CreateClaimRequest Request = new()
    {
        CoverId = "cover-1",
        Created = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Name = "Hull damage",
        Type = ClaimType.Fire,
        DamageCost = 500
    };

    private readonly IRepository<Claim> _claims = Substitute.For<IRepository<Claim>>();
    private readonly IValidator<CreateClaimRequest> _validator = Substitute.For<IValidator<CreateClaimRequest>>();
    private readonly IAuditer _auditer = Substitute.For<IAuditer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ClaimsService _service;

    public ClaimsServiceTests()
    {
        _validator.ValidateAsync(Request).Returns([]);
        _service = new ClaimsService(_claims, _validator, _auditer, _unitOfWork);
    }

    [Fact]
    public async Task Create_saves_the_claim_and_its_audit_together()
    {
        var claim = await _service.CreateAsync(Request);

        Assert.True(Guid.TryParse(claim.Id, out _));
        Assert.Equal(Request.CoverId, claim.CoverId);
        Assert.Equal(Request.Created, claim.Created);
        Assert.Equal(Request.Name, claim.Name);
        Assert.Equal(Request.Type, claim.Type);
        Assert.Equal(Request.DamageCost, claim.DamageCost);
        Received.InOrder(() =>
        {
            _claims.Add(claim);
            _auditer.AuditClaim(claim.Id, "POST");
            _unitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Create_with_an_invalid_request_saves_nothing()
    {
        ValidationFailure[] failures = [new("DamageCost", "Too high")];
        _validator.ValidateAsync(Request).Returns(failures);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => _service.CreateAsync(Request));

        Assert.Equal(failures, exception.Failures);
        _auditer.DidNotReceiveWithAnyArgs().AuditClaim(default!, default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_saves_the_removal_and_its_audit_together()
    {
        _claims.RemoveAsync("claim-1").Returns(true);

        Assert.True(await _service.DeleteAsync("claim-1"));

        Received.InOrder(() =>
        {
            _claims.RemoveAsync("claim-1");
            _auditer.AuditClaim("claim-1", "DELETE");
            _unitOfWork.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Delete_of_an_unknown_claim_audits_nothing()
    {
        _claims.RemoveAsync("unknown").Returns(false);

        Assert.False(await _service.DeleteAsync("unknown"));

        _auditer.DidNotReceiveWithAnyArgs().AuditClaim(default!, default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync();
    }
}
