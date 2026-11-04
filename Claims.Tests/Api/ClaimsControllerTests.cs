using System.Net;
using System.Net.Http.Json;
using Claims.Auditing;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests.Api;

[Collection(nameof(ApiCollection))]
public class ClaimsControllerTests(ApiFixture api)
{
    private readonly HttpClient _client = api.CreateClient();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_claim_can_be_read_and_deleted()
    {
        var cover = await api.CreateCoverAsync();

        var createResponse = await _client.PostAsJsonAsync("/Claims", ValidClaim(cover), ApiFixture.Json, _cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var claim = (await createResponse.Content.ReadFromJsonAsync<Claim>(ApiFixture.Json, _cancellationToken))!;

        var fetched = await _client.GetFromJsonAsync<Claim>($"/Claims/{claim.Id}", ApiFixture.Json, _cancellationToken);
        Assert.Equal(cover.Id, fetched!.CoverId);
        Assert.Equal(ClaimType.Fire, fetched.Type);
        Assert.Equal(5_000m, fetched.DamageCost);

        var all = await _client.GetFromJsonAsync<List<Claim>>("/Claims", ApiFixture.Json, _cancellationToken);
        Assert.Contains(all!, c => c.Id == claim.Id);

        var deleteResponse = await _client.DeleteAsync($"/Claims/{claim.Id}", _cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await _client.GetAsync($"/Claims/{claim.Id}", _cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);

        var audits = await api.WaitForAuditsAsync<ClaimAudit>(a => a.ClaimId == claim.Id, expectedCount: 2);
        Assert.Equal(["POST", "DELETE"], audits.Select(a => a.HttpRequestType));
    }

    // Guards against dates shifting on their way through MongoDB on servers not running in UTC.
    [Fact]
    public async Task Claim_on_the_last_day_of_the_cover_is_accepted()
    {
        var cover = await api.CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            ValidClaim(cover) with { Created = cover.EndDate.Date.ToString("yyyy-MM-dd") },
            ApiFixture.Json,
            _cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Breaking_a_business_rule_returns_400_with_the_reason()
    {
        var cover = await api.CreateCoverAsync();

        var response = await _client.PostAsJsonAsync(
            "/Claims",
            ValidClaim(cover) with { DamageCost = 100_001m },
            ApiFixture.Json,
            _cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFixture.Json, _cancellationToken);
        Assert.Contains("DamageCost", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Malformed_request_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/Claims", new { Name = "No cover" }, ApiFixture.Json, _cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_claim_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/Claims/unknown", _cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/Claims/unknown", _cancellationToken)).StatusCode);
    }

    private static ClaimPayload ValidClaim(Cover cover) =>
        new(cover.Id, cover.StartDate.ToString("O"), "Engine fire", "Fire", 5_000m);

    private sealed record ClaimPayload(string CoverId, string Created, string Name, string Type, decimal DamageCost);
}
