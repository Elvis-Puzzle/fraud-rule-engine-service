using fraud_rule_engine_service.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Persistence;

/// <summary>
/// Shared entity model for both the read/write and read-only contexts: one Postgres
/// role/connection string for writes, a least-privilege read-only role/connection string for
/// queries, both pointed at the same schema.
/// </summary>
public abstract class ApplicationDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<TransactionSnapshotEntity> TransactionSnapshots => Set<TransactionSnapshotEntity>();
    public DbSet<FraudCaseEntity> FraudCases => Set<FraudCaseEntity>();
    public DbSet<FraudCaseTriggeredRuleEntity> FraudCaseTriggeredRules => Set<FraudCaseTriggeredRuleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fraud_rule_engine");

        modelBuilder.Entity<TransactionSnapshotEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AccountId, e.OccurredAt });
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FraudCaseEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => new { e.AccountId, e.EvaluatedAt });
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Severity);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasMany(e => e.TriggeredRules)
                .WithOne(r => r.FraudCase)
                .HasForeignKey(r => r.FraudCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FraudCaseTriggeredRuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RuleCode);
        });
    }
}
