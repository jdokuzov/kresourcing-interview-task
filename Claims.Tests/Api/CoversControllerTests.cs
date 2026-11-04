using System.Net;
using System.Net.Http.Json;
using Claims.Auditing;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Claims.Tests.Api;

[Collection(nameof(ApiCollection))]
public class CoversControllerTests(ApiFixture api)
{
    private readonly HttpClient _client = api.CreateClient();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Created_cover_can_be_read_and_deleted()
    {
        var cover = await api.CreateCoverAsync();

        var fetched = await _client.GetFromJsonAsync<Cover>($"/Covers/{cover.Id}", ApiFixture.Json, _cancellationToken);
        Assert.Equal(cover.Premium, fetched!.Premium);
        Assert.Equal(cover.StartDate, fetched.StartDate);
        Assert.Equal(cover.EndDate, fetched.EndDate);
        Assert.Equal(CoverType.Yacht, fetched.Type);

        var all = await _client.GetFromJsonAsync<List<Cover>>("/Covers", ApiFixture.Json, _cancellationToken);
        Assert.Contains(all!, c => c.Id == cover.Id);

        var deleteResponse = await _client.DeleteAsync($"/Covers/{cover.Id}", _cancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await _client.GetAsync($"/Covers/{cover.Id}", _cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);

        var audits = await api.WaitForAuditsAsync<CoverAudit>(a => a.CoverId == cover.Id, expectedCount: 2);
        Assert.Equal(["POST", "DELETE"], audits.Select(a => a.HttpRequestType));
    }

    [Fact]
    public async Task Breaking_a_business_rule_returns_400_with_the_reason()
    {
        var response = await _client.PostAsJsonAsync(
            "/Covers",
            new { StartDate = ApiFixture.Today.AddDays(-1), EndDate = ApiFixture.Today.AddDays(10), Type = "Tanker" },
            ApiFixture.Json,
            _cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFixture.Json, _cancellationToken);
        Assert.Contains("StartDate", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Compute_returns_the_premium()
    {
        var response = await _client.PostAsync(
            "/Covers/compute?startDate=2030-01-01&endDate=2030-01-11&coverType=Tanker",
            content: null,
            _cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Unknown_cover_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/Covers/unknown", _cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/Covers/unknown", _cancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Swagger_document_is_served()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json", _cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
