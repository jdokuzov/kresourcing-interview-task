using Claims.Domain;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Persistence;

/// <summary>MongoDB store for claims and covers. All storage mapping lives here, keeping the domain model clean.</summary>
public class ClaimsContext(DbContextOptions<ClaimsContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>(claim =>
        {
            claim.ToCollection("claims");
            claim.Property(c => c.CoverId).HasElementName("coverId");
            claim.Property(c => c.Created).HasElementName("created");
            claim.Property(c => c.Name).HasElementName("name");
            claim.Property(c => c.Type).HasElementName("claimType");
            claim.Property(c => c.DamageCost).HasElementName("damageCost");
        });

        modelBuilder.Entity<Cover>(cover =>
        {
            cover.ToCollection("covers");
            cover.Property(c => c.StartDate).HasElementName("startDate");
            cover.Property(c => c.EndDate).HasElementName("endDate");
            cover.Property(c => c.Type).HasElementName("claimType");
            cover.Property(c => c.Premium).HasElementName("premium");
        });
    }
}
