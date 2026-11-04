using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Claims.Auditing;
using Claims.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Claims.Tests.Api;

/// <summary>
/// Runs the real API against real databases (Testcontainers, so Docker is required).
/// Shared by all API tests, because the containers take a while to start.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static DateTime Today => DateTime.UtcNow.Date;

    /// <summary>Creates a 30-day cover starting today.</summary>
    public async Task<Cover> CreateCoverAsync()
    {
        var response = await CreateClient().PostAsJsonAsync(
            "/Covers",
            new { StartDate = $"{Today:yyyy-MM-dd}", EndDate = $"{Today.AddDays(30):yyyy-MM-dd}", Type = "Yacht" },
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Cover>(Json))!;
    }

    /// <summary>Audit records are written in the background, so give them a moment to arrive.</summary>
    public async Task<List<T>> WaitForAuditsAsync<T>(Func<T, bool> predicate, int expectedCount) where T : class
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

        for (var attempt = 0; ; attempt++)
        {
            var audits = (await context.Set<T>().AsNoTracking().ToListAsync()).Where(predicate).ToList();
            if (audits.Count >= expectedCount || attempt == 300)
            {
                return audits;
            }

            await Task.Delay(100);
        }
    }
}

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
