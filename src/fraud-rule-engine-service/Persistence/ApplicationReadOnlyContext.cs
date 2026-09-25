using Microsoft.EntityFrameworkCore;

namespace fraud_rule_engine_service.Persistence;

/// <summary>
/// Backed by a least-privilege, read-only Postgres role/connection string. Used by the retrieval
/// API so a compromised or buggy query path can never mutate fraud-case data. No-tracking is
/// configured where the context is registered (<see cref="Configuration.PersistenceConfiguration"/>)
/// rather than via `OnConfiguring`, since pooled contexts don't allow options to be modified there.
/// </summary>
public sealed class ApplicationReadOnlyContext(DbContextOptions<ApplicationReadOnlyContext> options)
    : ApplicationDbContextBase(options);
